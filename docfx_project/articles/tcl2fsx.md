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

For the F# script, this is built to depend on .NET Core. You therefore need to use F# interactive for this rather tha the .NET standard version. The easiest way to do this is to use Visual Stdio Code with the Ionide Extension.

You can also run it on the command line like TCL but using:

```
dotnet fsi ImportWhite.fsx
```
