namespace Tests

open System.IO
open Microsoft.VisualStudio.TestTools.UnitTesting
open ScincFuncs

[<TestClass>]
type TestTree () =
    let tfol = @"D:\GitHub\ScincNet\Tests\data\"
    let testdb = tfol + "test"
    let treedb = tfol + "tree"
    
    [<TestCleanup>]  
    member this.testClean() = 
        Base.Close()|>ignore
        if File.Exists(treedb + ".si4") then
            File.Delete(treedb + ".si4")
            File.Delete(treedb + ".sg4")
            File.Delete(treedb + ".sn4")
  
    [<TestInitialize>]  
    member this.testInit()   =
        if not (File.Exists(treedb + ".si4")) then
            File.Copy(testdb + ".si4", treedb + ".si4")
            File.Copy(testdb + ".sg4", treedb + ".sg4")
            File.Copy(testdb + ".sn4", treedb + ".sn4")
        Base.Open(treedb)|>ignore

    [<TestMethod>]
     member this.TreeSearch () =
        let mutable treestr = ""
        let actual = Tree.Search(&treestr)
        Assert.AreEqual(0, actual)
        let lines = treestr.Split('\n')
        let bits = lines.[0].Split('|')
        let rw = int(bits.[0])
        let mv = bits.[1].Trim()
        let nm = int(bits.[2].Trim())
        let freq = float(bits.[3].Trim())
        let scr = float(bits.[4].Trim())
        let pctDraws = float(bits.[5].Trim())
        let avgElo = int(bits.[6].Trim())
        let perf = int(bits.[7].Trim())
        let avgYear = int(bits.[8].Trim())
        let ecoStr = bits.[9].Trim()
        let tots = lines.[1].Split('|')
        let totnm = int(tots.[0].Trim())
        let totscr = float(tots.[1].Trim())
        let totpctDraws = float(tots.[2].Trim())
        let totavgElo = int(tots.[3].Trim())
        let totperf = int(tots.[4].Trim())
        let totavgYear = int(tots.[5].Trim())
        Assert.AreEqual(1,rw)
        Assert.AreEqual("d4",mv)
        Assert.AreEqual(5,nm)
        Assert.AreEqual(100.0,freq)
        Assert.AreEqual(0.0,scr)
        Assert.AreEqual(0.0,pctDraws)
        Assert.AreEqual(0,avgElo)
        Assert.AreEqual(0,perf)
        Assert.AreEqual(2016,avgYear)
        Assert.AreEqual("A40a",ecoStr)
        Assert.AreEqual(5,totnm)
        Assert.AreEqual(0.0,totscr)
        Assert.AreEqual(0.0,totpctDraws)
        Assert.AreEqual(0,totavgElo)
        Assert.AreEqual(0,totperf)
        Assert.AreEqual(2016,totavgYear)
