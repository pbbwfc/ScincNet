namespace Tests

open System.IO
open Microsoft.VisualStudio.TestTools.UnitTesting
open ScincFuncs

[<TestClass>]
type TestFilt () =
    let tfol = @"D:\GitHub\ScincNet\Tests\data\"
    let testdb = tfol + "test"
    let filtdb = tfol + "filt"
    
    [<TestCleanup>]  
    member this.testClean() = 
        Base.Close()|>ignore
        if File.Exists(filtdb + ".si4") then
            File.Delete(filtdb + ".si4")
            File.Delete(filtdb + ".sg4")
            File.Delete(filtdb + ".sn4")
  
    [<TestInitialize>]  
    member this.testInit()   =
        if not (File.Exists(filtdb + ".si4")) then
            File.Copy(testdb + ".si4", filtdb + ".si4")
            File.Copy(testdb + ".sg4", filtdb + ".sg4")
            File.Copy(testdb + ".sn4", filtdb + ".sn4")
        Base.Open(filtdb)|>ignore

    [<TestMethod>]
     member this.FiltCount () =
        let actual = Filt.Count()
        Assert.AreEqual(6, actual)
        