namespace ScincNet

open System.Windows.Forms
open System.IO
open FSharp.Json

module Recents = 
    
    let mutable dbs:string list = []
    let fol = Application.LocalUserAppDataPath
    let optfl = Path.Combine(fol,"recents.json")

    let get() =
        if File.Exists(optfl) then 
            let str = File.ReadAllText(optfl)  
            dbs <- Json.deserialize (str)

    let save() =
        let str = Json.serialize (dbs)
        File.WriteAllText(optfl, str)
    
    let add recent =
        get()
        dbs<-recent::dbs
        save()