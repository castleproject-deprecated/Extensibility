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

        let build_composer (elem:XElement) = 
            let typeNameElem = elem.Element(XName.Get("type"))
            let typeName = if typeNameElem <> null then typeNameElem.Value else null
            let parameters = 
                seq { 
                    for el in elem.Descendants(XName.Get("parameters")) do 
                        yield el.Value
                }
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

