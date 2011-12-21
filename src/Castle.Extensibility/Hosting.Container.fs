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


    type BundlePartDefinition(types:Type seq) as this = 
        class
            inherit ComposablePartDefinition()

            new (folder:string) = 
                let types = new RefHelpers.DirectoryTypesLoaderGuarded(folder)
                BundlePartDefinition(types.Types)
            
            [<DefaultValue>] val mutable private _exports : ExportDefinition seq
            [<DefaultValue>] val mutable private _imports : ImportDefinition seq

            let check_member (t, m:ICustomAttributeProvider) = 
                if m.IsDefined(typeof<BundleExportAttribute>, true) || 
                   m.IsDefined(typeof<IAttributedImportDef>, true) then Some(t,m) else None

            let to_cname = System.ComponentModel.Composition.AttributedModelServices.GetContractName
            let to_typeId (t:Type) = System.ComponentModel.Composition.AttributedModelServices.GetTypeIdentity(t)

            let build_export (t:Type, m:ICustomAttributeProvider) = 
                let att = m.GetCustomAttributes(typeof<BundleExportAttribute>, true).SingleOrDefault() :?> BundleExportAttribute
                if att <> null then
                    let target = if att.ContractType = null then t else att.ContractType 
                    let contract_name = att.ContractName <!> to_cname target
                    let metadata = new Dictionary<string, obj>()
                    metadata.[CompositionConstants.ExportTypeIdentityMetadataName] <- to_typeId target
                    ExportDefinition(contract_name, metadata)
                else
                    null
            
            let build_import (t:Type, m:ICustomAttributeProvider) = 
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

            let build_bundle_metadata (t:Type) = 
                let imports = List<ImportDefinition>()
                let exports = List<ExportDefinition>()

                if t.IsDefined(typeof<BundleExportAttribute>, true) then
                    exports.Add(build_export(t, t))

                let flags = BindingFlags.Public ||| BindingFlags.Instance

                let fields = t.GetFields(flags)    |> Seq.map (fun f -> (f.FieldType, f :> ICustomAttributeProvider))
                let props = t.GetProperties(flags) |> Seq.map (fun f -> (f.PropertyType, f :> ICustomAttributeProvider))
                let constructorInfo = t.GetConstructors(flags) |> Seq.filter (fun c -> c.IsDefined(typeof<ImportingConstructorAttribute>, false)) 
                let parameters = 
                    if not (Seq.isEmpty constructorInfo) then 
                        (constructorInfo |> Seq.head) .GetParameters() |> Seq.map (fun f -> (f.ParameterType, f :> ICustomAttributeProvider))
                    else
                        Seq.empty
                
                let bundleMembers = Seq.append fields <| Seq.append props parameters |> Seq.choose check_member
                
                for m in bundleMembers do 
                    let importDef = build_import m
                    let exportDef = build_export m
                    if importDef <> null then imports.Add importDef
                    if exportDef <> null then exports.Add exportDef
                    
                if (exports.Count <> 0 || imports.Count <> 0) then
                    Some(exports, imports)
                else
                    None

            do
                let bundleTypes = types |> Seq.choose build_bundle_metadata
                this._exports <- bundleTypes |> Seq.map (fun t -> fst t) |> Seq.concat
                this._imports <- bundleTypes |> Seq.map (fun t -> snd t) |> Seq.concat

            override x.ExportDefinitions = x._exports
            override x.ImportDefinitions = x._imports
            override x.CreatePart() = 
                upcast new MefBundlePart(types, x._exports, x._imports)
        end


    and MefBundlePart(types:Type seq, exports, imports) = 
        class
            inherit ComposablePart()
            
            let _catalog = new TypeCatalog(types)
            let _flags = CompositionOptions.DisableSilentRejection ||| CompositionOptions.IsThreadSafe ||| CompositionOptions.ExportCompositionService
            let _container = lazy( new CompositionContainer(_catalog, _flags) )
            
            override x.ExportDefinitions = exports
            override x.ImportDefinitions = imports
            override x.Activate() = 
                let cont = _container.Force()
                let starters = cont.GetExports<IModuleStarter>()
                starters |> Seq.iter (fun s -> s.Force().Initialize())

            override x.GetExportedValue(expDef) = 
                // very naive implementation, but should do for now
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
                        batch.AddExport e |> ignore
                    _container.Force().Compose(batch) 

            interface IDisposable with 
                member x.Dispose() = 
                    _container.Force().Dispose()
                    _catalog.Dispose()
        end


    type BundleCatalog(dir:string) = 
        class
            inherit ComposablePartCatalog()
            let _parts = List<ComposablePartDefinition>()

            do 
                let dirs = Directory.GetDirectories(dir)
                for f in dirs do
                    _parts.Add (BundlePartDefinition(f))

            let select_candidates (cpd:ComposablePartDefinition) (imp:ImportDefinition) = 
                let exports = 
                    cpd.ExportDefinitions 
                    |> Seq.choose (fun e -> if e.ContractName = imp.ContractName then Some(e) else None)
                if not (Seq.isEmpty exports) then
                    Some((cpd,exports))
                else
                    None

            override x.Parts = _parts.AsQueryable()
            override x.GetExports(impDef) = 
                let candidates = 
                    _parts 
                    |> Seq.choose (fun p -> select_candidates p impDef)
                
                seq { 
                    for candidate in candidates do
                        for e in snd candidate do
                            yield (fst candidate, e)
                }
                
        end


    type HostingContainer (bundles:BundleCatalog seq, appCatalog:ComposablePartCatalog) = 
        let catalogs = seq {  yield appCatalog
                              yield! (bundles |> Seq.cast<ComposablePartCatalog>) }
        let _aggCatalogs = new AggregateCatalog(catalogs)
        let _container   = new CompositionContainer(_aggCatalogs, CompositionOptions.DisableSilentRejection)

        new (bundleDir:string, appCatalog) = 
            let bundles = [|new BundleCatalog(bundleDir)|]
            HostingContainer(bundles, appCatalog)
        
        member x.GetExportedValue() = _container.GetExportedValue()
        member x.GetExportedValue(name) = _container.GetExportedValue(name)

        

