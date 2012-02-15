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
    open System.Xml
    open System.Xml.Linq
    open System.ComponentModel.Composition
    open System.ComponentModel.Composition.Hosting
    open System.ComponentModel.Composition.Primitives
    open Castle.Extensibility

    (* 
    <?xml version="1.0" encoding="utf-8"?>
    <manifest>
      <exports>
        <export>
            <contract>x.IService</contract>
            <metadata>
                <entry key="TypeIdentity" type="">value</entry>
            </metadata>
        </export>
      </exports>
      <imports>
        <import>
            <contract>aa</contract>
            <cardinality>ZeroOrMore</cardinality>
            <!--
            <requiredCreationPolicy>Any</requiredCreationPolicy>
            <requiredMetadata>
              KeyValuePair<string, Type> 
            </requiredMetadata>
            isRecomposable
            isPrerequisite
            -->
            <metadata>
                <entry key="_TypeIdentity" type="System.Type">IService</entry>
            </metadata>
        </import>
      </imports>
    </manifest>    
    *)


    type DefinitionCache(exports:ExportDefinition seq, imports:ImportDefinition seq) = 
        member x.Exports = exports
        member x.Imports = imports


    module DefinitionsCacheReader = 
       
        let private build_metadata_entry (entry:XElement) (binder:IBindingContext) = 
            let key = entry.Attribute(XName.Get("key")).Value
            let typeName = entry.Attribute(XName.Get("type")).Value
            let valAsStr = entry.Value
            let typeIns = Type.GetType(typeName, true, false)
            // supported types
            if not typeIns.IsPrimitive && typeIns <> typeof<string> && typeIns <> typeof<Type> 
            then failwithf "Unsupported type in metadata entry: %s" typeName
            let value = 
                if typeIns = typeof<Type> then 
                    binder.GetContextType(valAsStr) |> box
                else
                    Convert.ChangeType(valAsStr, typeIns)
            (key, value)

        let private build_metadata (metadataElem:XElement) (binder:IBindingContext) bundleName = 
            let dict = Dictionary<string,obj>()
            dict.["_BundleSource"] <- bundleName
            if metadataElem <> null then 
                metadataElem.Elements()
                |> Seq.iter (fun m -> let pair = build_metadata_entry m binder
                                      dict.Add(fst pair, snd pair) )
            dict
    
        let private build_exp_definition (elem:XElement) (binder:IBindingContext) bundleName = 
            let contractElem = elem.Element(XName.Get("contract"))
            let metadataElem = elem.Element(XName.Get("metadata"))
            let contract = 
                if contractElem <> null then contractElem.Value.Trim() else failwith "The 'contract' element is required"

            ExportDefinition(contract, (build_metadata metadataElem binder bundleName) )
            
        let private build_imp_definition (elem:XElement) (binder:IBindingContext) bundleName : ImportDefinition = 
            let contractElem = elem.Element(XName.Get("contract"))
            let metadataElem = elem.Element(XName.Get("metadata"))
            let cardinalityElem = elem.Element(XName.Get("cardinality"))
            let contract = 
                if contractElem <> null then contractElem.Value.Trim() else failwith "The 'contract' element is required"
            let metadata = build_metadata metadataElem binder bundleName
            let _, typeIdentity = metadata.TryGetValue(CompositionConstants.ExportTypeIdentityMetadataName)
            let typeIdentity = if typeIdentity <> null then typeIdentity.ToString() else null
            let requiredMetadata : KeyValuePair<string,Type> seq = Seq.empty
            let cardinality = 
                if cardinalityElem <> null && cardinalityElem.Value != null then
                    Enum.Parse(typeof<ImportCardinality>, cardinalityElem.Value.Trim()) :?> ImportCardinality
                else ImportCardinality.ZeroOrOne
            let isRecomposable = true
            let isPreReq = false
            upcast ContractBasedImportDefinition(contract, typeIdentity, requiredMetadata, cardinality, isRecomposable, isPreReq, CreationPolicy.Any, metadata)


        let build_manifest (input:TextReader) (physicalPath:string) (binder:IBindingContext) (bundleName:string) = 
            let doc = XDocument.Load(input)
            let exports = doc.Root.Descendants(XName.Get("export"))
            let imports = doc.Root.Descendants(XName.Get("import"))

            let exportDefs = exports |> Seq.map (fun element -> build_exp_definition element binder bundleName)
            let importDefs = imports |> Seq.map (fun element -> build_imp_definition element binder bundleName)

            DefinitionCache(exportDefs, importDefs)


    module ManifestReader = 
        (* 
        <manifest>
            <name>bundle name</name>
            <version>1.2.2.1</version>
            <composer>
                <type>qualified type name</type>
                <parameters>
                    <parameter></parameter>
                    <parameter></parameter>
                </parameters>
            </composer>
            
            <exports>
                <contract> </contract>
                <contract> </contract>
                <contract> </contract>
            </exports>

            <imports>
                <contract> </contract>
                <contract> </contract>
                <contract> </contract>
            </imports>

            <dependencies>
                <bundle>name, version</bundle>
                <bundle>name, version</bundle>

                <assembly>qualified name </assembly>
                <assembly>qualified name </assembly>
            </dependencies>

            <behaviors>
                <behavior optional='true'> name </behavior>
                <behavior optional='true'> name </behavior>
            </behaviors>
        </manifest>
        *)

        let private build_composer (elem:XElement) = 
            let typeNameElem = elem.Element(XName.Get("type"))
            let typeName = if typeNameElem <> null then typeNameElem.Value else null
            let parameters = 
                seq { 
                    for el in elem.Descendants(XName.Get("parameter")) do 
                        yield el.Value
                } |> Seq.toList
            ComposerSettings(typeName, parameters)

        let build_manifest (input:Stream) (physicalPath:string) = 
            let doc = XDocument.Load(input)
            // todo: assert root is 'manifest'

            let name = ref ""
            let mutable composer : ComposerSettings = null
            let version = ref (Version())

            for elem in doc.Root.Elements() do 
                match elem.Name.LocalName with
                | "name" -> name := elem.Value
                | "version" -> version := Version(elem.Value)
                | "composer" -> 
                    composer <- build_composer(elem)
                | "exports" -> ()
                | "imports" -> ()
                | "dependencies" -> ()
                | "behaviors" -> ()
                | _ -> ()
            
            Manifest(!name, !version, composer, physicalPath)

