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
        Assert.AreEqual("      Move", treestr.Substring(0,10))
