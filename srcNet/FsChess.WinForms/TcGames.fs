namespace FsChess.WinForms

open System.Windows.Forms
open FsChess

[<AutoOpen>]
module TcGamesLib =
    type TcGames() as gmstc =
        inherit TabControl(Width = 800, Height = 250)
        
        let cliptp = new TpGames()
        //events
        let selEvt = new Event<_>()

        do
            gmstc.TabPages.Add(cliptp)
            cliptp.GmSel|>Observable.add selEvt.Trigger

        ///Refresh the selected tab
        member gmstc.Refrsh(bd:Brd) =
            let tp = gmstc.SelectedTab:?>TpGames
            let fen = bd|>Board.ToStr
            tp.Refrsh(fen)

        ///Refresh the selected tab
        member gmstc.SelNum(num:int) =
            let tp = gmstc.SelectedTab:?>TpGames
            tp.SelNum(num)

        ///Add a new tab
        member gmstc.AddTab() =
            let tp = new TpGames()
            tp.Init()
            gmstc.TabPages.Add(tp)
            gmstc.SelectedTab<-gmstc.TabPages.[gmstc.TabPages.Count-1]
            tp.GmSel|>Observable.add selEvt.Trigger

        ///Close the selected tab
        member gmstc.Close() =
            let tp = gmstc.SelectedTab:?>TpGames
            tp.Close()
            gmstc.TabPages.Remove(tp)


        ///Provides the selected Game
        member __.GmSel = selEvt.Publish


