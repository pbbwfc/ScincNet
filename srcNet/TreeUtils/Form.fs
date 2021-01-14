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
        inherit Form(Text = "TreeUtils", Icon = ico "tree.ico", Width=800, Height=500, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, StartPosition = FormStartPosition.CenterScreen)
        let bfol = 
            let pth = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),"ScincNet\\bases")
            Directory.CreateDirectory(pth)|>ignore
            pth
        let tfol = 
            let pth = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),"ScincNet\\trees")
            Directory.CreateDirectory(pth)|>ignore
            pth

        let mutable bd = Board.Start

        let sts = new WbStats(Dock=DockStyle.Fill)
        let crbtn = new Button(Text="Create Tree")
        let prg = new ProgressBar(Width=200)

        let domvsel(mvstr) =
            let mv = mvstr|>Move.FromSan bd
            bd <- bd|>Board.Push mv
            sts.UpdateStr(bd)

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
                        if i%100=0 then prg.Value<-i
                    Base.Close()|>ignore
                    //now create tree for each
                    let numpos = StaticTree.NumPos()
                    prg.Maximum<-numpos
                    StaticTree.CreateArrays()
                    for i = 1 to numpos do
                        StaticTree.ProcessPos(i)
                        if i%100=0 then prg.Value<-i
                    prg.Value<-0
                    let fn = Path.Combine(tfol,nm + ".tr4")
                    StaticTree.Save(fn)
                    StaticTree.Load(fn)
                    sts.RefrshStatic()
                    

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
            sts.MvSel |> Observable.add domvsel
           