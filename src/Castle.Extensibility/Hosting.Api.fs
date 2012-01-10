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
    open System.ComponentModel.Composition
    open System.ComponentModel.Composition.Hosting
    open System.ComponentModel.Composition.Primitives
    open Castle.Extensibility


    [<AllowNullLiteral>]
    type IBehavior = 
        interface
            abstract member GetBehaviorExports : imports:ImportDefinition seq * 
                                                 exports:ExportDefinition seq * manifest:Manifest -> Export seq
        end


    
    // [<TypeEquivalence; Guid>]
    [<AllowNullLiteral>]
    type IComposablePartDefinitionBuilder =
        interface 
            abstract member Build : ctx:IBindingContext * 
                                    exports:ExportDefinition seq * 
                                    imports:ImportDefinition seq * 
                                    manifest:Manifest * 
                                    frameworkCtx:ModuleContext * 
                                    behaviors:IBehavior seq -> ComposablePartDefinition
        end

    [<AllowNullLiteral>]
    type CompositeComposerBuilder(parameters:string seq) = 
        
        interface IComposablePartDefinitionBuilder with
            
            member x.Build(context, exports, imports, manifest, frameworkCtx, behaviors) = 
                let composers = 
                    seq {             
                        for composer in parameters do 
                            let compType = context.GetContextType(composer)
                            let compInstance = Activator.CreateInstance( compType )
                            // compInstance.
                            yield compInstance 
                    }
                upcast CompositePartDefinition(composers, context, exports, imports, manifest, frameworkCtx, behaviors)

                
    and [<AllowNullLiteral>] 
        CompositePartDefinition(composers, context, exports, imports, manifest, frameworkCtx, behaviors) = 
        inherit ComposablePartDefinition()

        override x.ExportDefinitions = exports
        override x.ImportDefinitions = imports

        override x.CreatePart() = 
            upcast CompositePart(composers, context, exports, imports, manifest, frameworkCtx, behaviors)


    and [<AllowNullLiteral>]
        CompositePart(composers, context, exports, imports, manifest, frameworkCtx, behaviors) = 
        inherit ComposablePart() 

        override x.ExportDefinitions = exports
        override x.ImportDefinitions = imports

        override x.Activate() = 
            ()

        override x.GetExportedValue(expDef) = 
            null
        
        override x.SetImport(impDef, exports) = 
            ()


