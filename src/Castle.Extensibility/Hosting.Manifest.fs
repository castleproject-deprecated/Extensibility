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
    open System.ComponentModel.Composition
    open System.ComponentModel.Composition.Hosting
    open System.ComponentModel.Composition.Primitives
    open Castle.Extensibility


    [<AllowNullLiteral>]
    type Manifest(name:string, version:Version, composer:ComposerSettings, deploymentPath:string) = 

        member x.Name = name
        member x.Version = version
        member x.Composer = composer
        member x.DeploymentPath = deploymentPath
        member internal x.HasCustomComposer = composer <> null && not (String.IsNullOrEmpty(composer.TypeName))
        
        (*
        member x.Exports with get() = x._exports and set(v) = x._exports <- v
        member x.Imports with get() = x._imports and set(v) = x._imports <- v
        member x.CustomComposer with get() = x._composer and set(v) = x._composer <- v

        // Dependencies : bundles or assemblies?
        // Behaviors : act-as definitions must be understood by the hosting/framework
        *)

    and [<AllowNullLiteral>] ComposerSettings(typename, parameters:string seq) = 
        member x.TypeName = typename
        member x.Parameters = parameters




