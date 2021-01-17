namespace FsChessPgn

open System.IO
open FsChess
open MessagePack

module StaticTree =

    let Save(fn:string, stsdict) =
        let bin = MessagePackSerializer.Serialize<BrdStats>(stsdict)
        File.WriteAllBytes(fn,bin)
        
    let Load(fn:string) =
        use fs = new FileStream(fn, FileMode.Open, FileAccess.Read)
        MessagePackSerializer.Deserialize<BrdStats>(fs)
    
    