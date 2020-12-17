namespace ScincNet

open System
open System.Drawing
open System.Windows.Forms
open FsChess
open FsChess.WinForms

module Form =
    let img nm =
        let thisExe = System.Reflection.Assembly.GetExecutingAssembly()
        let file = thisExe.GetManifestResourceStream("ScincNet.Images." + nm)
        Image.FromStream(file)
    let ico nm =
        let thisExe = System.Reflection.Assembly.GetExecutingAssembly()
        let file = thisExe.GetManifestResourceStream("ScintNet.Icons." + nm)
        new Icon(file)

    type FrmMain() as this =
        inherit Form(Text = "ScincNet", WindowState = FormWindowState.Maximized, IsMdiContainer = true)

        let mutable gms = []
        let mutable ct = 0
     
        let bd = new PnlBoard(Dock=DockStyle.Fill)
        let pgn = new WbPgn(Dock=DockStyle.Fill)
        let sts = new WbStats(Dock=DockStyle.Fill)
        let gms = new DgvGames(Dock=DockStyle.Fill)
        let anl = new PnlAnl(Dock=DockStyle.Fill)

        let donew() =
            
            
            ()


        //TODO
        let tb = 
            let ts = new ToolStrip()
            let newb = new ToolStripButton(Image = img "new.png", ImageTransparentColor = Color.Magenta, DisplayStyle = ToolStripItemDisplayStyle.Image, Text = "&New")


            ts.Items.Add(newb)|>ignore
            ts

        let mm = 
            let ms = new MenuStrip()
            // file menu
            let filem = new ToolStripMenuItem(Text = "&File")
            // file new
            let newm = new ToolStripMenuItem(Image = img "new.png", ImageTransparentColor = Color.Magenta, ShortcutKeys = (Keys.Control|||Keys.N), Text = "&New")
            newm.Click.Add(fun _ -> donew())
            filem.DropDownItems.Add(newm)|>ignore
            
            
            
            
            ms.Items.Add(filem)|>ignore
            
            ms



        let gmlbl = new Label(Text="Game: White v. Black",Width=400,TextAlign=ContentAlignment.MiddleLeft,Font = new Font(new FontFamily("Arial"), 12.0f))
        //TODO need to set this
        let fllbl = new Label(Text="Filter: 99999999/99999999",Width=400,TextAlign=ContentAlignment.MiddleLeft,Font = new Font(new FontFamily("Arial"), 12.0f))
        let vpnl = new FlowLayoutPanel(Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown)

        let bgpnl = new Panel(Dock=DockStyle.Fill,BorderStyle=BorderStyle.Fixed3D)
        let lfpnl = new Panel(Dock=DockStyle.Left,BorderStyle=BorderStyle.Fixed3D,Width=400)
        let rtpnl = new Panel(Dock=DockStyle.Fill,BorderStyle=BorderStyle.Fixed3D)
        let lftpnl = new Panel(Dock=DockStyle.Top,BorderStyle=BorderStyle.Fixed3D,Height=400)
        let lfmpnl = new Panel(Dock=DockStyle.Top,BorderStyle=BorderStyle.Fixed3D,Height=50)
        let lfbpnl = new Panel(Dock=DockStyle.Fill,BorderStyle=BorderStyle.Fixed3D)
        let rttpnl = new Panel(Dock=DockStyle.Top,BorderStyle=BorderStyle.Fixed3D,Height=350)
        let rtmpnl = new Panel(Dock=DockStyle.Top,BorderStyle=BorderStyle.Fixed3D,Height=100)
        let rtbpnl = new Panel(Dock=DockStyle.Fill,BorderStyle=BorderStyle.Fixed3D)
 
        do
            gms|>rtbpnl.Controls.Add
            rtbpnl|>rtpnl.Controls.Add
            anl|>rtmpnl.Controls.Add
            rtmpnl|>rtpnl.Controls.Add
            pgn|>rttpnl.Controls.Add
            rttpnl|>rtpnl.Controls.Add
            rtpnl|>bgpnl.Controls.Add
            sts|>lfbpnl.Controls.Add
            lfbpnl|>lfpnl.Controls.Add
            gmlbl|>vpnl.Controls.Add
            fllbl|>vpnl.Controls.Add
            vpnl|>lfmpnl.Controls.Add
            lfmpnl|>lfpnl.Controls.Add
            bd|>lftpnl.Controls.Add
            lftpnl|>lfpnl.Controls.Add
            lfpnl|>bgpnl.Controls.Add
            bgpnl|>this.Controls.Add
            tb|>this.Controls.Add
            mm|>this.Controls.Add
    