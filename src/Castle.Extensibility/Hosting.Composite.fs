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
    open System.Xml
    open System.Xml.Linq
    open System.ComponentModel.Composition
    open System.ComponentModel.Composition.Hosting
    open System.ComponentModel.Composition.Primitives
    open Castle.Extensibility

    type private ExportComparer private () = 
        static member Instance = ExportComparer()
        interface IEqualityComparer<ExportDefinition> with 
            member x.GetHashCode(e) = e.ContractName.GetHashCode()
            member x.Equals(e1, e2) = 
                String.CompareOrdinal(e1.ContractName, e2.ContractName) = 0
    type private ImportComparer private () = 
        static member Instance = ImportComparer()
        interface IEqualityComparer<ImportDefinition> with
            member x.GetHashCode(e) = e.ContractName.GetHashCode()
            member x.Equals(e1, e2) = 
                String.CompareOrdinal(e1.ContractName, e2.ContractName) = 0

    [<AllowNullLiteral>]
    type CompositeComposerBuilder(parameters:string seq) = 
        
        interface IComposablePartDefinitionBuilder with
            
            member x.Build(context, exports, imports, manifest, frameworkCtx, behaviors) = 
                let cpds = 
                    parameters 
                    |> Seq.map (fun composer -> 
                                    let compType = context.GetContextType(composer)
                                    let args : string [] = [||]
                                    let builder = Activator.CreateInstance( compType, args ) :?> IComposablePartDefinitionBuilder
                                    builder.Build(context, exports, imports, manifest, frameworkCtx, behaviors) )
                    |> Seq.toList
                               
                upcast CompositePartDefinition(cpds, context, exports, imports, manifest, frameworkCtx, behaviors)

                
    and [<AllowNullLiteral>] 
        CompositePartDefinition(cpds, context, exports, imports, manifest, frameworkCtx, behaviors) = 
        inherit ComposablePartDefinition()

        override x.ExportDefinitions = exports
        override x.ImportDefinitions = imports

        override x.CreatePart() =
            upcast CompositePart(cpds, context, exports, imports, manifest, frameworkCtx, behaviors)


    and [<AllowNullLiteral>]
        CompositePart(cpds, context, exports, imports, manifest, frameworkCtx, behaviors) = 
        inherit ComposablePart() 

        let parts = 
            cpds 
            |> Seq.map ( fun cpd -> cpd.CreatePart() ) 
            |> Seq.toList

        let importsState : (ComposablePart * ImportDefinition * Ref<bool>) list =
            seq {
                   for p in parts do
                       for im in p.ImportDefinitions do
                           yield (p, im, ref false)
                } |> Seq.toList

        let get_exp_value (def) (part:ComposablePart) = 
            let value = part.GetExportedValue(def)
            if value <> null then Some(value) else None

        override x.ExportDefinitions = exports
        override x.ImportDefinitions = imports

        override x.Activate() = 

            // check if there's any import that we could potentially satisfy within the parts we own
            importsState 
                |> Seq.filter (fun (_,_,r) -> !r = false) 
                |> Seq.iter 
                    (
                      fun (p,imp,r) -> 
                        (
                            for cand in parts do
                                let exports = 
                                    cand.ExportDefinitions
                                    |> Seq.filter (fun e -> e.ContractName = imp.ContractName && imp.IsConstraintSatisfiedBy(e))
                                if not (Seq.isEmpty exports) then
                                    let exp = Seq.head exports
                                    let valueGetter = fun _ -> cand.GetExportedValue( exp )
                                    // needs fix:
                                    // CPD/CP should only expose the subset of exports that they can satisfy
                                    // if value <> null then 
                                    p.SetImport(imp, [System.ComponentModel.Composition.Primitives.Export(exp, valueGetter)] )
                            ()
                        ) 
                    )

            // we should activate the ones without pending imports
            parts |> Seq.iter (fun p -> p.Activate())

            
        override x.GetExportedValue(expDef) = 
            // Shouldnt this behave as an aggregator too?
            match parts |> Seq.tryPick (fun p -> get_exp_value expDef p ) with
            | Some value -> value
            | None -> null
        

        override x.SetImport(impDef, exports) = 
            if not (Seq.isEmpty exports) then
                importsState 
                |> Seq.filter (fun (_,imp,_) -> imp == impDef) 
                |> Seq.iter (fun (_,_,r) -> r := true)
            parts 
                // for the parts that import this impDef
                |> Seq.filter (fun p -> p.ImportDefinitions.Contains(impDef, ImportComparer.Instance) ) 
                // call set import
                |> Seq.iter (fun p -> p.SetImport(impDef, exports) )
            
