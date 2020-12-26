namespace FsChess.WinForms

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
        member _.Refrsh(fen:string) =
            gmsui.Clear()
            let formatStr = "g*|w*|b*|r*|m*|d*|e*|W*|B*|n*|s*|D*|V*|C*|A*|o*|O*|U*|S*|c*|E*|F*"
            //apply filter
            ScincFuncs.Search.Board(fen,b)|>ignore
            
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

 
        ///Provides the selected Game
        member __.GmSel = selEvt.Publish
