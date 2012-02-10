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

    module DefinitionsCacheWriter = 

        let private write_metadata_entry (keyvalue:KeyValuePair<string,obj>) : XElement = 
            let key = keyvalue.Key
            let value = keyvalue.Value
            let entryElem = XElement(XName.Get("entry"))
            entryElem.Add(XAttribute(XName.Get("key"), key))
            match value.GetType() with
                | v when v.IsPrimitive || v = typeof<string> ->
                    [| 
                        XAttribute(XName.Get("type"), value.GetType().AssemblyQualifiedName) |> box
                        XText(value.ToString()) |> box
                    |]
                | v when typeof<Type>.IsAssignableFrom(v)  ->
                    let value = value :?> Type
                    [| 
                        XAttribute(XName.Get("type"), typeof<Type>.AssemblyQualifiedName) |> box
                        XText(value.AssemblyQualifiedName) |> box
                    |]
                | _ -> failwithf "unsupported type %O" (value.GetType())
            |> entryElem.Add
            entryElem

        let private write_metadata (metadata:IDictionary<string,obj>) = 
            let metadataElem = XElement(XName.Get("metadata"))
            metadataElem.Add( metadata |> Seq.map (fun kv -> write_metadata_entry kv ) |> Seq.cast<obj> )
            metadataElem

        let write_manifest (output:TextWriter) (exports:ExportDefinition seq) (imports:ImportDefinition seq) = 
            let doc = XDocument()
            let exportElements = XElement(XName.Get("exports", ""))
            let importElements = XElement(XName.Get("imports", ""))
            
            for exp in exports do
                let expElem = XElement(XName.Get("export"))
                expElem.Add(XElement(XName.Get("contract"), XText(exp.ContractName)))
                if exp.Metadata.Count <> 0 then
                    expElem.Add( write_metadata exp.Metadata )

                exportElements.Add expElem
                // printfn "Exporting %s" exp.ContractName
            
            for imp in imports do
                let impElem = XElement(XName.Get("import"))
                impElem.Add(XElement(XName.Get("contract"), XText(imp.ContractName)))
                impElem.Add(XElement(XName.Get("cardinality"), XText(imp.Cardinality.ToString())))
                if imp.Metadata.Count <> 0 then
                    impElem.Add( write_metadata imp.Metadata )

                importElements.Add impElem
                // printfn "Importing %s" imp.ContractName
            
            let root = XElement(XName.Get("manifest", ""))
            root.Add(exportElements)
            root.Add(importElements)
            doc.AddFirst(root)
            doc.Save(output)
    

