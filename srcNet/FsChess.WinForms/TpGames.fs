namespace FsChess.WinForms

open System.Drawing
open System.IO
open System.Windows.Forms

[<AutoOpen>]
module TpGamesLib =

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
                        RowHeadersVisible=false, Dock=DockStyle.Fill
                        )
        let mutable crw = -1
        let mutable gmsui = new System.ComponentModel.BindingList<ScincFuncs.gmui>()
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
            
        ///Refresh the list
        member _.Refrsh(fen:string) =
            gmsui.Clear()
            //apply filter
            ScincFuncs.Search.Board(fen,b)|>ignore
            let mutable gmsl = new ResizeArray<ScincFuncs.gmui>()
            let chunk = ScincFuncs.ScidGame.List(&gmsl,1u,100u)
            gmsl|>Seq.iter(fun gmui -> gmsui.Add(gmui))
            gms.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells)
            //update filter count
            fn <- ScincFuncs.Filt.Count()
            settxt()

        ///Refresh the list
        member _.SelNum(num:int) =
            for rwo in gms.Rows do
                let rw = rwo:?>DataGridViewRow
                if rw.Cells.[0].Value:?>int = num then
                    crw <- num
                    gms.CurrentCell <- rw.Cells.[0]


        /// initialise
        member _.Close() =
            ScincFuncs.Base.Close()|>ignore
 
        ///Provides the selected Game
        member __.GmSel = selEvt.Publish
