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


    [<AbstractClass>]
    type BundlePartDefinitionBase(exports, imports) = 
        class
            inherit ComposablePartDefinition()

            static let check_member (t, m:ICustomAttributeProvider) = 
                if m.IsDefined(typeof<BundleExportAttribute>, true) || 
                   m.IsDefined(typeof<IAttributedImportDef>, true) then Some(t,m) else None

            static let to_cname = System.ComponentModel.Composition.AttributedModelServices.GetContractName
            static let to_typeId (t:Type) = System.ComponentModel.Composition.AttributedModelServices.GetTypeIdentity(t)

            static let build_export (t:Type, m:ICustomAttributeProvider) = 
                let att = m.GetCustomAttributes(typeof<BundleExportAttribute>, true).SingleOrDefault() :?> BundleExportAttribute
                if att <> null then
                    let target = if att.ContractType = null then t else att.ContractType 
                    let contract_name = att.ContractName <!> to_cname target
                    let metadata = new Dictionary<string, obj>()
                    metadata.[CompositionConstants.ExportTypeIdentityMetadataName] <- to_typeId target
                    ExportDefinition(contract_name, metadata)
                else
                    null
            
            static let build_import (t:Type, m:ICustomAttributeProvider) = 
                let att = m.GetCustomAttributes(typeof<IAttributedImportDef>, true).SingleOrDefault() :?> IAttributedImportDef
                if att <> null then
                    let target = 
                        let typ = if att.ContractType = null then t else att.ContractType
                        if att.Cardinality = ImportCardinality.ZeroOrMore && typ.IsGenericType && typ.GetGenericTypeDefinition() = typedefof<IEnumerable<_>> then
                            typ.GetGenericArguments().Single()
                        else
                            typ
                    let contract_name = att.ContractName <!> to_cname target
                    let metadata = new Dictionary<string, obj>()
                    let typeId = to_typeId target
                    ContractBasedImportDefinition(contract_name, typeId, Seq.empty, att.Cardinality, true, false, att.CreationPolicyReq)
                else
                    null

            static let build_bundle_metadata (t:Type) = 
                if t = null then None
                else
                    let imports = List<ImportDefinition>()
                    let exports = List<ExportDefinition>()
    
                    if t.IsDefined(typeof<BundleExportAttribute>, true) then
                        exports.Add(build_export(t, t))
    
                    let flags = BindingFlags.Public ||| BindingFlags.Instance
    
                    // let fields = t.GetFields(flags)    |> Seq.map (fun f -> (f.FieldType, f :> ICustomAttributeProvider))
                    let props = t.GetProperties(flags) |> Seq.map (fun f -> (f.PropertyType, f :> ICustomAttributeProvider))
                    let constructorInfo = t.GetConstructors(flags) |> Seq.filter (fun c -> c.IsDefined(typeof<ImportingConstructorAttribute>, false)) 
                    let parameters = 
                        if not (Seq.isEmpty constructorInfo) then 
                            (constructorInfo |> Seq.head) .GetParameters() |> Seq.map (fun f -> (f.ParameterType, f :> ICustomAttributeProvider))
                        else
                            Seq.empty
                    
                    // let bundleMembers = Seq.append fields <| Seq.append props parameters |> Seq.choose check_member
                    let bundleMembers = Seq.append props parameters |> Seq.choose check_member
                    
                    for m in bundleMembers do 
                        let importDef = build_import m
                        let exportDef = build_export m
                        if importDef <> null then imports.Add importDef
                        if exportDef <> null then exports.Add exportDef
                        
                    if (exports.Count <> 0 || imports.Count <> 0) then
                        Some(exports, imports)
                    else
                        None

            new (types:Type seq) = 
                let bundleTypes = types |> Seq.choose build_bundle_metadata
                let exports = bundleTypes |> Seq.map (fun t -> fst t) |> Seq.concat
                let imports = bundleTypes |> Seq.map (fun t -> snd t) |> Seq.concat
                BundlePartDefinitionBase(exports, imports)

            override x.ExportDefinitions = exports
            override x.ImportDefinitions = imports
        end 


    
    // MefCompositePartDefinition(catalog, context, catalog_exports, catalog_imports, manifest, frameworkCtx, behaviors)
    type MefBundlePartDefinition(catalog:ComposablePartCatalog, exports:ExportDefinition seq, imports, manifest:Manifest, fxServices, fxContext, behaviors:IBehavior seq) = 
        class
            inherit BundlePartDefinitionBase(exports, imports)

            new (catalog:ComposablePartCatalog, manifest:Manifest, fxServices, fxContext, behaviors) = 
                let catalog_exports = catalog.Parts |> Seq.collect (fun p -> p.ExportDefinitions)
                let catalog_imports = catalog.Parts |> Seq.collect (fun p -> p.ImportDefinitions)
                MefBundlePartDefinition(catalog, catalog_exports, catalog_imports, manifest, fxServices, fxContext, behaviors)

            new (types:Type seq, manifest:Manifest, bindingContext, fxServices, behaviors) = 
                MefBundlePartDefinition(new TypeCatalog(types), manifest, fxServices, null, behaviors)

            new (folder:string, manifest, bindingContext:BindingContext, fxServices, behaviors) = 
                bindingContext.LoadAssemblies(folder)
                let types = bindingContext.GetAllTypes()
                MefBundlePartDefinition(types, manifest, bindingContext, fxServices, behaviors)
            
            override x.CreatePart() = 
                let frameworkCtx : ModuleContext = fxContext <!> upcast FrameworkContext(fxServices, manifest.Name)
                upcast new MefBundlePart(catalog, manifest, exports, imports, frameworkCtx, behaviors)
        end

    
    and MefBundlePart(catalog, manifest:Manifest, exports, imports, frameworkCtx, behaviors) = 
        class
            inherit ComposablePart()
            
            // we should actually use AssemblyCatalog as it supports the Custom refection context attribute
            let _flags = CompositionOptions.DisableSilentRejection ||| CompositionOptions.IsThreadSafe ||| CompositionOptions.ExportCompositionService
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
                upcast MefBundlePartDefinition(new TypeCatalog(types), manifest, null, frameworkCtx, behaviors)



