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

    [<AllowNullLiteral>]
    type CompositeComposerBuilder(parameters:string seq) = 
        
        do 
            ()

        interface IComposablePartDefinitionBuilder with
            
            member x.Build(context, exports, imports, manifest, frameworkCtx, behaviors) = 
                let cpds = 
                    seq {             
                        for composer in parameters do 
                            let compType = context.GetContextType(composer)
                            let builder = Activator.CreateInstance( compType ) :?> IComposablePartDefinitionBuilder
                            let cpd = builder.Build(context, exports, imports, manifest, frameworkCtx, behaviors)
                            yield cpd 
                    }
                upcast CompositePartDefinition(cpds, context, exports, imports, manifest, frameworkCtx, behaviors)

                
    and [<AllowNullLiteral>] 
        CompositePartDefinition(cpds, context, exports, imports, manifest, frameworkCtx, behaviors) = 
        inherit ComposablePartDefinition()

        override x.ExportDefinitions = exports
        override x.ImportDefinitions = imports

        override x.CreatePart() =
            let parts = seq { for cpd in cpds do yield cpd.CreatePart() } 
            upcast CompositePart(parts, context, exports, imports, manifest, frameworkCtx, behaviors)


    and [<AllowNullLiteral>]
        CompositePart(parts, context, exports, imports, manifest, frameworkCtx, behaviors) = 
        inherit ComposablePart() 

        let get_exp_value (def) (part:ComposablePart) = 
            let value = part.GetExportedValue(def)
            if value <> null then Some(value) else None

        override x.ExportDefinitions = exports
        override x.ImportDefinitions = imports

        override x.Activate() = 
            parts |> Seq.iter (fun p -> p.Activate())
            
        override x.GetExportedValue(expDef) = 
            match parts |> Seq.tryPick (fun p -> get_exp_value expDef p ) with
            | Some value -> value
            | None -> null
        
        override x.SetImport(impDef, exports) = 
            // this is likely to be too naive as an implementation
            parts |> Seq.iter (fun p -> p.SetImport(impDef, exports))
            
