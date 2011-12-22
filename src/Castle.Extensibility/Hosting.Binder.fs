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
        
        let _contexts = List<_>()
        let _asm2Ctxs = Dictionary<Assembly, List<BindingContext>>()

        let resolve_asm (sender) (args:ResolveEventArgs) : Assembly = 
            
            if args.RequestingAssembly <> null then
                let res, ctxs = _asm2Ctxs.TryGetValue args.RequestingAssembly
                if res then
                    let find (ctx:BindingContext) = 
                        let res, asm = ctx.Name2Asms.TryGetValue(args.Name)
                        if res then Some(asm) else None
                    match ctxs |> Seq.tryPick find with 
                    | Some asm -> asm
                    | None -> null
                else 
                    null
            else
                null

        let _eventHandler = ResolveEventHandler(resolve_asm)
         
        do
            AppDomain.CurrentDomain.add_AssemblyResolve _eventHandler

        member x.DefineBindingContext() = 
            let ctx = BindingContext()
            _contexts.Add ctx
            ctx

        interface IDisposable with 

            member x.Dispose() = 
                AppDomain.CurrentDomain.remove_AssemblyResolve _eventHandler
                


    and [<AllowNullLiteral>] 
        BindingContext() = 
        class
            let _asms = List<Assembly>()
            let _name2Asm = Dictionary<string, Assembly>()

            let load_assembly_guarded (file) = 
                try Assembly.LoadFile file with | ex -> null

            member internal x.Name2Asms = _name2Asm

            member x.LoadAssemblies(folder:string) = 
                let files = Directory.GetFiles(folder, "*.dll")
                for file in files do
                    let asm = load_assembly_guarded file
                    if asm <> null then x.AddAssembly(asm)

            member x.AddAssembly(asm:Assembly) = 
                _name2Asm.[asm.FullName] <- asm
                _asms.Add(asm)

            member x.GetAllTypes() = 
                _asms |> Seq.collect RefHelpers.guard_load_types 
            
            member x.GetType(name) = 
                let find (asm:Assembly) = 
                    let typ = asm.GetType(name, false)
                    if typ <> null then Some(typ) else None
                match _asms |> Seq.tryPick find with
                | Some typ -> typ
                | None -> null
       end



