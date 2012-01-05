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