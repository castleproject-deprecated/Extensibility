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
    type BindingContext() = 
        // let _asms = List<Assembly>()
        let _asms = HashSet<Assembly>()
        let _name2Asm = Dictionary<string, Assembly>()

        let load_assembly_guarded (file) = 
            try Assembly.LoadFile file with | ex -> null

        member x.Contains (asm:Assembly) = _asms.Contains asm

        member x.Name2Asms = _name2Asm

        member x.LoadAssemblies(folder:string) = 
            let files = Directory.GetFiles(folder, "*.dll")
            for file in files do
                let asm = load_assembly_guarded file
                if asm <> null then x.AddAssembly(asm)

        member x.AddAssembly(asm:Assembly) = 
            _name2Asm.[asm.FullName] <- asm
            _asms.Add asm |> ignore

        member x.GetAllTypes() = 
            _asms |> Seq.collect RefHelpers.guard_load_public_types |> Seq.filter (fun t -> t <> null)
        
        member x.GetType(name:string) = 
            if name.Contains(",") then 
                Type.GetType(name, false, false)
            else
                let find (asm:Assembly) = 
                    let typ = asm.GetType(name, false)
                    if typ <> null then Some(typ) else None
                match _asms |> Seq.tryPick find with
                | Some typ -> typ
                | None -> null

        interface IBindingContext with 

            member x.GetAllTypes() = x.GetAllTypes()
            member x.GetContextType(name) = x.GetType(name)
            


    [<AllowNullLiteral>] 
    type CustomBinder [<System.Security.SecurityCritical>] () = 
        let _contexts = List<_>()

        let resolve_asm (sender) (args:ResolveEventArgs) : Assembly = 
            if args.RequestingAssembly <> null then
                let find (ctx:BindingContext) : Assembly option = 
                    if ctx.Contains args.RequestingAssembly then
                        let res, asm = ctx.Name2Asms.TryGetValue(args.Name)
                        if res then Some(asm) else None
                    else
                        None
                let res = _contexts |> Seq.tryPick find
                match res with 
                  | Some asm -> 
                    Diagnostics.Debug.Write (sprintf "Resolved %O. Requesting assembly: %O" args.Name args.RequestingAssembly)
                    asm
                  | None -> 
                    Diagnostics.Debug.Write (sprintf "Could not find %O. Requesting assembly: %O" args.Name args.RequestingAssembly)
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
                





