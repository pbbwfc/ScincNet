namespace ScincNet

open System.Windows.Forms
open System.IO
open FSharp.Json

module Recents = 
    
    let mutable dbs:Set<string> = Set.empty
    let mutable trs:Set<string> = Set.empty
    let mutable strs:Set<string> = Set.empty
    let fol = Application.LocalUserAppDataPath
    let recfl = Path.Combine(fol,"recents.json")
    let trfl = Path.Combine(fol,"recenttrees.json")
    let strfl = Path.Combine(fol,"recentstatictrees.json")
    
    let getrecs() =
        if File.Exists(recfl) then 
            let str = File.ReadAllText(recfl)  
            dbs <- Json.deserialize (str)
                    |>List.filter(fun db -> File.Exists(db + ".si4"))
                    |>Set.ofList

    let gettrs() =
        if File.Exists(trfl) then 
            let str = File.ReadAllText(trfl)  
            trs <- Json.deserialize (str)
                    |>List.filter(fun db -> File.Exists(db + ".si4"))
                    |>Set.ofList

    let getstrs() =
        if File.Exists(strfl) then 
            let str = File.ReadAllText(strfl)  
            strs <- Json.deserialize (str)
                    |>List.filter(fun t -> Directory.Exists(t))
                    |>Set.ofList
    
    let saverecs() =
        let str = Json.serialize (dbs|>Set.toList)
        File.WriteAllText(recfl, str)

    let savetrs() =
        let str = Json.serialize (trs|>Set.toList)
        File.WriteAllText(trfl, str)

    let savestrs() =
        let str = Json.serialize (strs|>Set.toList)
        File.WriteAllText(strfl, str)

    let addrec recent =
        getrecs()
        dbs<- dbs.Add recent
        saverecs()

    let addtr recent =
        gettrs()
        trs<- trs.Add recent
        savetrs()

    let addstr recent =
        getstrs()
        strs<- strs.Add recent
        savestrs()