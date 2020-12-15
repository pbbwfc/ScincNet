// Usage: dotnet fsi importWhite.fsx
#r @"D:\GitHub\ScincNet\debug\bin\ScincFuncs.dll"
open ScincFuncs
open System.IO

// Open the database:
let basename = @"D:/tmp/WhiteFSX"
Base.Create(basename)|>ignore

if (Base.Open(basename)<0) then
    printfn "Error opening database %s" basename

if (Base.Isreadonly()) then
    printfn "Error database %s is read only" basename

let num = Base.NumGames()

printfn "number of games: %i" num

let fol = "D:/tmp/"
let pgns = ["Benko.pgn"; "Benoni.pgn"; "Budapest.pgn"; "Dutch.pgn"; "Grunfeld.pgn"; "KingsIndian.pgn"; "OldIndian.pgn"; "QGA.pgn"; "QGDmain.pgn"; "QGDtarr.pgn"; "QGDtri.pgn"; "QGDunus.pgn"; "Slav.pgn"]

let doimp pgn =
    let pgnfile = Path.Combine(fol,pgn)
    let mutable num = 0
    let mutable msgs = ""
    if (Base.Import(&num,&msgs,pgnfile)<>0) then
        printfn "Error importing: %s" pgnfile
    printfn "Imported %i games from %s" num pgnfile
    if msgs="" then
        printfn "There were no PGN errors or warnings."
    else
        printfn "PGN errors/warnings:"
        printfn "%s" msgs

pgns|>List.iter doimp

Base.Close()|>ignore


// now classify
// Open the ECO file:
let ecofile = "D:/tmp/scid.eco"
if Eco.Read(ecofile)<>0 then
    printfn "Error reading ECO file"

if (Base.Open(basename)<0) then
    printfn "Error opening database %s" basename

if (Base.Isreadonly()) then
    printfn "Error database %s is read only" basename

let mutable msgs = ""
Eco.Base(&msgs)|>ignore
printfn "Classifying games...\n%s" msgs

Base.Close()|>ignore

//now strip comments
// Open the database:
if (Base.Open(basename)<0) then
    printfn "Error opening database %s" basename

if (Base.Isreadonly()) then
    printfn "Error database %s is read only" basename

for i=1 to Base.NumGames() do
    if ScidGame.Load(uint(i))<>0 then
        printfn "Error: could not load game number %i" i
    ScidGame.StripComments()|>ignore
    ScidGame.Save(uint(i))|>ignore

Base.Close()|>ignore

//#now add names

//set nms [list "Benko" "Benoni" "Budapest" "Dutch" "Grunfeld" "Kings Indian" "Old Indian" "QGA" "QGD main" "QGD tarrasch" "QGD triangle" "QGD unusual" "Slav"]

//# Open the database:
//if {[catch {sc_base open $basename} result]} {
//    puts stderr "Error opening database \"$basename\": $result"
//    exit 1
//}
//if {[sc_base isReadOnly]} {
//    puts stderr "Error: database \"$basename\" is read-only."
//    exit 1
//}

//for {set i 1} {$i <= [sc_base numGames]} {incr i} {
//    if {[catch { sc_game load $i }]} {
//        puts "Error: could not load game number $i"
//        exit 1
//    }
//    set nm [lindex $nms [expr $i-1]]
//    sc_game tags set -white $nm

//    sc_game save $i
//}
//sc_base close
