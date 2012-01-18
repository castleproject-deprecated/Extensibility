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

[<AutoOpen>]
module Helpers
    
    open System
    open System.Collections.Generic
    open System.Linq
    open System.Reflection
    open System.Dynamic
    open System.Security
    open System.Runtime.CompilerServices
    open System.Runtime.InteropServices

    [<assembly: AssemblyVersion("0.1.0.0")>]
    [<assembly: AssemblyFileVersion("0.1.0.0")>]
    [<assembly: AllowPartiallyTrustedCallers>]
    [<assembly: SecurityTransparentAttribute>]
    do
        // 
        ()


    let inline (==) a b = Object.ReferenceEquals(a, b)
    let inline (!=) a b = not (Object.ReferenceEquals(a, b))

    let inline (<?>) (a:bool) (b:bool) = 
        if a = true then a else b 

    let inline (<!>) (a:'a) (b:'a) = 
        if a <> null then a else b 