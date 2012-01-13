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

    type BundlePartDefinitionShim private (exports, imports, manifest:Manifest, bindingContext, fxServices, behaviors) as this = 
        inherit BundlePartDefinitionBase(exports, imports)

        [<DefaultValue>] val mutable private _customComposer : IComposablePartDefinitionBuilder

        let mutable _innerCpd : ComposablePartDefinition = null

        new (folder:string, manifest, bindingContext:BindingContext, fxServices, behaviors) = 
            bindingContext.LoadAssemblies(folder)
            let types = bindingContext.GetAllTypes()
            let result = BundlePartDefinitionBase.CollectBundleDefinitions types
            let exports = fst result
            let imports = snd result
            BundlePartDefinitionShim(exports, imports, manifest, bindingContext, fxServices, behaviors)

        do
            let settings = manifest.Composer
            let customComType = 
                bindingContext.GetType(settings.TypeName)
            let args : obj[] = 
                if Seq.isEmpty settings.Parameters then [||] else [|settings.Parameters|]

            this._customComposer <- Activator.CreateInstance(customComType, args) :?> IComposablePartDefinitionBuilder
            let frameworkCtx = FrameworkContext(fxServices, manifest.Name)
            _innerCpd <- this._customComposer.Build(bindingContext, this.ExportDefinitions, this.ImportDefinitions, manifest, frameworkCtx, behaviors)

        // override x.ExportDefinitions = _innerCpd.ExportDefinitions
        // override x.ImportDefinitions = _innerCpd.ImportDefinitions
        override x.Metadata = _innerCpd.Metadata
        override x.CreatePart() = _innerCpd.CreatePart()


