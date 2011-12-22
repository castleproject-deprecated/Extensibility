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


    type BundleCatalog(dir:string) = 
        inherit ComposablePartCatalog()

        let mutable _bindingContext : BindingContext = null

        let build_manifest(dir) =
            let manifestPath = Path.Combine(dir, "manifest.xml") 
            if File.Exists(manifestPath) then
                use fs = File.OpenRead(manifestPath)
                ManifestReader.build_manifest fs
            else
                let name = DirectoryInfo(dir).Name
                Manifest(name, Version(0,0), null)

        let _parts = lazy (
                            // todo: assert we have a bindingContext 
                            let list = List<ComposablePartDefinition>()
                            let dirs = Directory.GetDirectories(dir)
                            for f in dirs do
                                let manifest = build_manifest(f)
                                if manifest.CustomComposer <> null then
                                    list.Add (BundlePartDefinitionShim(f, manifest, _bindingContext))
                                else 
                                    list.Add (BundlePartDefinition(f, manifest, _bindingContext))
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

        member x.BindingContext with get() = _bindingContext and set(v) = _bindingContext <- v

        override x.Parts = _parts.Force().AsQueryable()
        override x.GetExports(impDef) = 
            let candidates = _parts.Force() |> Seq.choose (fun p -> select_candidates p impDef)

            seq { 
                for candidate in candidates do
                    for e in snd candidate do
                        yield (fst candidate, e)
            }


    type HostingContainer (bundles:BundleCatalog seq, appCatalog:ComposablePartCatalog) = 
        let catalogs = seq {  yield appCatalog
                              yield! (bundles |> Seq.cast<ComposablePartCatalog>) }
        let _aggCatalogs = new AggregateCatalog(catalogs)
        let _container   = new CompositionContainer(_aggCatalogs, CompositionOptions.DisableSilentRejection)
        let _binder = new CustomBinder()

        do
            for bundle in bundles do
                bundle.BindingContext <- _binder.DefineBindingContext()

        new (bundleDir:string, appCatalog) = 
            let bundles = [|new BundleCatalog(bundleDir)|]
            new HostingContainer(bundles, appCatalog)
        
        member x.GetExportedValue() = _container.GetExportedValue()
        member x.GetExportedValue(name) = _container.GetExportedValue(name)

        interface IDisposable with 
            
            member x.Dispose() = 
                _container.Dispose()
                _aggCatalogs.Dispose()
                (_binder :> IDisposable).Dispose()



