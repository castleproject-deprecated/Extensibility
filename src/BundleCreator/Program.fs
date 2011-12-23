
#nowarn "62"

open Microsoft.FSharp.Compatibility.OCaml

let name = ref ""
let targetDir = ref ""
let sourceDir = ref ""

open Arg
open Sys
open System.IO
open Ionic.Zip

let arglist = 
    [ 
        ("-name", Arg.String(fun v -> name := v), "Bundle name")
        ("-source", Arg.String(fun v -> sourceDir := v), "Bundle source folder (where the assemblies, manifest and etc are)")
        ("-target", Arg.String(fun v -> targetDir := v), "Bundle package destination folder")
    ]

if Sys.argv.Length <> 1 then
    Arg.parse arglist (fun _ -> ()) "Bundle Creator"
else 
    Arg.usage arglist "Bundle Creator"
    exit 1

// todo, should open the manifest, compute name + version and etc
// for now we just zip it

let zip = new ZipFile()
let entry = zip.AddDirectory(!sourceDir)
let targetFile = Path.Combine(!targetDir, !name + ".zip") 
if File.Exists targetFile then File.Delete targetFile
zip.Save(targetFile)


