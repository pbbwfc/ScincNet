namespace FsChess.WinForms

open System.Windows.Forms

[<AutoOpen>]
module TcGamesLib =
    type TcGames() as gmstc =
        inherit TabControl(Width = 800, Height = 250)
        
        let cliptp = new TpGames()

        do
            gmstc.TabPages.Add(cliptp)

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

