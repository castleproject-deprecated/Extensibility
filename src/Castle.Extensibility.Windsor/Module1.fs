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

        //[<DefaultValue>] val mutable private _container : WindsorContainer 
        let _container = lazy ( new WindsorContainer() ) 

        member internal x.Kernel = _container.Force().Kernel

        override x.ExportDefinitions = exports
        override x.ImportDefinitions = imports

        override x.Activate() = 
            let cont = _container.Force()
            let starters = 
                bindingCtx.GetAllTypes()
                |> Seq.filter (fun t -> not t.IsAbstract && typeof<IModuleStarter>.IsAssignableFrom(t))
            // runs all module starters
            for starterType in starters do
                let starter = Activator.CreateInstance starterType :?> IModuleStarter
                starter.Initialize(WindsorContext(cont, fxContext))
            
        override x.GetExportedValue(expDefinition) = 
            let key = expDefinition.ContractName
            if x.Kernel.HasComponent(key) then
                x.Kernel.Resolve(key)
            else
                null

        override x.SetImport(impDef, exports) = 
            for export in exports do
                let expValue = export.Value
                x.Kernel.Register( 
                    Component.For( expValue.GetType() ).Named( impDef.ContractName ).Instance(expValue) ) 
                    |> ignore

        interface IDisposable with

            member x.Dispose() =
                if _container.IsValueCreated then
                    _container.Force().Dispose()
    

