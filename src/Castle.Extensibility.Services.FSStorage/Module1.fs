// Learn more about F# at http://fsharp.net

namespace Castle.Extensibility.Services.FSStorage
    
    open System
    open System.IO
    open Castle.Extensibility
    open Castle.Extensibility.Services.Storage
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


    type FSStorageService(bundleName:string, safeStorageFolder:string) = 

        let getStorage(scope:string) : IStorage = 
            let path = Path.Combine (safeStorageFolder, Path.Combine(bundleName, scope))
            upcast FSStorage(path)

        interface IStorageService with

            member x.GetTempFile() = Path.GetTempFileName()
            
            member x.GetStorage() = 
                getStorage "default"

            member x.GetStorage(scope:string) = 
                getStorage scope


    and FSStorage(folder:string) = 
        
        interface IStorage with 

            member x.Files = 
                seq { yield! Directory.GetFiles(folder) }
            
            member x.Folder = folder

            member x.Exists(name) = 
                File.Exists (Path.Combine(folder, name))

