// Usage: dotnet fsi popTree.fsx
#r @"D:\GitHub\ScincNet\rel\bin\ScincFuncs.dll"
open ScincFuncs

// Open the database:
let basename = @"C:\Users\phil\Documents\ScincNet\bases_extra\Caissabase_2020_11_14"

if (Base.Open(basename)<>1) then
    printfn "Error opening database %s" basename

if (Base.Isreadonly()) then
    printfn "Error database %s is read only" basename

let basenum = Base.Current()
let ply = 20

#time "on"

if (Tree.Populate(ply,basenum,200u)<>0) then
    printfn "Error populating tree for %s" basename

if (Tree.Write(basenum)<>0) then
    printfn "Error writing tree for %s" basename

Base.Close()|>ignore

#time "off"