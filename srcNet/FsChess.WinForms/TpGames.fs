﻿namespace FsChess.WinForms

open System.Drawing
open System.IO
open System.Windows.Forms

[<AutoOpen>]
module TpGamesLib =
    type GmUI =
        {
            Num : int
            White : string
            Black : string
            Result : string
            Length : string
            Date : string
            Event : string
            W_Elo : string
            B_Elo : string
            Round : string
            Site : string
            Deleted : string
            Variations : string
            Comments : string
            Annos : string
            ECO : string
            Opening : string
            Flags : string
            Start : string
            Country : string
            EventDate : string
            EndMaterial : string
        }

    type TpGames() as gmstp =
        inherit TabPage(Width = 800, Height = 250, 
                Text = "Clipbase")
        let gms = new DataGridView(Width = 800, Height = 250, 
                        AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders, 
                        ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                        AllowUserToAddRows = false,
                        AllowUserToDeleteRows = false,
                        ReadOnly = true,
                        CellBorderStyle = DataGridViewCellBorderStyle.Single,
                        GridColor = Color.Black, MultiSelect = false,
                        RowHeadersVisible=false, Dock=DockStyle.Fill)
        let mutable crw = -1
        let mutable gmsui = new System.ComponentModel.BindingList<GmUI>()
        let bs = new BindingSource()
        //scinc related
        let mutable b = 9 //base number
        let mutable nm = "" //base name
        let mutable gn = 0 //number of games
        let mutable fn = 0 //number of games in filter

        //events
        let selEvt = new Event<_>()

        let settxt() =
            let txt = b.ToString() + "-" + nm + "-" + fn.ToString() + "/" + gn.ToString()
            gmstp.Text <- txt
 
        let str2gmui (ln:string) =
            let f = ln.Split([|'|'|])
            {
                Num =  f.[0]|>int
                White = f.[1]
                Black = f.[2]
                Result = f.[3]
                Length = f.[4]
                Date = f.[5]
                Event = f.[6]
                W_Elo = f.[7]
                B_Elo = f.[8]
                Round = f.[9]
                Site = f.[10]
                Deleted = f.[11]
                Variations = f.[12]
                Comments = f.[13]
                Annos = f.[14]
                ECO = f.[15]
                Opening = f.[16]
                Flags = f.[17]
                Start = f.[18]
                Country = f.[19]
                EventDate = f.[20]
                EndMaterial = f.[21]
            }
        
        let dosave() =
            ///TODO
            ()
            
        let dodoubleclick(e:DataGridViewCellEventArgs) =
            let rw = e.RowIndex
            gms.CurrentCell <- gms.Rows.[rw].Cells.[0]
            crw <- gms.Rows.[rw].Cells.[0].Value:?>int
            crw|>selEvt.Trigger
        
        let setup() =
            bs.DataSource <- gmsui
            gms.DataSource <- bs
            gmstp.Controls.Add(gms)
            gms.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells)

        do 
            setup()
            gms.CellDoubleClick.Add(dodoubleclick)

        /// initialise
        member _.Init() =
            ScincFuncs.Base.Getfilename(&nm)|>ignore
            nm <- Path.GetFileNameWithoutExtension(nm)
            b <- ScincFuncs.Base.Current()
            gn <- ScincFuncs.Base.NumGames()
            gmsui.Clear()
            let formatStr = "g*|w*|b*|r*|m*|d*|e*|W*|B*|n*|s*|D*|V*|C*|A*|o*|O*|U*|S*|c*|E*|F*"
            let mutable glist = ""
            let chunk = ScincFuncs.ScidGame.List(&glist,1u,100u,formatStr)
            let lines =
                let glist1 = glist.TrimEnd('\n')
                glist1.Split([|'\n'|])
            lines|>Array.map str2gmui|>Array.iter(fun gmui -> gmsui.Add(gmui))
            gms.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells)
            fn <- gn
            settxt()
        
        ///Refresh the list
        member _.Refrsh() =
            gmsui.Clear()
            let formatStr = "g*|w*|b*|r*|m*|d*|e*|W*|B*|n*|s*|D*|V*|C*|A*|o*|O*|U*|S*|c*|E*|F*"
            let mutable glist = ""
            let chunk = ScincFuncs.ScidGame.List(&glist,1u,100u,formatStr)
            let lines =
                let glist1 = glist.TrimEnd('\n')
                glist1.Split([|'\n'|])
            lines|>Array.map str2gmui|>Array.iter(fun gmui -> gmsui.Add(gmui))
            gms.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells)
            //update filter count
            fn <- ScincFuncs.Filt.Count()
            settxt()

        ///Saves the database
        member _.Save() = dosave()

        ///Saves the PGN file with a new name
        member _.SaveAs(inm:string) = 
            //TODO
            dosave()

        ///Changes the contents of the Game that is selected
        //member _.ChangeGame(igm:Game) =
        //    cgm <- igm
        //    gmchg <- true

        ///Changes the header of the Game that is selected
        //member _.ChangeGameHdr(igm:Game) =
        //    cgm <- igm
        //    gmchg <- true
        //    let rw = gms.SelectedCells.[0].RowIndex
        //    let chdr = gmsui.[rw]
        //    //let nchdr = {chdr with White=cgm.WhitePlayer;W_Elo=cgm.WhiteElo;Black=cgm.BlackPlayer;B_Elo=cgm.BlackElo;Result=cgm.Result|>Result.ToUnicode;
        //    //                       Date=cgm|>GameDate.ToStr;Event=cgm.Event;Round=cgm.Round;Site=cgm.Site}
        //    //gmsui.[rw] <- nchdr
        //    ()

        ///Creates a new Game
        member _.NewGame() =
            //need to check if want to save
            //if gmchg then
            //    let nm = cgm.WhitePlayer + " v. " + cgm.BlackPlayer
            //    let dr = MessageBox.Show("Do you want to save the game: " + nm + " ?","Save Game",MessageBoxButtons.YesNo)
            //    if dr=DialogResult.Yes then
            //        dosave()
            //cbd <- Board.Start
            //cgm <- GameEMP
            crw <- 0 //TODO
            gmsui.Clear()
            //TODO
            gms.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells)
            //filtgms|>filtEvt.Trigger
            //cgm|>selEvt.Trigger

        ///Deletes selected Game
        member gmstp.DeleteGame() =
            //let nm = cgm.WhitePlayer + " v. " + cgm.BlackPlayer
            //let dr = MessageBox.Show("Do you really want to permanently delete the game: " + nm + " ?","Delete Game",MessageBoxButtons.YesNo)
            //if dr=DialogResult.Yes then
            //    let orw = gms.SelectedCells.[0].RowIndex
            //    //save without gams
            //    //reload saves pgn
            //    cbd <- Board.Start
            //    gmsui.Clear()
            //    //TODO
            //    gms.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells)
            //    //filtgms|>filtEvt.Trigger
            //    gmchg <- false
            //    //select row
            //    let rw = if orw=0 then 0 else orw-1
            //    //TODO
            //    cgm|>selEvt.Trigger
            //    gms.CurrentCell <- gms.Rows.[rw].Cells.[0]
            ()

        ///Export filtered games
        member gmstp.ExportFilter(filtfil:string) =
            //TODO
            ()
        
        ///Provides the selected Game
        member __.GmSel = selEvt.Publish
