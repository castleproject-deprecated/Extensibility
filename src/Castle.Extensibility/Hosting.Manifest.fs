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


    type Manifest() = 
        [<DefaultValue>] val mutable private _name : string
        [<DefaultValue>] val mutable private _version : Version
        [<DefaultValue>] val mutable private _exports : ExportDefinition seq
        [<DefaultValue>] val mutable private _imports : ImportDefinition seq
        
        member x.Name with get() = x._name and set(v) = x._name <- v
        member x.Version with get() = x._version and set(v) = x._version <- v
        member x.Exports with get() = x._exports and set(v) = x._exports <- v
        member x.Imports with get() = x._imports and set(v) = x._imports <- v

        // Dependencies : bundles or assemblies?

        // Behaviors : act-as definitions must be understood by the hosting/framework


    type IComposablePartDefinitionBuilder =
        interface 
            abstract member Build : unit -> ComposablePartDefinition
        end

    [<AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false); AllowNullLiteral>]
    type BundleComposerAttribute() = 
        class
            inherit Attribute()

        end