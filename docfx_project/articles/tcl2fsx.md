# Converting TCL to FSX

This article shows how a sample TCL script can be converted to an F# version. 

If you follow this article you will see some examples of how to use the provided API functions.

The full sample scipts can be found here:

- TCL script [ImportWhite.tcl](https://github.com/pbbwfc/ScincNet/blob/main/scripts/importWhite.tcl)
- F# script [ImportWhite.fsx](https://github.com/pbbwfc/ScincNet/blob/main/scripts/importWhite.fsx)

## Usage

For the TCL script it needs to be run using the SCID interpreter: **tcscid.exe**. 

If you navigate to the location of the script then you can run it in a command prompt using:

```
"D:\Scid vs PC-4.21\bin\tcscid" ImportWhite.tcl
```

For the F# script, this is built to depend on .NET Core. You therefore need to use F# interactive for this rather tha the .NET Standard version. The easiest way to do this is to use Visual Studio Code with the Ionide Extension.

You can also run it on the command line, like TCL, but using:

```
dotnet fsi ImportWhite.fsx
```
To use the .NET version of the API in the F# script you need to reference **ScincFuncs.dll** and open the namespace **ScincFuncs** at the start of the script. We are also going to use file operations and so also need to open **System.IO**. Thus we use:

```fsharp
#r @"D:\GitHub\ScincNet\debug\bin\ScincFuncs.dll"
open ScincFuncs
open System.IO
```

## Step 1 - Create the Database

In TCL, we first create the database. We then open the database and print an error message if this fails. We then check that it is not read only. We then get the number of games in the database (which should be 0) and then print a message to the console.

This is the code:

```tcl
set basename D:/tmp/WhiteTCL
sc_base create $basename

if {[catch {sc_base open $basename} result]} {
    puts stderr "Error opening database \"$basename\": $result"
    exit 1
}
if {[sc_base isReadOnly]} {
    puts stderr "Error: database \"$basename\" is read-only."
    exit 1
}
set num [sc_base numGames] 

puts "number of games: $num"
```

This should produce this output:

```console
number of games: 0
```

The correspondong F~ code is:

```fsharp
let basename = @"D:/tmp/WhiteFSX"
Base.Create(basename)|>ignore

if (Base.Open(basename)<0) then
    printfn "Error opening database %s" basename

if (Base.Isreadonly()) then
    printfn "Error database %s is read only" basename

let num = Base.NumGames()

printfn "number of games: %i" num
```

This produces this output:

```console
number of games: 0
```