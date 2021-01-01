namespace ScincNet

open System.Windows.Forms
open System.IO
open FSharp.Json

module Recents = 
    
    let mutable dbs:Set<string> = Set.empty
    let fol = Application.LocalUserAppDataPath
    let optfl = Path.Combine(fol,"recents.json")

    let get() =
        if File.Exists(optfl) then 
            let str = File.ReadAllText(optfl)  
            dbs <- Json.deserialize (str)
                    |>List.filter(fun db -> File.Exists(db + ".si4"))
                    |>Set.ofList

    let save() =
        let str = Json.serialize (dbs|>Set.toList)
        File.WriteAllText(optfl, str)
    
    let add recent =
        get()
        dbs<- dbs.Add recent
        save()