namespace TreeUtils

open System.IO
open System.Drawing
open System.Windows.Forms
open FsChess.WinForms
open ScincFuncs
open FsChess

module Form =
    let img nm =
        let thisExe = System.Reflection.Assembly.GetExecutingAssembly()
        let file = thisExe.GetManifestResourceStream("TreeUtils.Images." + nm)
        Image.FromStream(file)
    let ico nm =
        let thisExe = System.Reflection.Assembly.GetExecutingAssembly()
        let file = thisExe.GetManifestResourceStream("TreeUtils." + nm)
        new Icon(file)

    type FrmMain() as this =
        inherit Form(Text = "TreeUtils", Icon = ico "tree.ico", Width=600, Height=400, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, StartPosition = FormStartPosition.CenterScreen)
        let bfol = 
            let pth = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),"ScincNet\\bases")
            Directory.CreateDirectory(pth)|>ignore
            pth

        let sts = new WbStats(Dock=DockStyle.Fill)
        let crbtn = new Button(Text="Create Tree")
        let prg = new ProgressBar()

        let docreate(e) =
            let ndlg = new OpenFileDialog(Title="Open Database",Filter="Scid databases(*.si4)|*.si4",InitialDirectory=bfol)
            if ndlg.ShowDialog() = DialogResult.OK then
                let nm = Path.GetFileNameWithoutExtension(ndlg.FileName)
                let basename = Path.Combine(Path.GetDirectoryName(ndlg.FileName), nm)
                if (Base.Open(basename)<>1) then
                    MessageBox.Show("Error opening database " + basename)|>ignore
                elif (Base.Isreadonly()) then
                    MessageBox.Show("Error database " + basename + " is read only" )|>ignore
                else
                    let ply = 20
                    let numgames = Base.NumGames()
                    StaticTree.Init()
                    prg.Minimum<-0
                    prg.Maximum<-numgames
                    for i = 1 to numgames do
                        StaticTree.ProcessGame(i)
                        prg.Value<-i
                    ()
                
            
            


        let bgpnl = new Panel(Dock=DockStyle.Fill,BorderStyle=BorderStyle.Fixed3D)
        let btmpnl = new Panel(Dock=DockStyle.Bottom,BorderStyle=BorderStyle.Fixed3D,Height=30)

        do
            bgpnl.Controls.Add(sts)
            this.Controls.Add(bgpnl)
            crbtn.Top <- int(float(btmpnl.Height) * 0.5 - float(crbtn.Height) * 0.5)
            prg.Top <- int(float(btmpnl.Height) * 0.5 - float(prg.Height) * 0.5)
            prg.Left <- int(float(crbtn.Width) + 10.0)
            btmpnl.Controls.Add(crbtn)
            btmpnl.Controls.Add(prg)
            this.Controls.Add(btmpnl)
            crbtn.Click.Add(docreate)
            
            