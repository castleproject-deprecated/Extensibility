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




    [<AllowNullLiteral>] 
    type CustomBinder() = 
        
        let resolve_asm (sender) (args:ResolveEventArgs) : Assembly = 
            if args.RequestingAssembly <> null then
                null
            else
                null

        let _eventHandler = ResolveEventHandler(resolve_asm)
         
        do
            AppDomain.CurrentDomain.add_AssemblyResolve _eventHandler

        member x.DefineBindingContext() = 
            BindingContext()

        interface IDisposable with 

            member x.Dispose() = 
                AppDomain.CurrentDomain.remove_AssemblyResolve _eventHandler
                ()

    and [<AllowNullLiteral>] 
        BindingContext() = 
        class
            let _asms = List<Assembly>()

            let load_assembly_guarded (file) = 
                try Assembly.LoadFile file with | ex -> null

            member x.LoadAssemblies(folder:string) = 
                let files = Directory.GetFiles(folder, "*.dll")
                for file in files do
                    let asm = load_assembly_guarded file
                    if asm <> null then x.AddAssembly(asm)

            member x.AddAssembly(asm:Assembly) = 
                _asms.Add(asm)

            member x.GetAllTypes() = 
                _asms |> Seq.collect RefHelpers.guard_load_types 
            
       end


    type internal DirectoryTypesLoaderGuarded(folder, bindingContext:BindingContext) = 
        let _types = List<Type>()

        let load_assembly_guarded (file:string) : Assembly = 
            try
                // let name = AssemblyName.GetAssemblyName(file);
                // let asm = Assembly.Load name
                let asm = Assembly.LoadFile file
                asm
            with | ex -> null

        do 
            let files = Directory.GetFiles(folder, "*.dll")
            for file in files do
                let asm = load_assembly_guarded file
                if asm <> null then bindingContext.AddAssembly(asm)
                
            _types.AddRange (bindingContext.GetAllTypes())
        
        member x.Types = _types :> _ seq


