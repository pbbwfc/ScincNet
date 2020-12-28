namespace Tests

open System.IO
open Microsoft.VisualStudio.TestTools.UnitTesting
open ScincFuncs

[<TestClass>]
type TestTree () =
    let tfol = @"D:\GitHub\ScincNet\Tests\data\"
    let testdb = tfol + "test"
    let treedb = tfol + "tree"
    let eco = tfol + "scid.eco"
    
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
        Eco.Read(eco)|>ignore

    [<TestMethod>]
     member this.TreeSearch () =
        let mutable tsts = new ScincFuncs.totstats()
        let mutable mvsts = new ResizeArray<ScincFuncs.mvstats>()
        let actual = Tree.Search(&mvsts,&tsts,"rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",1)
        Assert.AreEqual(0, actual)
        Assert.AreEqual(5,tsts.TotCount)
        Assert.AreEqual(1.0,tsts.TotFreq)
        Assert.AreEqual(0,tsts.TotWhiteWins)
        Assert.AreEqual(0,tsts.TotDraws)
        Assert.AreEqual(5,tsts.TotBlackWins)
        Assert.AreEqual(0.0,tsts.TotScore)
        Assert.AreEqual(0.0,tsts.TotDrawPc)
        Assert.AreEqual(0,tsts.TotAvElo)
        Assert.AreEqual(0,tsts.TotPerf)
        Assert.AreEqual(2016,tsts.TotAvYear)
        let mvst = mvsts.[0]
        Assert.AreEqual("d4",mvst.Mvstr)
        Assert.AreEqual(5,mvst.Count)
        Assert.AreEqual(1.0,mvst.Freq)
        Assert.AreEqual(0,mvst.WhiteWins)
        Assert.AreEqual(0,mvst.Draws)
        Assert.AreEqual(5,mvst.BlackWins)
        Assert.AreEqual(0.0,mvst.Score)
        Assert.AreEqual(0.0,mvst.DrawPc)
        Assert.AreEqual(0,mvst.AvElo)
        Assert.AreEqual(0,mvst.Perf)
        Assert.AreEqual(2016,mvst.AvYear)
        Assert.AreEqual("A40a",mvst.ECO)
        
        
        
        





