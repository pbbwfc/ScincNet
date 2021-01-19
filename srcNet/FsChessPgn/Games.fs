namespace FsChessPgn

open FsChess
open System.IO
open System.Text

module Games =

    let ReadFromStream(stream : Stream) = 
        let sr = new StreamReader(stream)
        let db = RegParse.AllGamesRdr(sr)
        db

    let ReadSeqFromFile(file : string) = 
        let stream = new FileStream(file, FileMode.Open)
        let db = ReadFromStream(stream)
        db

    let ReadFromFile(file : string) = 
        let stream = new FileStream(file, FileMode.Open)
        let result = ReadFromStream(stream) |> Seq.toList
        stream.Close()
        result

    let ReadIndexListFromFile(file : string) = ReadFromFile(file)|>List.indexed

    let ReadFromString(str : string) = 
        let byteArray = Encoding.ASCII.GetBytes(str)
        let stream = new MemoryStream(byteArray)
        let result = ReadFromStream(stream) |> Seq.toList
        stream.Close()
        result

    let ReadOneFromString(str : string) = 
        let gms = str|>ReadFromString
        gms.Head

    let SetaMoves(gml:UnencodedGame list) =
        gml|>List.map GameUnencoded.SetaMoves

