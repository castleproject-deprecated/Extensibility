namespace Castle.Extensibility.Windsor

    open System
    open System.ComponentModel.Composition
    open System.ComponentModel.Composition.Primitives
    open System.ComponentModel.Composition.Hosting
    open Castle.Windsor
    open Castle.Extensibility.Hosting


    type WindsorComposer() = 
                
        interface IComposablePartDefinitionBuilder with 
            member x.Build(types, exports, imports) = 
                upcast WindsorPartDefinition(exports, imports)


    and WindsorPartDefinition(exports, imports) = 
        inherit ComposablePartDefinition()

        override x.ExportDefinitions = exports
        override x.ImportDefinitions = imports

        override x.CreatePart() = 
            upcast new WindsorPart(exports, imports)


    and WindsorPart(exports, imports) = 
        inherit ComposablePart()

        override x.ExportDefinitions = exports
        override x.ImportDefinitions = imports

        override x.Activate() = 
            ()
            
        override x.GetExportedValue(expDefinition) = 
            null

        override x.SetImport(impDef, exports) = 
            ()

        interface IDisposable with

            member x.Dispose() =
                ()

