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
    open System.Collections.Generic
    open System.ComponentModel.Composition
    open System.ComponentModel.Composition.Primitives
    open System.ComponentModel.Composition.ReflectionModel
    open System.Runtime.Remoting.Messaging

    (*
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
    *)

    [<AllowNullLiteral>]
    type IBindingContext = 
        interface
            abstract member GetAllTypes : unit -> Type seq
            abstract member GetContextType : string -> Type
        end

    [<AbstractClass; AllowNullLiteral>]
    type ModuleContext() =
        class
            
            abstract member HasService<'T when 'T : null> : unit -> bool

            abstract member GetService<'T when 'T : null> : unit -> 'T 

            // abstract member GetServiceTracker<'T when 'T : null> : unit -> ServiceTracker<'T>
                
            // default x.GetServiceTracker() = ServiceTracker()
        end

    [<Interface>]
    type IModuleStarter = 
        interface
            
            abstract member Initialize : ctx:ModuleContext -> unit

            abstract member Terminate : unit -> unit

        end


    type UnitOfWorkEventArgs(unit:UnitOfWork) = 
        inherit EventArgs()
        member x.UnitOfWork = unit

    and [<AllowNullLiteral>]
        UnitOfWork() as self = 
        let _disposed = ref false
        let _closedF = ref false
        let _process = new DelegateEvent<EventHandler<UnitOfWorkEventArgs>>()
        let _closed  = new DelegateEvent<EventHandler<UnitOfWorkEventArgs>>()
        let _aborted = new DelegateEvent<EventHandler<UnitOfWorkEventArgs>>()
        let mutable _parent : UnitOfWork = null
        let _dict = lazy Dictionary<string,obj>()

        let ensure_not_disposed() = 
            if !_disposed then raise(ObjectDisposedException("UnitOfWork"))

        static let bucketName = "bundle.context.unitofwork"
        let mutable _stack : Stack<_> = null

        do 
            let existing = CallContext.GetData(bucketName)
            if existing <> null then 
                _stack <- existing :?> Stack<UnitOfWork>
                if _stack.Count <> 0 then 
                    _parent <- _stack.Peek()
                _stack.Push self |> ignore
            else
                _stack <- Stack<UnitOfWork>([self])
                CallContext.SetData(bucketName, _stack)

        static member Current : UnitOfWork = 
            let stack = CallContext.GetData(bucketName) :?> Stack<UnitOfWork>
            if stack <> null 
            then if stack.Count = 0 then null else stack.Peek()
            else null

        member x.Context = _dict.Force()
        member x.Parent = _parent

        [<CLIEvent>]
        member x.ProcessCalled = _process.Publish
        [<CLIEvent>]
        member x.Closed = _closed.Publish
        [<CLIEvent>]
        member x.Aborted = _aborted.Publish

        member x.Abort() = 
            ensure_not_disposed()
            _aborted.Trigger([|x;UnitOfWorkEventArgs(x)|])

        member x.Process() = 
            ensure_not_disposed()
            _process.Trigger([|x;UnitOfWorkEventArgs(x)|])

        member x.Close() = 
            ensure_not_disposed()
            if !_closedF then raise (InvalidOperationException("Already closed"))
            _closedF := true
            _closed.Trigger([|x;UnitOfWorkEventArgs(x)|])
            let unit = _stack.Pop()
            if unit != x then
                raise (InvalidOperationException("Expected current object instance on stack, but found other"))

        interface IDisposable with
            member x.Dispose() = 
                x.Close()
                _disposed := true
    

