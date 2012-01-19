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

namespace Castle.Extensibility.Windsor

    open System
    open System.Collections.Generic
    open System.ComponentModel.Composition
    open System.ComponentModel.Composition.Primitives
    open System.ComponentModel.Composition.Hosting
    open Castle.Windsor
    open Castle.MicroKernel.Registration
    open Castle.Extensibility
    open Castle.Extensibility.Hosting


    type WindsorContext(container:WindsorContainer, fxContext:ModuleContext) = 
        inherit ModuleContext()

        override x.HasService<'T when 'T : null> () = 
            if typeof<IWindsorContainer> = typeof<'T> then
                true
            else
                fxContext.HasService() <?> container.Kernel.HasComponent(typeof<'T>)

        override x.GetService<'T when 'T : null> () =
            if typeof<IWindsorContainer> = typeof<'T> then
                container |> box :?> 'T
            else
                let serv = fxContext.GetService<'T>()
                if serv == null then
                    container.Resolve()
                else
                    serv


    type WindsorComposer() = 
        interface IComposablePartDefinitionBuilder with 
            member x.Build(bindingCtx, exports, imports, manifest, fxContext, behaviors) = 
                let filteredImports = imports |> Seq.filter (fun i -> i.ContractName <> "WindsorContainer")
                upcast WindsorPartDefinition(bindingCtx, exports, filteredImports, manifest, fxContext, behaviors)


    and WindsorPartDefinition(bindingCtx, exports, imports, manifest, fxContext, behaviors) = 
        inherit ComposablePartDefinition()

        override x.ExportDefinitions = exports
        override x.ImportDefinitions = imports

        override x.CreatePart() = 
            upcast new WindsorPart(bindingCtx, exports, imports, manifest, fxContext, behaviors)


    and WindsorPart(bindingCtx, exports, imports, manifest, fxContext, behaviors) = 
        inherit ComposablePart()

        let container_export_meta = 
            let dict = Dictionary<string,obj>()
            dict.[CompositionConstants.ExportTypeIdentityMetadataName] <- AttributedModelServices.GetTypeIdentity(typeof<IWindsorContainer>) |> box
            dict
        let container_export = ExportDefinition("WindsorContainer", container_export_meta)
        let exports_with_container = Seq.append exports [container_export]
        let _activated = ref false
        let _exportsToRegister = List<_>()

        let add_export (export:Export, container:WindsorContainer) = 
            let expValue = export.Value
            let def = export.Definition
            let reg = Component.For( expValue.GetType() ).Named( def.ContractName ).Instance(expValue)
            container.Register( reg ) |> ignore

        let mutable _container : WindsorContainer = null

        let run_starters () = 
            let starters = 
                bindingCtx.GetAllTypes()
                |> Seq.filter (fun t -> not t.IsAbstract && typeof<IModuleStarter>.IsAssignableFrom(t))

            // runs all module starters
            for starterType in starters do
                let starter = Activator.CreateInstance starterType :?> IModuleStarter
                starter.Initialize(WindsorContext(_container, fxContext))

        member internal x.Container = 
            if _container = null then 
                _container <- new WindsorContainer()
                for behavior in behaviors do
                    behavior.GetBehaviorExports( imports, exports, manifest ) 
                        |> Seq.iter (fun exp -> add_export (exp, _container) )
            _container

        override x.ExportDefinitions = exports_with_container
        override x.ImportDefinitions = imports

        override x.Activate() = 
            if !_activated then raise(InvalidOperationException("Already activated"))

            _activated := true
            let cont = x.Container

            // run all pending registrations (see SetImport below) 
            _exportsToRegister |> Seq.iter( fun f -> f() ) 
            _exportsToRegister.Clear()

            run_starters()
            
            
        override x.GetExportedValue(expDefinition) = 
            if expDefinition.ContractName = container_export.ContractName then
                x.Container |> box
            else 
                let key = expDefinition.ContractName
                if x.Container.Kernel.HasComponent(key) then
                    x.Container.Resolve(key)
                else
                    null

        override x.SetImport(impDef, exports) = 
            let addExport = fun _ ->
                for export in exports do
                    let expValue = export.Value
                    let contractType = 
                        let res, ctype = impDef.Metadata.TryGetValue("_TypeIdentityType")
                        if res then ctype :?> Type else expValue.GetType()
                    if not (x.Container.Kernel.HasComponent(impDef.ContractName)) then 
                        if expValue <> null then 
                            x.Container.Register( 
                                Component.For( contractType ).Named( impDef.ContractName ).Instance(expValue) ) |> ignore
                        else 
                            System.Diagnostics.Debug.WriteLine (sprintf "Evaluation of an Export returned a null value. Investigate? Contract: %s" impDef.ContractName)
                    else
                        System.Diagnostics.Debug.WriteLine (sprintf "Skipping registration of %s since Windsor already has a component with the same key. Duplicated import definitions?" impDef.ContractName)
        
            if !_activated then 
                addExport()
            else 
                _exportsToRegister.Add addExport 


        interface IDisposable with

            member x.Dispose() =
                ()

    

