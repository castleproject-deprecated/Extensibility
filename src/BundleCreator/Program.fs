#nowarn "62"

open Arg
open Sys
open System
open System.IO
open Ionic.Zip
open Microsoft.FSharp.Compatibility.OCaml
open Castle.Extensibility
open Castle.Extensibility.Hosting 
open System.Xml
open System.Xml.Linq
open Fake

let name = ref ""
let targetDir = ref ""
let sourceDir = ref ""
let isWeb = ref false
let verboseOut = ref false
let rawFileList : Ref<string[]> = ref Array.empty

let arglist = 
    [ 
        ("-name", Arg.String(fun v -> name := v), "Bundle name")
        ("-source", Arg.String(fun v -> sourceDir := v), "Bundle source folder (where the assemblies, manifest and etc are)")
        ("-target", Arg.String(fun v -> targetDir := v), "Bundle package destination folder")
        ("-filelist", Arg.String(fun v -> rawFileList := v.Split([|';'|], StringSplitOptions.RemoveEmptyEntries)), "A ';'-separated list of file names, relative to -source folder")
        ("-web", Arg.Set(isWeb), "Use to indicate -source points to a web folder (folder with /bin and artifacts)")
        ("-v", Arg.Set(verboseOut), "Verbose output")
    ]

if Sys.argv.Length <> 1 then
    Arg.parse arglist (fun _ -> ()) "Bundle Creator"
else 
    Arg.usage arglist "Bundle Creator"
    exit 1

printfn "Bundling %s..." !name

if Array.isEmpty !rawFileList then
    rawFileList := [|"*.*"|];

// First thing, we will load the assemblies in the specified path, 
// list all exportable/importable contracts and save them to manifest-generated.xml

let zip = new ZipFile()

// Sets up custom binder

let customBinder = new CustomBinder()
let bindingContext = customBinder.DefineBindingContext()
let fs1 = Files [!sourceDir] (!rawFileList |> List.ofArray) [] // |> ScanImmediately 

fs1 |> Seq.iter (fun f -> printfn "-- %s" f)

exit 0

if Array.isEmpty !rawFileList then
    bindingContext.LoadAssemblies(!sourceDir)   
    zip.AddDirectory(!sourceDir, "") |> ignore
else
    for file in !rawFileList do
        let file = file.Trim()
        // has wildcard?
        

        ()

    ()
    

exit 0

// Load types thru binder

let bundleTypes = 
    bindingContext.GetAllTypes() 
let contracts = BundlePartDefinitionBuilder.CollectBundleDefinitions(bundleTypes)

printfn "Imports for %s" !name
snd contracts |> Seq.iter (fun c -> printfn "- %s" c.ContractName)
printfn "Exports for %s" !name
fst contracts |> Seq.iter (fun c -> printfn "- %s" c.ContractName)

let targetGenManifestFile = Path.Combine(!sourceDir, "manifest-generated.xml")
if File.Exists targetGenManifestFile then File.Delete targetGenManifestFile

let fs = new FileStream(targetGenManifestFile, FileMode.CreateNew)
DefinitionsCacheWriter.write_manifest (new StreamWriter(fs)) (fst contracts) (snd contracts)
fs.Close()

// todo, should open the manifest, compute name + version and etc
// for now we just zip it



let targetFile = Path.Combine(!targetDir, !name + ".zip") 
if File.Exists targetFile then File.Delete targetFile
zip.Save(targetFile)

printfn "Done bundling %s" !name
