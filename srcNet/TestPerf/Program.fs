// Learn more about F# at http://fsharp.org

open System
open System.IO
open FsChess
open MessagePack
//open System.Text
//open MBrace.FsPickler

[<EntryPoint>]
let main argv =
    let tfol = 
        let pth = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),"ScincNet\\trees")
        Directory.CreateDirectory(pth)|>ignore
        pth
    let mutable st = DateTime.Now
    let mutable nd = DateTime.Now
    let logtime() = 
        let el = float((nd-st).Milliseconds)/1000.0
        printfn "Elapsed time %f seconds" el
    
    let Save(fn:string, stsdict:BrdStats) =
        let bin = MessagePackSerializer.Serialize<BrdStats>(stsdict)
        File.WriteAllBytes(fn,bin)
    let Load(fn:string) =
        use fs = new FileStream(fn, FileMode.Open, FileAccess.Read)
        MessagePackSerializer.Deserialize<BrdStats>(fs)

    let lfol = Path.Combine(tfol,"lightning")
    let CreateLightning() =
        use env = new LightningDB.LightningEnvironment(lfol)
        env.MaxDatabases<-2
        env.Open()
        use tx = env.BeginTransaction()
        use db = tx.OpenDatabase("Tree",new LightningDB.DatabaseConfiguration(Flags = LightningDB.DatabaseOpenFlags.Create))
        tx.Commit()
    
    let SaveLightning(posns:string[],stss:stats[]) =
        use env = new LightningDB.LightningEnvironment(lfol)
        env.MaxDatabases<-2
        env.Open()
        //env.MapSize <- 350000000L
        env.MapSize <- 1350000000L
        //env.MapSize <- 10000000L
        use tx = env.BeginTransaction()
        use db = tx.OpenDatabase("Tree")
        for i = 0 to posns.Length-1 do
            let cd = tx.Put(db,MessagePackSerializer.Serialize<string>(posns.[i]),MessagePackSerializer.Serialize<stats>(stss.[i]))
            if int(cd)<>0 then 
                let curr_limit = float(env.MapSize)
                let mult = float(posns.Length/i)*1.1
                let new_limit = mult * curr_limit
                tx.Abort()
                failwith (sprintf "Code: %s" (cd.ToString()))

        tx.Commit()

    let ReadLightning(posns:string[]) =
        use env = new LightningDB.LightningEnvironment(lfol)
        env.MaxDatabases<-2
        env.Open()
        use tx = env.BeginTransaction()
        use db = tx.OpenDatabase("Tree")
        let getv (posn:string) =
            let cd,k,v = tx.Get(db,MessagePackSerializer.Serialize<string>(posn)).ToTuple()
            if int(cd)<>0 then 
                failwith (sprintf "Code: %s" (cd.ToString()))
            else
                let ro = new ReadOnlyMemory<byte>(v.CopyToNewArray())
                MessagePackSerializer.Deserialize<stats>(ro)
        let vs = posns|>Array.map getv
        let cd = tx.Commit()
        if int(cd)<>0 then 
            failwith (sprintf "Final Code: %s" (cd.ToString()))
        else vs


    //let dfol = Path.Combine(tfol,"leveldb")
    //let CreateLevel() =
    //    let options = new LevelDB.Options(CreateIfMissing = true)
    //    let db = new LevelDB.DB(options, dfol)
    //    db.Close()

    //let SaveLevel(posns:string[],stss:stats[]) =
    //    use db = new LevelDB.DB(new LevelDB.Options(), dfol)
    //    for i = 0 to posns.Length-1 do
    //        db.Put(Encoding.UTF8.GetBytes(posns.[i]),MessagePackSerializer.Serialize<stats>(stss.[i]))

    //let ReadLevel(posns:string[]) =
    //    use db = new LevelDB.DB(new LevelDB.Options(), dfol)
    //    let getv (posn:string) =
    //        let v = db.Get(Encoding.UTF8.GetBytes(posn))
    //        let ro = new ReadOnlyMemory<byte>(v)
    //        MessagePackSerializer.Deserialize<stats>(ro)
    //    posns|>Array.map getv

    //let lfol2 = Path.Combine(tfol,"lightning2")
    //let CreateLightning2() =
    //    use env = new LightningDB.LightningEnvironment(lfol2)
    //    env.MaxDatabases<-2
    //    env.Open()
    //    use tx = env.BeginTransaction()
    //    use db = tx.OpenDatabase("Fritz1",new LightningDB.DatabaseConfiguration(Flags = LightningDB.DatabaseOpenFlags.Create))
    //    tx.Commit()
     
    //let SaveLightning2(posns:string[],stss:stats[]) =
    //    use env = new LightningDB.LightningEnvironment(lfol2)
    //    env.MaxDatabases<-2
    //    env.Open()
    //    env.MapSize <- 650000000L
    //    use tx = env.BeginTransaction()
    //    use db = tx.OpenDatabase("Fritz1")
    //    let bs = FsPickler.CreateBinarySerializer()
    //    for i = 0 to posns.Length-1 do
    //        let cd = tx.Put(db,Encoding.UTF8.GetBytes(posns.[i]),bs.Pickle(stss.[i]))
    //        if int(cd)<>0 then 
    //            let curr_limit = float(env.MapSize)
    //            let mult = float(posns.Length/i)*1.1
    //            let new_limit = mult * curr_limit
    //            failwith (sprintf "Code: %s" (cd.ToString()))

    //    tx.Commit()

    //let ReadLightning2(posns:string[]) =
    //    use env = new LightningDB.LightningEnvironment(lfol2)
    //    env.MaxDatabases<-2
    //    env.Open()
    //    use tx = env.BeginTransaction()
    //    use db = tx.OpenDatabase("Fritz1")
    //    let bs = FsPickler.CreateBinarySerializer()
    //    let getv (posn:string) =
    //        let cd,k,v = tx.Get(db,Encoding.UTF8.GetBytes(posn)).ToTuple()
    //        if int(cd)<>0 then 
    //            failwith (sprintf "Code: %s" (cd.ToString()))
    //        else
    //            let ba = v.CopyToNewArray()
    //            bs.UnPickle<stats> ba
    //    let vs = posns|>Array.map getv
    //    let cd = tx.Commit()
    //    if int(cd)<>0 then 
    //        failwith (sprintf "Final Code: %s" (cd.ToString()))
    //    else vs




    let nm = "Fritz1"
    let testtr4 = Path.Combine(tfol,nm + ".tr4")
    //Load dictionary
    st <- DateTime.Now
    let stsdict = Load(testtr4)
    nd <- DateTime.Now
    logtime()
    //Create arrays
    let numpos = stsdict.Count
    let mutable posns = Array.zeroCreate numpos 
    let mutable stss = Array.zeroCreate numpos
    stsdict.Keys.CopyTo(posns,0)
    stsdict.Values.CopyTo(stss,0)
    //==========================================================================
    //Use lightning
    printfn "Lightning----------------------------------------------------------"
    File.Delete(Path.Combine(lfol,"lock.mdb"))
    File.Delete(Path.Combine(lfol,"data.mdb"))
    let cd = StaticTree.CreateBig(lfol)
    st <- DateTime.Now
    let cd = StaticTree.Save(posns,stss,lfol)
    nd <- DateTime.Now
    logtime()
    //st <- DateTime.Now
    //let cd = StaticTree.Compact(lfol)
    //nd <- DateTime.Now
    //logtime()
    st <- DateTime.Now
    let nstss = StaticTree.ReadArray(posns.[..1000],lfol)
    nd <- DateTime.Now
    logtime()
    let sts = nstss.[99]
    let tst = sts.MvsStats.Count
    printfn "Lightning count for mvs in posn 99 is %i" tst
    st <- DateTime.Now
    let nstss = StaticTree.Read(posns.[1000],lfol)
    nd <- DateTime.Now
    logtime()
    st <- DateTime.Now
    let nstss = StaticTree.ReadArray(posns.[..1000],lfol)
    nd <- DateTime.Now
    logtime()
    st <- DateTime.Now
    let nstss = StaticTree.ReadArray(posns.[..1000],lfol)
    nd <- DateTime.Now
    logtime()
    st <- DateTime.Now
    let nstss = StaticTree.Read(posns.[1000],lfol)
    nd <- DateTime.Now
    logtime()
    st <- DateTime.Now
    let nstss = StaticTree.Read(posns.[1000],lfol)
    nd <- DateTime.Now
    logtime()


    
    //==========================================================================
    //Use leveldb
    //printfn "LevelDb----------------------------------------------------------"
    //CreateLevel()
    //let pstss = Array.zip posns stss
    //st <- DateTime.Now
    //SaveLevel(posns,stss)
    //nd <- DateTime.Now
    //logtime()
    //st <- DateTime.Now
    //let nstss = ReadLevel(posns.[..1000])
    //nd <- DateTime.Now
    //logtime()
    //let sts = nstss.[99]
    //let tst = sts.MvsStats.Count
    //printfn "LevelDb count for mvs in posn 99 is %i" tst
    //st <- DateTime.Now
    //let nstss = ReadLevel(posns.[1000..1000])
    //nd <- DateTime.Now
    //logtime()

    //==========================================================================
    //Use lightning2
    //printfn "Lightning2----------------------------------------------------------"
    //File.Delete(Path.Combine(lfol2,"lock.mdb"))
    //File.Delete(Path.Combine(lfol2,"data.mdb"))
    //let cd = CreateLightning2()
    //let pstss = Array.zip posns stss
    //st <- DateTime.Now
    //let cd = SaveLightning2(posns,stss)
    //nd <- DateTime.Now
    //logtime()
    //st <- DateTime.Now
    //let nstss = ReadLightning2(posns.[..1000])
    //nd <- DateTime.Now
    //logtime()
    //let sts = nstss.[99]
    //let tst = sts.MvsStats.Count
    //printfn "LevelDb count for mvs in posn 99 is %i" tst
    //st <- DateTime.Now
    //let nstss = ReadLightning2(posns.[1000..1000])
    //nd <- DateTime.Now
    //logtime()


    0 // return an integer exit code
