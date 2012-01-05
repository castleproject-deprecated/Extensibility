// Learn more about F# at http://fsharp.net

module Module1

    open System.Security
    open System.Reflection
    open System.Runtime.CompilerServices
    open System.Runtime.InteropServices

    [<assembly: AssemblyVersion("0.1.0.0")>]
    [<assembly: AssemblyFileVersion("0.1.0.0")>]
    [<assembly: AllowPartiallyTrustedCallers>]
    [<assembly: SecurityTransparentAttribute>]
    do
        // 
        ()
