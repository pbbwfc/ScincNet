namespace ScincNet

open System
open System.Drawing
open System.Windows.Forms
open FsChess
open FsChess.WinForms

module Form =

    type FrmMain() as this =
        inherit Form(Text = "ScincNet", WindowState = FormWindowState.Maximized, IsMdiContainer = true)

        let mutable gms = []
        let mutable ct = 0
     
        let bd,pgn = CreateLnkBrdPgn()
        let pnl = new Panel(Dock=DockStyle.Top,Height=30)
        let nmlbl =
             new Label(Text = "Not loaded", Dock = DockStyle.Left, 
                       TextAlign = ContentAlignment.MiddleLeft,Width=400)
        let ts =
            new ToolStrip(Anchor = AnchorStyles.Right, 
                          GripStyle = ToolStripGripStyle.Hidden, 
                          Dock = DockStyle.None, Left = 100)

        let rtpnl = new Panel(Dock=DockStyle.Fill)
        //TODO
        let tb = new ToolStrip()
        let mm = new MenuStrip()



        do
            pgn|>rtpnl.Controls.Add
            //[ homeb; prevb; nextb; endb ] 
            //|> List.iter (fun c -> ts.Items.Add(c) |> ignore)
            pnl.Controls.Add(ts)
            pnl.Controls.Add(nmlbl)
            pnl|>rtpnl.Controls.Add
            rtpnl|>this.Controls.Add
            bd|>this.Controls.Add
            tb|>this.Controls.Add
            mm|>this.Controls.Add
