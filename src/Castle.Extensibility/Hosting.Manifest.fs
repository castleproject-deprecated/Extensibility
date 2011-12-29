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


    [<AllowNullLiteral>]
    type Manifest(name:string, version:Version, customComposer:string) = 

        member x.Name = name
        member x.Version = version
        member x.CustomComposer = customComposer
        (*
        member x.Name with get() = x._name and set(v) = x._name <- v
        member x.Version with get() = x._version and set(v) = x._version <- v
        member x.Exports with get() = x._exports and set(v) = x._exports <- v
        member x.Imports with get() = x._imports and set(v) = x._imports <- v
        member x.CustomComposer with get() = x._composer and set(v) = x._composer <- v

        // Dependencies : bundles or assemblies?
        // Behaviors : act-as definitions must be understood by the hosting/framework
        *)

    [<AllowNullLiteral>]
    type IBehavior = 
        interface
            abstract member GetBehaviorExports : imports:ImportDefinition seq * exports:ExportDefinition seq * manifest:Manifest -> Export seq
        end

    
    // [<TypeEquivalence; Guid>]
    type IComposablePartDefinitionBuilder =
        interface 
            abstract member Build : ctx:BindingContext * 
                                    exports:ExportDefinition seq * 
                                    imports:ImportDefinition seq * 
                                    manifest:Manifest * 
                                    frameworkCtx:ModuleContext * 
                                    behaviors:IBehavior seq -> ComposablePartDefinition
        end


    module ManifestReader = 
        
        (* 
        <manifest>
            <name>bundle name</name>
            <version>1.2.2.1</version>
            <composer>qualified type name</composer>
            
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

        let build_manifest(input:Stream) = 
            let doc = XDocument.Load(input)
            
            // todo: assert root is 'manifest'

            let name = ref ""
            let composer = ref ""
            let version = ref (Version())

            for elem in doc.Root.Elements() do 
                match elem.Name.LocalName with
                | "name" -> name := elem.Value
                | "version" -> version := Version(elem.Value)
                | "composer" -> composer := elem.Value
                | "exports" -> ()
                | "imports" -> ()
                | "dependencies" -> ()
                | "behaviors" -> ()
                | _ -> ()
            Manifest(!name, !version, !composer)