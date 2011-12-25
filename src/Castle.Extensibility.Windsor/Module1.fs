namespace Castle.Extensibility.Windsor

    open System
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
                let serv = fxContext.GetService()
                if serv == null then
                    container.Resolve()
                else
                    serv


    type WindsorComposer() = 
        interface IComposablePartDefinitionBuilder with 
            member x.Build(bindingCtx, exports, imports, manifest, fxContext) = 
                upcast WindsorPartDefinition(bindingCtx, exports, imports, manifest, fxContext)


    and WindsorPartDefinition(bindingCtx, exports, imports, manifest, fxContext) = 
        inherit ComposablePartDefinition()

        override x.ExportDefinitions = exports
        override x.ImportDefinitions = imports

        override x.CreatePart() = 
            upcast new WindsorPart(bindingCtx, exports, imports, manifest, fxContext)


    and WindsorPart(bindingCtx, exports, imports, manifest, fxContext) = 
        inherit ComposablePart()

        [<DefaultValue>] val mutable private _container : WindsorContainer 

        override x.ExportDefinitions = exports
        override x.ImportDefinitions = imports

        override x.Activate() = 
            x._container <- new WindsorContainer()
            let starters = 
                bindingCtx.GetAllTypes()
                |> Seq.filter (fun t -> not t.IsAbstract && typeof<IModuleStarter>.IsAssignableFrom(t))
            // runs all module starters
            for starterType in starters do
                let starter = Activator.CreateInstance starterType :?> IModuleStarter
                starter.Initialize(WindsorContext(x._container, fxContext))
            ()
            
        override x.GetExportedValue(expDefinition) = 
            let key = expDefinition.ContractName
            if x._container.Kernel.HasComponent(key) then
                x._container.Kernel.Resolve(key)
            else
                null

        override x.SetImport(impDef, exports) = 
            for export in exports do
                let expValue = export.Value
                x._container.Kernel.Register( 
                    Component.For( expValue.GetType() ).Named( impDef.ContractName ).Instance(expValue) ) 
                    |> ignore

        interface IDisposable with

            member x.Dispose() =
                if x._container <> null then
                    x._container.Dispose()
                    x._container <- null
    

