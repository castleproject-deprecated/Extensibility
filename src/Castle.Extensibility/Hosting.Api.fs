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


    
    [<System.Runtime.InteropServices.Guid("8b4f1335-9017-4eac-887e-06add66c7778")>]
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

    

