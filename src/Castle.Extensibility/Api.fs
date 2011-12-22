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

namespace Castle.Extensibility

    open System
    open System.ComponentModel.Composition
    open System.ComponentModel.Composition.Primitives
    open System.ComponentModel.Composition.ReflectionModel

    [<AllowNullLiteral>]
    type ServiceTracker<'a when 'a : null>() = 
        class
            let mutable _ref : Lazy<'a> = null
            let _ev = Event<_>()
            
            [<Import(AllowDefault = true, AllowRecomposition = true)>]
            member x.Service with get() = _ref and set(v) = _ref <- v

            [<CLIEvent>]
            member this.Changed = _ev.Publish

            interface IPartImportsSatisfiedNotification with
                
                member x.OnImportsSatisfied() = 
                    _ev.Trigger(x, EventArgs.Empty)
                    
        end

    [<AbstractClass>]
    type ModuleContext() =
        class
            
            abstract member HasService<'a when 'a : null> : 'a -> bool

            abstract member GetService<'a when 'a : null>  : service:'a -> 'a 

            abstract member GetServiceTracker<'a when 'a : null> : service:'a -> ServiceTracker<'a>
                
            default x.GetServiceTracker(service) = ServiceTracker()
        end

    [<Interface>]
    type IModuleStarter = 
        interface
            
            abstract member Initialize : ctx:ModuleContext -> unit

            abstract member Terminate : unit -> unit

        end