namespace FsChess.WinForms

open System.IO
open System.Windows.Forms

[<AutoOpen>]
module TcGamesLib =
    type TcGames() as gmstc =
        inherit TabControl(Width = 800, Height = 250)
        
        let cliptp = new TpGames()

 

        do
            gmstc.TabPages.Add(cliptp)

        ///Refresh the list
        member gmstc.Refrsh() =
            let tp = gmstc.SelectedTab:?>TpGames
            tp.Refrsh()

        member gmstc.AddTab() =
            let tp = new TpGames()
            let mutable dbnm = ""
            ScincFuncs.Base.Getfilename(&dbnm)|>ignore
            let dbno = ScincFuncs.Base.Current()
            let txt = dbno.ToString() + "-" + Path.GetFileNameWithoutExtension(dbnm)
            tp.Text <- txt
            gmstc.TabPages.Add(tp)

