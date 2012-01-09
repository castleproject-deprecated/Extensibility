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
    open Ionic.Zip


    [<System.Security.SecuritySafeCritical>]
    type BundleCatalog(dir:string) = 
        inherit ComposablePartCatalog()

        let mutable _fxServices : Dictionary<Type, string -> obj> = null
        let mutable _bindingContextFactory : unit -> BindingContext = fun _ -> null
        let mutable _behaviors : IBehavior seq = null

        static let build_manifest (dir) =
            let manifestPath = Path.Combine(dir, "manifest.xml") 
            if File.Exists(manifestPath) then
                use fs = File.OpenRead(manifestPath)
                ManifestReader.build_manifest fs dir
            else
                let name = DirectoryInfo(dir).Name
                Manifest(name, Version(0,0), null, dir)

        do
            if Directory.Exists(dir) then  
                for zipFile in Directory.GetFiles(dir, "*.zip") do
                    let bundleName = Path.GetFileNameWithoutExtension zipFile
                    let zip = new ZipFile(zipFile)
                    try
                        let bundleFolder = Path.Combine(dir, bundleName)
                        if Directory.Exists(bundleFolder) then
                            Directory.Delete( bundleFolder, true )
                        Directory.CreateDirectory bundleFolder |> ignore
                        zip.ExtractAll(bundleFolder, ExtractExistingFileAction.OverwriteSilently)
                    finally
                        zip.Dispose() 
                        

        let _parts = lazy (
                            // todo: assert we have a bindingContext 
                            let list = List<ComposablePartDefinition>()
                            if Directory.Exists(dir) then  
                                let dirs = Directory.GetDirectories(dir)
                                for f in dirs do
                                    let manifest = build_manifest(f)
                                    if manifest.CustomComposer <> null then
                                        list.Add (BundlePartDefinitionShim(f, manifest, _bindingContextFactory(), _fxServices, _behaviors))
                                    else 
                                        list.Add (BundlePartDefinition(f, manifest, _bindingContextFactory(), _fxServices, _behaviors))
                            list :> _ seq
                          )

        let select_candidates (cpd:ComposablePartDefinition) (imp:ImportDefinition) = 
            let exports = 
                cpd.ExportDefinitions 
                |> Seq.choose (fun e -> if e.ContractName = imp.ContractName then Some(e) else None)
            if not (Seq.isEmpty exports) then
                Some((cpd,exports))
            else
                None

        member x.Behaviors with get() = _behaviors and set(v) = _behaviors <- v
        member x.BindingContextFactory with get() = _bindingContextFactory and set(v) = _bindingContextFactory <- v
        member x.FrameworkServices with get() = _fxServices and set(v) = _fxServices <- v

        override x.Parts = _parts.Force().AsQueryable()
        override x.GetExports(impDef) = 
            let candidates = _parts.Force() |> Seq.choose (fun p -> select_candidates p impDef)

            seq { 
                for candidate in candidates do
                    for e in snd candidate do
                        yield (fst candidate, e)
            }

    [<System.Security.SecuritySafeCritical>]
    type HostingContainer (bundles:BundleCatalog seq, appCatalog:ComposablePartCatalog) = 
        let catalogs = seq {  yield appCatalog
                              yield! (bundles |> Seq.cast<ComposablePartCatalog>) }
        let _aggCatalogs = new AggregateCatalog(catalogs)
        let _container   = new CompositionContainer(_aggCatalogs, CompositionOptions.DisableSilentRejection)
        let _behaviors = List<IBehavior>()
        let _binder = new CustomBinder()
        let _type2Act = Dictionary<Type, string -> obj>()

        do
            for bundle in bundles do
                bundle.Behaviors <- _behaviors :> _ seq
                bundle.FrameworkServices <- _type2Act
                bundle.BindingContextFactory <- fun _ -> _binder.DefineBindingContext()

        new (bundleDir:string, appCatalog) = 
            let bundles = [|new BundleCatalog(bundleDir)|]
            new HostingContainer(bundles, appCatalog)
        
        member x.AddSupportedBehavior( behavior:IBehavior ) = 
            _behaviors.Add behavior 

        member x.AddFrameworkService<'T when 'T : null>( activatorFunc:Func<string, obj> ) = 
            _type2Act.Add(typeof<'T>, (fun name -> activatorFunc.Invoke(name)))

        member x.GetExportedValue() = _container.GetExportedValue()
        member x.GetExportedValue(name) = _container.GetExportedValue(name)
        member x.GetExportedValues() = _container.GetExportedValues()
        member x.GetExportedValues(name) = _container.GetExportedValues(name)
        member x.SatisfyImports(target:obj) = 
            _container.SatisfyImportsOnce(target)

        member x.Dispose() = 
            (x :> IDisposable).Dispose()

        interface IDisposable with 
    
            // [<System.Security.SecuritySafeCritical>]        
            member x.Dispose() = 
                _container.Dispose()
                _aggCatalogs.Dispose()
                (_binder :> IDisposable).Dispose()



