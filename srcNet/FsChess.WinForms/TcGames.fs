﻿namespace FsChess.WinForms

open System.Windows.Forms

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
        member gmstc.Refrsh() =
            let tp = gmstc.SelectedTab:?>TpGames
            tp.Refrsh()

        ///Add a new tab
        member gmstc.AddTab() =
            let tp = new TpGames()
            tp.Init()
            gmstc.TabPages.Add(tp)
            gmstc.SelectedTab<-gmstc.TabPages.[gmstc.TabPages.Count-1]
            tp.GmSel|>Observable.add selEvt.Trigger

        ///Provides the selected Game
        member __.GmSel = selEvt.Publish


