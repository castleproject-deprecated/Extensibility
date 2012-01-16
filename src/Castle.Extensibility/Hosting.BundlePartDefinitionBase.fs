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


    [<AbstractClass>]
    type BundlePartDefinitionBuilder() = 
        class

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
                    let constructors = t.GetConstructors(flags) 
                    let parameters = 
                        if not (Seq.isEmpty constructors) then 
                            constructors 
                            |> Seq.collect (fun c -> c.GetParameters()) 
                            |> Seq.map (fun f -> (f.ParameterType, f :> ICustomAttributeProvider))
                        else
                            Seq.empty
                    
                    // let bundleMembers = Seq.append fields <| Seq.append props parameters |> Seq.choose check_member
                    let bundleMembers = 
                        Seq.append props parameters 
                        |> Seq.choose check_member 
                        |> Seq.distinctBy (fun t -> fst t)
                    
                    for m in bundleMembers do 
                        let importDef = build_import m
                        let exportDef = build_export m
                        if importDef <> null then imports.Add importDef
                        if exportDef <> null then exports.Add exportDef
                        
                    if (exports.Count <> 0 || imports.Count <> 0) then
                        Some(exports, imports)
                    else
                        None

            static let collect_bundle_definitions(types) = 
                let bundleTypes = types |> Seq.choose build_bundle_metadata
                let exports = bundleTypes |> Seq.map (fun t -> fst t) |> Seq.concat
                let imports = bundleTypes |> Seq.map (fun t -> snd t) |> Seq.concat
                (exports,imports)

            static member CollectBundleDefinitions(types:Type seq) = 
                collect_bundle_definitions types


        end 



