namespace FsChess.WinForms

open System.Drawing
open System.Windows.Forms

[<AutoOpen>]
module DgvGamesLib =
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

    type DgvGames() as gms =
        inherit DataGridView(Width = 800, Height = 250, 
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders, 
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                GridColor = Color.Black, MultiSelect = false,
                RowHeadersVisible=false)

        let mutable crw = -1
        let mutable gmchg = false
        let mutable gmsui = new System.ComponentModel.BindingList<GmUI>()
        let bs = new BindingSource()
        //scinc related
        let mutable b = 0 //base number


        //events
        let filtEvt = new Event<_>()
        let selEvt = new Event<_>()
        let pgnEvt = new Event<_>()

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
            crw <- e.RowIndex
            //need to check if want to save
            //if gmchg then
            //    //let nm = cgm.WhitePlayer + " v. " + cgm.BlackPlayer
            //    //let dr = MessageBox.Show("Do you want to save the game: " + nm + " ?","Save Game",MessageBoxButtons.YesNoCancel)
            //    if dr=DialogResult.Yes then
            //        dosave()
            //        //TODO
            //        cgm|>selEvt.Trigger
            //    elif dr=DialogResult.No then
            //        //TODO
            //        cgm|>selEvt.Trigger
            //else
            //    //TODO
            //    cgm|>selEvt.Trigger
            gms.CurrentCell <- gms.Rows.[crw].Cells.[0]
            crw|>selEvt.Trigger
        
        let setup() =
            bs.DataSource <- gmsui
            gms.DataSource <- bs

        do 
            setup()
            gms.CellDoubleClick.Add(dodoubleclick)

        ///Refresh the list
        member this.Refrsh() =
            b <- ScincFuncs.Base.Current()
            gmsui.Clear()
            //set chunk [sc_game list $glstart $c $glistCodes]
            let formatStr = "g*|w*|b*|r*|m*|d*|e*|W*|B*|n*|s*|D*|V*|C*|A*|o*|O*|U*|S*|c*|E*|F*"
            let mutable glist = ""
            let chunk = ScincFuncs.ScidGame.List(&glist,1u,100u,formatStr)
            let lines =
                let glist1 = glist.TrimEnd('\n')
                glist1.Split([|'\n'|])
            lines|>Array.map str2gmui|>Array.iter(fun gmui -> gmsui.Add(gmui))
            gms.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells)

            ()


        ///Saves the database
        member _.Save() = dosave()

        ///Saves the PGN file with a new name
        member _.SaveAs(inm:string) = 
            //TODO
            dosave()

        ///Sets the Board to be filtered on
        //member gms.SetBoard(ibd:Brd) =
        //    cbd <- ibd
        //    gmsui.Clear()
        //    //TODO - need to filter by board
        //    gms.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells)
        //    //filtgms|>filtEvt.Trigger

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
            gmchg <- false
            //cgm|>selEvt.Trigger

        ///Deletes selected Game
        member gms.DeleteGame() =
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
        member gms.ExportFilter(filtfil:string) =
            //TODO
            ()
        
        ///Provides the revised filtered list of Games
        member __.FiltChng = filtEvt.Publish
        
        ///Provides the selected Game
        member __.GmSel = selEvt.Publish

        ///Provides the initial Board when the PGN file selected changes
        member __.PgnChng = pgnEvt.Publish
