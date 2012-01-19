//  Copyright 2004-2011 Castle Project - http://www.castleproject.org/
//  Hamilton Verissimo de Oliveira and individual contributors as indicated. 
//  See the committers.txt/contributors.txt in the distribution for a 
//  full listing of individual contributors.
// 
//  This is free software; you can redistribute it and/or modify it
//  under the terms of the GNU Lesser General Public License as
//  published by the Free Software Foundation; either version 3 of
//  the License, or (at your option) any later version.
// 
//  You should have received a copy of the GNU Lesser General Public
//  License along with this software; if not, write to the Free
//  Software Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA
//  02110-1301 USA, or see the FSF site: http://www.fsf.org.

namespace Castle.Extensibility.Hosting

    open System
    open System.IO
    open System.Linq
    open System.Reflection
    open System.Threading
    open System.Collections.Generic
    open System.ComponentModel.Composition
    open System.ComponentModel.Composition.Hosting
    open System.ComponentModel.Composition.Primitives
    open Castle.Extensibility

    type internal FrameworkContext(type2Activator:Dictionary<Type, string -> obj>, bundleName) = 
        inherit ModuleContext()

        override x.HasService<'T when 'T : null>() =
            type2Activator.ContainsKey(typeof<'T>)
        
        override x.GetService<'T when 'T : null>() =
            let res, activator = type2Activator.TryGetValue(typeof<'T>)
            if res then
                activator(bundleName) :?> 'T
            else 
                null
           
    type MefContext(fxContainer:ModuleContext) = 
        inherit ModuleContext()

        override x.HasService() = fxContainer.HasService()
        override x.GetService() = fxContainer.GetService()


    type MefBundlePartDefinition(catalog:ComposablePartCatalog, exports:ExportDefinition seq, imports, manifest:Manifest, fxServices, fxContext, behaviors:IBehavior seq) = 
        class
            inherit ComposablePartDefinition()

            new (types:Type seq, manifest:Manifest, bindingContext, fxServices, behaviors) =
                let result = BundlePartDefinitionBuilder.CollectBundleDefinitions types
                // todo: should we compute the subset? if this part definition is part of a composite, 
                // then its exports/imports will be just a subset of the total
                // we could compute this subset by comparing what we found on the Type catalog
                let exports = fst result 
                let imports = snd result 
                MefBundlePartDefinition(new TypeCatalog(types), exports, imports, manifest, fxServices, null, behaviors)

            new (folder:string, manifest, bindingContext:BindingContext, fxServices, behaviors) = 
                bindingContext.LoadAssemblies(folder)
                let types = bindingContext.GetAllTypes()
                MefBundlePartDefinition(types, manifest, bindingContext, fxServices, behaviors)
            
            override x.ExportDefinitions = exports
            override x.ImportDefinitions = imports

            override x.CreatePart() = 
                let frameworkCtx : ModuleContext = fxContext <!> upcast FrameworkContext(fxServices, manifest.Name)
                upcast new MefBundlePart(catalog, manifest, exports, imports, frameworkCtx, behaviors)
        end

    
    and MefBundlePart(catalog, manifest:Manifest, exports, imports, frameworkCtx, behaviors) = 
        class
            inherit ComposablePart()
            
            // we should actually use AssemblyCatalog as it supports the Custom refection context attribute
            let _flags = CompositionOptions.DisableSilentRejection ||| CompositionOptions.IsThreadSafe //||| CompositionOptions.ExportCompositionService
            let _container = lazy( new CompositionContainer(catalog, _flags) )
            
            override x.ExportDefinitions = exports
            override x.ImportDefinitions = imports
            override x.Activate() = 
                let cont = _container.Force()
                let starters = cont.GetExports<IModuleStarter>()
                let name = manifest.Name
                let ctx = MefContext(frameworkCtx)
                starters |> Seq.iter (fun s -> s.Force().Initialize(ctx))

            override x.GetExportedValue(expDef) = 
                let typeId = expDef.Metadata.[CompositionConstants.ExportTypeIdentityMetadataName].ToString()
                let impDef = ContractBasedImportDefinition(expDef.ContractName, typeId, Seq.empty, ImportCardinality.ZeroOrMore, true, false, CreationPolicy.Any)
                let exports = _container.Force().GetExports(impDef)
                if Seq.isEmpty exports then
                    null
                else
                    let values = exports |> Seq.map (fun e -> e.Value)
                    if values.Count() > 1 then
                        values :> obj
                    else
                        values |> Seq.head
                
            override x.SetImport( importDef, exports ) = 
                if not (Seq.isEmpty exports) then 
                    let batch = CompositionBatch()
                    for e in exports do
                        batch.AddExport e |> ignore // todo: collect the parts so we can dispose them 
                    _container.Force().Compose(batch) 

            interface IDisposable with 
                member x.Dispose() = 
                    _container.Force().Dispose()
        end
    

    [<AllowNullLiteral>]
    type MefComposerBuilder(parameters:string seq) = 
        
        interface IComposablePartDefinitionBuilder with
            
            member x.Build(context, exports, imports, manifest, frameworkCtx, behaviors) = 
                let types = context.GetAllTypes()
                let catalog = new TypeCatalog(types)

                let exportsubset = System.Linq.Enumerable.Intersect(exports, catalog.Parts |> Seq.collect(fun p -> p.ExportDefinitions), ExportComparer.Instance)
                let importsubset = System.Linq.Enumerable.Intersect(imports, catalog.Parts |> Seq.collect(fun p -> p.ImportDefinitions), ImportComparer.Instance)

                upcast MefBundlePartDefinition(catalog, exportsubset, importsubset, manifest, null, frameworkCtx, behaviors)



