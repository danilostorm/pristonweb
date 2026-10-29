using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StormPT
{
    internal static class Entry
    {
        public const string PublicKey = "<RSAKeyValue><Modulus>r/W2LGrYfvVEzWSXMHJLYxNHmA9NA7S0cu0tYYwQGCJ3zdJYZnFUwHYCwUJOosS8F9GHi7kpCg7PTgeTdAyrewnE/BmjB/j6fITSXrj5v1Yh+l+zeeXOMNhyM3o0VngPBTAyzN+u7vcWUX6Spv/msSLnw7KiOC0ZVR6XMskKlAtjRihhLAueo6leHhC3ZUoAtgXyWB1JdKiIGV9amg8KL2eYyls4wWzyaEDg8Lmr4BlUYW3cNszGNonvPE5jUXLqP+VZoRgnFSlrwSSW2X8TKugUKthJZ7FbDOkxUY9pK7/DW2ViYk3PqwFNL0OaPR3b06mTqncqz1gYWNAH8MHCAQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
        [STAThread]
        private static void Main(string[] args)
        {
            if(args.Length==5 && args[0]=="--replace") { ReplaceSelf(args); return; }
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Launcher());
        }
        internal static string Hash(string path)
        {
            using(var sha=SHA256.Create())
            using(var s=File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(s)).Replace("-","").ToLowerInvariant();
        }
        private static void ReplaceSelf(string[] args)
        {
            try {
                string old=Path.GetFullPath(args[1]), staged=Path.GetFullPath(args[2]);
                string root=Path.GetDirectoryName(old);
                if(!string.Equals(Path.GetFileName(old),"launcher.exe",StringComparison.OrdinalIgnoreCase))return;
                if(!staged.StartsWith(Path.Combine(root,".stormpt-updates")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))return;
                if(args[3].Length!=64 || !File.Exists(staged))return;
                int pid;if(!int.TryParse(args[4],out pid))return;
                try { using(var proc=Process.GetProcessById(pid))proc.WaitForExit(25000); } catch(ArgumentException) {}
                if(!string.Equals(Hash(staged),args[3],StringComparison.OrdinalIgnoreCase))return;
                string backup=old+".bak";if(File.Exists(backup))File.Delete(backup);
                if(File.Exists(old))File.Move(old,backup);
                try { File.Copy(staged,old,true);Process.Start(new ProcessStartInfo(old){UseShellExecute=true,WorkingDirectory=root}); }
                catch { if(!File.Exists(old) && File.Exists(backup))File.Move(backup,old); }
            } catch {}
        }
    }
    internal sealed class ManifestItem
    {
        public string Relative, Hash, Url, ArchiveHash;
        public long Size, ArchiveSize;
        public string[] Parts;
    }
    internal sealed class StrictWebClient:WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            var request=base.GetWebRequest(address);
            var web=request as HttpWebRequest;
            if(web!=null){web.AllowAutoRedirect=false;web.Timeout=30000;web.ReadWriteTimeout=30000;}
            return request;
        }
    }
    internal sealed class Launcher:Form
    {
        const string DefaultManifestUrl="https://raw.githubusercontent.com/danilostorm/pristonweb/stormpt-launcher-v0.1.0/launcher/feed/manifest.txt";

        const int DesignW=1448, DesignH=1086;
        readonly string root=AppDomain.CurrentDomain.BaseDirectory;
        readonly string localIni;
        Bitmap artwork;
        string uiStatus="Verificando atualizacoes...";
        string uiDetail="Conectando ao servidor de atualizacoes.";
        int percentage=0, hover=0, online=-1;
        bool busy;
        System.Windows.Forms.Timer statusTimer;
        string[][] news=new string[][] {
            new string[]{"Launcher v0.3.0","29/10/2026","Atualizacoes automaticas e seguranca para seu cliente."},
            new string[]{"Instrutora Amanda","29/10/2026","Adicionada em Ricarten e Pillai. Novas missoes em desenvolvimento."},
            new string[]{"Novos personagens","29/10/2026","Agora comecam no nivel 1. Bem-vindo ao StormPT!"}
        };
        static readonly Rectangle PlayZone=new Rectangle(862,809,525,189);
        static readonly Rectangle VerifyZone=new Rectangle(1172,597,207,54);
        static readonly Rectangle AllNewsZone=new Rectangle(1269,109,103,51);
        static readonly Rectangle GearZone=new Rectangle(1227,4,68,53);
        static readonly Rectangle MinZone=new Rectangle(1295,4,58,53);
        static readonly Rectangle CloseZone=new Rectangle(1351,4,84,53);
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hwnd,int msg,int wp,int lp);
        public Launcher()
        {
            localIni=Path.Combine(root,"launcher.ini");
            string old=Path.Combine(root,"StormPTLauncher.ini");
            if(!File.Exists(localIni)&&File.Exists(old))File.Copy(old,localIni);
            Text="StormPT Launcher";
            AutoScaleMode=AutoScaleMode.None;
            KeyPreview=true;
            BackColor=Color.FromArgb(2,14,32);
            ForeColor=Color.White;
            FormBorderStyle=FormBorderStyle.None;
            StartPosition=FormStartPosition.CenterScreen;
            MaximizeBox=false;
            MinimizeBox=true;
            ShowInTaskbar=true;
            DoubleBuffered=true;
            SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer,true);
            Rectangle work=Screen.PrimaryScreen.WorkingArea;
            double factor=Math.Min(1.0,Math.Min((work.Width-32)/(double)DesignW,(work.Height-32)/(double)DesignH));
            factor=Math.Max(.52,factor);
            ClientSize=new Size((int)Math.Round(DesignW*factor),(int)Math.Round(DesignH*factor));
            MinimumSize=ClientSize;
            MaximumSize=ClientSize;
            LoadArt();
            LoadNews();
            Shown+=(s,e)=>{
                if(!File.Exists(localIni))
                    File.WriteAllText(localIni,"ManifestURL="+DefaultManifestUrl+"\r\nGameExecutable=Game.exe\r\n",Encoding.UTF8);
                StartUpdate(false);
                CheckServer();
            };
            statusTimer=new System.Windows.Forms.Timer();
            statusTimer.Interval=60000;
            statusTimer.Tick+=(s,e)=>CheckServer();
            statusTimer.Start();
        }
        void LoadArt()
        {
            string path=Path.Combine(root,"launcher-background.png");
            if(!File.Exists(path))return;
            Bitmap next=null;
            try{
                byte[] bytes=File.ReadAllBytes(path);
                using(var mem=new MemoryStream(bytes))
                using(var img=Image.FromStream(mem)){
                    if(img.Width!=DesignW||img.Height!=DesignH)throw new IOException("Arte invalida do launcher.");
                    next=new Bitmap(img);
                }
                Bitmap prev=artwork;artwork=next;next=null;
                if(prev!=null)prev.Dispose();
            }catch(Exception){if(next!=null)next.Dispose();}
        }
        void LoadNews()
        {
            string path=Path.Combine(root,"launcher-news.txt");
            if(!File.Exists(path))return;
            try{
                var list=new List<string[]>();
                foreach(string raw in File.ReadAllLines(path,Encoding.UTF8)){
                    if(list.Count==3)break;
                    if(string.IsNullOrWhiteSpace(raw)||raw.StartsWith("#"))continue;
                    string[] parts=raw.Split('|');
                    if(parts.Length!=3||parts[0].Length>55||parts[1].Length>25||parts[2].Length>180)continue;
                    list.Add(parts);
                }
                if(list.Count>0)news=list.ToArray();
            }catch(IOException){}
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using(var b=new SolidBrush(Color.FromArgb(2,12,28)))e.Graphics.FillRectangle(b,ClientRectangle);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g=e.Graphics;
            g.InterpolationMode=InterpolationMode.HighQualityBicubic;
            g.SmoothingMode=SmoothingMode.AntiAlias;
            g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.ScaleTransform(ClientSize.Width/(float)DesignW,ClientSize.Height/(float)DesignH);
            if(artwork!=null)g.DrawImage(artwork,new Rectangle(0,0,DesignW,DesignH));
            else {
                using(var fill=new LinearGradientBrush(new Rectangle(0,0,DesignW,DesignH),
                    Color.FromArgb(5,20,44),Color.FromArgb(35,23,72),0f))
                    g.FillRectangle(fill,0,0,DesignW,DesignH);
                Print(g,"StormPT",95,370,90,Color.FromArgb(228,195,124),FontStyle.Bold,680);
                Print(g,"Arte oficial sera instalada automaticamente.",105,495,22,Color.White,FontStyle.Regular,700);
            }
            DrawNews(g);
            DrawUpdate(g);
            DrawServer(g);
            DrawFooter(g);
            if(busy) {
                using(var dark=new SolidBrush(Color.FromArgb(198,5,15,43)))g.FillRectangle(dark,PlayZone);
                Print(g,"ATUALIZANDO...",986,877,30,Color.FromArgb(218,231,255),FontStyle.Bold,360);
            } else if(hover==1){
                using(var pen=new Pen(Color.FromArgb(215,163,214,255),4f))g.DrawRectangle(pen,PlayZone);
            }
            if(hover==2){
                using(var pen=new Pen(Color.FromArgb(225,143,207,255),2.5f))g.DrawRectangle(pen,VerifyZone);
            }
        }
        void DrawNews(Graphics g)
        {
            using(var fill=new SolidBrush(Color.FromArgb(250,2,16,35)))
                g.FillRectangle(fill,new Rectangle(1049,165,338,392));
            using(var pen=new Pen(Color.FromArgb(95,185,142,77),1f)){
                g.DrawLine(pen,1051,294,1361,294);
                g.DrawLine(pen,1051,427,1361,427);
            }
            for(int i=0;i<3;i++){
                string[] item=i<news.Length?news[i]:new string[]{"Sem outras noticias","",""};
                int y=177+i*132;
                Print(g,item[0],1059,y,18,Color.FromArgb(255,230,195),FontStyle.Bold,289);
                Print(g,item[1],1059,y+34,13,Color.FromArgb(125,195,253),FontStyle.Regular,283);
                Print(g,item[2],1059,y+55,14,Color.FromArgb(215,230,247),FontStyle.Regular,287);
            }
        }
        void DrawUpdate(Graphics g)
        {
            using(var fill=new SolidBrush(Color.FromArgb(249,2,16,35)))
                g.FillRectangle(fill,new Rectangle(895,653,492,126));
            Color label=busy?Color.FromArgb(248,217,145):percentage==100?
                Color.FromArgb(125,246,169):Color.FromArgb(230,234,245);
            Print(g,uiStatus,908,664,18,label,FontStyle.Bold,440);
            using(var background=new SolidBrush(Color.FromArgb(36,68,98)))g.FillRectangle(background,906,713,399,17);
            using(var green=new LinearGradientBrush(new Rectangle(906,713,400,18),
                Color.FromArgb(16,168,218),Color.FromArgb(40,237,136),0f))
                g.FillRectangle(green,906,713,(int)(399f*percentage/100f),17);
            using(var border=new Pen(Color.FromArgb(129,201,228)))
                g.DrawRectangle(border,906,713,399,17);
            Print(g,percentage+"%",1310,709,20,Color.White,FontStyle.Bold,68);
            Print(g,uiDetail,905,735,13,Color.FromArgb(173,209,236),FontStyle.Regular,451);
        }
        void DrawServer(Graphics g)
        {
            using(var fill=new SolidBrush(Color.FromArgb(247,3,17,32))){
                g.FillRectangle(fill,new Rectangle(111,899,315,70));
                g.FillRectangle(fill,new Rectangle(427,888,383,82));
            }
            Color tint=online==1?Color.FromArgb(64,245,146):
                online==0?Color.FromArgb(252,129,109):Color.FromArgb(207,202,164);
            string state=online==1?"Conexao disponivel":online==0?"Servidor indisponivel":"Verificando...";
            Print(g,state,114,906,20,tint,FontStyle.Bold,310);
            Print(g,"Jogadores conectados: nao informado",114,938,13,Color.FromArgb(174,200,222),FontStyle.Regular,318);
            Print(g,"Login e mundo",447,900,16,Color.FromArgb(230,222,194),FontStyle.Bold,365);
            Print(g,"163.176.128.81 : 1997",448,927,16,Color.FromArgb(122,209,252),FontStyle.Regular,365);
            if(online!=1){
                using(var overlay=new SolidBrush(Color.FromArgb(245,4,18,34)))g.FillRectangle(overlay,680,829,125,36);
                Print(g,online==0?"Offline":"Verificando",687,836,15,tint,FontStyle.Bold,115);
            }
        }
        void DrawFooter(Graphics g)
        {
            using(var shade=new SolidBrush(Color.FromArgb(247,4,15,33)))
                g.FillRectangle(shade,new Rectangle(18,1017,511,42));
            Print(g,((char)169)+" 2026 StormPT. Todos os direitos reservados.",27,1035,13,
                  Color.FromArgb(183,211,240),FontStyle.Regular,490);
        }
        static void Print(Graphics g,string text,int x,int y,int size,Color color,FontStyle style,int maxWidth)
        {
            using(var f=new Font("Georgia",size,style,GraphicsUnit.Pixel))
            using(var brush=new SolidBrush(color))
            using(var format=new StringFormat()){
                format.Trimming=StringTrimming.EllipsisCharacter;
                g.DrawString(text??"",f,brush,new RectangleF(x,y,maxWidth,68f),format);
            }
        }
        int Hit(Point point)
        {
            int x=(int)(point.X*DesignW/(double)ClientSize.Width);
            int y=(int)(point.Y*DesignH/(double)ClientSize.Height);
            Point p=new Point(x,y);
            if(CloseZone.Contains(p))return 5;
            if(MinZone.Contains(p))return 6;
            if(GearZone.Contains(p))return 4;
            if(PlayZone.Contains(p))return 1;
            if(VerifyZone.Contains(p))return 2;
            if(AllNewsZone.Contains(p))return 3;
            return 0;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int hit=Hit(e.Location);
            Cursor=hit>0?Cursors.Hand:Cursors.Default;
            if(hover!=hit){hover=hit;Invalidate();}
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hover=0;Cursor=Cursors.Default;Invalidate();
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if(e.Button!=MouseButtons.Left)return;
            int hit=Hit(e.Location);
            if(hit==1){if(!busy)StartUpdate(true);return;}
            if(hit==2){if(!busy)StartUpdate(false);return;}
            if(hit==3){Process.Start(new ProcessStartInfo("https://stormpt.hoststorm.cloud/"){UseShellExecute=true});return;}
            if(hit==4){OpenSettings();return;}
            if(hit==5){Close();return;}
            if(hit==6){WindowState=FormWindowState.Minimized;return;}
            int y=(int)(e.Y*DesignH/(double)ClientSize.Height);
            if(y<65){ReleaseCapture();SendMessage(Handle,0xA1,0x2,0);}
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if(e.KeyCode==Keys.Enter&&!busy){StartUpdate(true);e.Handled=true;}
            if(e.KeyCode==Keys.F5&&!busy){StartUpdate(false);e.Handled=true;}
            if(e.KeyCode==Keys.Escape){Close();e.Handled=true;}
        }
        void OpenSettings()
        {
            try{
                if(!File.Exists(localIni))File.WriteAllText(localIni,"ManifestURL="+DefaultManifestUrl+"\r\nGameExecutable=Game.exe\r\n",Encoding.UTF8);
                Process.Start(new ProcessStartInfo("notepad.exe","\""+localIni+"\""){UseShellExecute=true});
            }catch(Exception ex){MessageBox.Show(ex.Message,"StormPT");}
        }
        void CheckServer()
        {
            Task.Factory.StartNew(()=>{
                try{
                    using(var c=new TcpClient()){
                        IAsyncResult r=c.BeginConnect("163.176.128.81",1997,null,null);
                        if(!r.AsyncWaitHandle.WaitOne(1700))return false;
                        c.EndConnect(r);
                        return c.Connected;
                    }
                }catch{return false;}
            }).ContinueWith(t=>{
                if(IsDisposed||!IsHandleCreated)return;
                BeginInvoke((MethodInvoker)(()=>{
                    online=t.Status==TaskStatus.RanToCompletion&&t.Result?1:0;
                    Invalidate();
                }));
            });
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if(statusTimer!=null){statusTimer.Stop();statusTimer.Dispose();}
            if(artwork!=null)artwork.Dispose();
            base.OnFormClosed(e);
        }
        string Setting(string name,string fallback)
        {
            if(!File.Exists(localIni))return fallback;
            foreach(string line in File.ReadAllLines(localIni))
            {
                var s=line.Trim();if(s.StartsWith("#")||s.StartsWith(";"))continue;
                int ix=s.IndexOf('=');if(ix>0&&string.Equals(s.Substring(0,ix).Trim(),name,StringComparison.OrdinalIgnoreCase))return s.Substring(ix+1).Trim();
            }
            return fallback;
        }
        void Report(string message,int percent)
        {
            if(IsDisposed)return;
            if(InvokeRequired){
                if(IsHandleCreated)BeginInvoke((MethodInvoker)(()=>Report(message,percent)));
                return;
            }
            uiStatus=message;
            percentage=Math.Max(0,Math.Min(100,percent));
            Invalidate();
        }
        void StartUpdate(bool playAfterUpdate)
        {
            if(busy)return;
            busy=true;percentage=0;
            uiStatus="Verificando atualizacoes...";
            uiDetail="Aguarde enquanto os arquivos sao conferidos.";
            Invalidate();
            Task.Factory.StartNew(()=>CheckAndUpdate()).ContinueWith(t=>{
                if(IsDisposed||!IsHandleCreated)return;
                BeginInvoke((MethodInvoker)(()=>{
                    busy=false;
                    if(t.IsFaulted){
                        uiStatus="Falha na atualizacao";
                        uiDetail=t.Exception.GetBaseException().Message;
                    }else if(t.Result){
                        uiStatus="Cliente atualizado";
                        uiDetail="Tudo pronto para jogar. Arquivos verificados por SHA-256.";
                        percentage=100;
                        LoadArt();
                        LoadNews();
                        if(playAfterUpdate)LaunchGame();
                    }else{
                        uiStatus="Feed nao configurado";
                        uiDetail="A atualizacao automatica nao esta disponivel.";
                    }
                    Invalidate();
                }));
            });
        }
        bool CheckAndUpdate()
        {
            string manifestString=Setting("ManifestURL",DefaultManifestUrl);
            if(string.IsNullOrWhiteSpace(manifestString))manifestString=DefaultManifestUrl;
            if(Process.GetProcessesByName("Game").Length>0||Process.GetProcessesByName("Game33").Length>0)
                throw new IOException("Feche o jogo para verificar atualizacoes.");
            Uri manifestUrl=ValidateUrl(manifestString,null);
            if(!manifestUrl.AbsolutePath.EndsWith("/manifest.txt",StringComparison.OrdinalIgnoreCase))
                throw new IOException("Feed deve terminar com /manifest.txt.");
            Uri baseUrl=new Uri(manifestUrl,".");
            Report("Verificando assinatura...",5);
            byte[] textBytes;string sigText;
            using(var wc=new StrictWebClient()){
                textBytes=wc.DownloadData(new Uri(manifestUrl.AbsoluteUri+"?t="+DateTime.UtcNow.Ticks));
                if(textBytes.Length>2*1024*1024)throw new IOException("Manifesto muito grande.");
                sigText=wc.DownloadString(new Uri(baseUrl,"manifest.sig?t="+DateTime.UtcNow.Ticks));
            }
            byte[] signature=Convert.FromBase64String(sigText.Trim());
            using(var rsa=new RSACryptoServiceProvider(new CspParameters(24))){
                rsa.FromXmlString(Entry.PublicKey);
                if(!rsa.VerifyData(textBytes,CryptoConfig.MapNameToOID("SHA256"),signature))throw new IOException("Assinatura da atualizacao invalida.");
            }
            string manifest=Encoding.UTF8.GetString(textBytes).TrimStart('\uFEFF');
            var lines=manifest.Replace("\r","").Split('\n');
            if(lines.Length==0 || !lines[0].StartsWith("VERSION|"))throw new IOException("Manifesto incompleto.");
            Version ver; if(!Version.TryParse(lines[0].Substring(8).Trim(),out ver))throw new IOException("Versao invalida.");
            string updates=Path.Combine(root,".stormpt-updates");Directory.CreateDirectory(updates);
            string state=Path.Combine(updates,"installed-version.txt");
            Version prev;
            if(File.Exists(state)&&Version.TryParse(File.ReadAllText(state).Trim(),out prev)&&ver<prev)
                throw new IOException("Downgrade bloqueado.");
            var items=new List<ManifestItem>();var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for(int i=1;i<lines.Length;i++){
                if(string.IsNullOrWhiteSpace(lines[i]))continue;
                string[] parts=lines[i].Split('|');
                if(parts.Length<5 || (parts[0]!="FILE" && parts[0]!="ARCHIVE"))
                    throw new IOException("Entrada invalida no manifesto.");
                if(parts[0]=="FILE" && parts.Length!=5)throw new IOException("FILE malformado.");
                if(parts[0]=="ARCHIVE" && parts.Length!=7)throw new IOException("ARCHIVE malformado.");
                var f=new ManifestItem(){Relative=parts[1],Hash=parts[3]};
                if(!long.TryParse(parts[2],out f.Size)||f.Size<0||f.Size>4L*1024*1024*1024||!ValidSha(f.Hash))
                    throw new IOException("Tamanho/hash invalido.");
                SafeFile(f.Relative);
                if(!names.Add(f.Relative.Replace('\\','/')))throw new IOException("Arquivo repetido.");
                if(parts[0]=="FILE"){f.Url=parts[4];ValidateUrl(f.Url,baseUrl);}
                else {
                    if(!long.TryParse(parts[4],out f.ArchiveSize)||f.ArchiveSize<=0||f.ArchiveSize>4L*1024*1024*1024)
                        throw new IOException("Arquivo compactado muito grande.");
                    f.ArchiveHash=parts[5];
                    if(!ValidSha(f.ArchiveHash))throw new IOException("SHA256 compactado invalido.");
                    f.Parts=parts[6].Split(',');
                    if(f.Parts.Length<1||f.Parts.Length>64)throw new IOException("Quantidade de partes invalida.");
                    foreach(var chunk in f.Parts)ValidateUrl(chunk,baseUrl);
                }
                items.Add(f);
            }
            if(items.Count>20000)throw new IOException("Numero de arquivos excessivo.");
            string staged=null;string stagedHash=null;
            for(int i=0;i<items.Count;i++){
                var f=items[i];
                string normalized=f.Relative.Replace('\\','/');
                if(normalized.Equals("Settings.ini",StringComparison.OrdinalIgnoreCase)||normalized.Equals("game.ini",StringComparison.OrdinalIgnoreCase)
                    ||normalized.StartsWith("save/",StringComparison.OrdinalIgnoreCase)||normalized.StartsWith("savedata/",StringComparison.OrdinalIgnoreCase))
                    continue;
                string target=SafeFile(f.Relative);
                Report("Verificando: "+normalized,10+(int)(75.0*i/Math.Max(1,items.Count)));
                if(File.Exists(target)&&string.Equals(Entry.Hash(target),f.Hash,StringComparison.OrdinalIgnoreCase))continue;
                string part=Path.Combine(updates,i.ToString()+".part");
                if(f.Parts!=null)DownloadArchive(f,baseUrl,updates,part);
                else Download(ValidateUrl(f.Url,baseUrl),part,f.Size);
                if(new FileInfo(part).Length!=f.Size || !string.Equals(Entry.Hash(part),f.Hash,StringComparison.OrdinalIgnoreCase))
                    throw new IOException("Arquivo corrompido: "+normalized);
                if(normalized.Equals("launcher.exe",StringComparison.OrdinalIgnoreCase)){
                    staged=Path.Combine(updates,"self.next.exe");File.Copy(part,staged,true);stagedHash=f.Hash;File.Delete(part);continue;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                string backup=target+".stormpt.bak";
                if(File.Exists(backup))File.Delete(backup);
                if(File.Exists(target))File.Move(target,backup);
                try{File.Move(part,target);}
                catch{if(!File.Exists(target)&&File.Exists(backup))File.Move(backup,target);throw;}
            }
            File.WriteAllText(state,ver.ToString(),Encoding.UTF8);
            if(staged!=null){
                string helper=Path.Combine(updates,"self-helper.exe");File.Copy(staged,helper,true);
                var pi=new ProcessStartInfo(helper){UseShellExecute=true,WorkingDirectory=root};
                pi.Arguments="--replace "+Quote(Application.ExecutablePath)+" "+Quote(staged)+" "+stagedHash+" "+Process.GetCurrentProcess().Id;
                Process.Start(pi);
                BeginInvoke((MethodInvoker)(()=>Close()));
            }
            return true;
        }
        static bool ValidSha(string h)
        {
            if(h==null||h.Length!=64)return false;
            foreach(char c in h)if(!Uri.IsHexDigit(c))return false;
            return true;
        }
        void DownloadArchive(ManifestItem f,Uri baseUrl,string updates,string dest)
        {
            string archive=Path.Combine(updates,"bundle-"+Guid.NewGuid().ToString("N")+".zip");
            try{
                using(var output=new FileStream(archive,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
                    long total=0;
                    byte[] buffer=new byte[65536];
                    foreach(string chunk in f.Parts){
                        using(var wc=new StrictWebClient())
                        using(var input=wc.OpenRead(ValidateUrl(chunk,baseUrl))){
                            int n;
                            while((n=input.Read(buffer,0,buffer.Length))>0){
                                total+=n;
                                if(total>f.ArchiveSize)throw new IOException("Pacote maior que o anunciado.");
                                output.Write(buffer,0,n);
                            }
                        }
                    }
                    if(total!=f.ArchiveSize)throw new IOException("Pacote baixado incompleto.");
                }
                if(!string.Equals(Entry.Hash(archive),f.ArchiveHash,StringComparison.OrdinalIgnoreCase))
                    throw new IOException("Pacote compactado nao passou SHA256.");
                using(var stream=File.OpenRead(archive))
                using(var zip=new ZipArchive(stream,ZipArchiveMode.Read)){
                    var entry=zip.GetEntry(f.Relative.Replace('\\','/'));
                    if(entry==null||entry.Length!=f.Size)throw new IOException("Arquivo esperado ausente no ZIP.");
                    using(var input=entry.Open())
                    using(var output=new FileStream(dest,FileMode.Create,FileAccess.Write,FileShare.None)){
                        long size=0;byte[] buffer=new byte[65536];int n;
                        while((n=input.Read(buffer,0,buffer.Length))>0){
                            size+=n;if(size>f.Size)throw new IOException("Arquivo descompactado excedeu o limite.");
                            output.Write(buffer,0,n);
                        }
                        if(size!=f.Size)throw new IOException("Arquivo extraido incompleto.");
                    }
                }
            }finally{try{if(File.Exists(archive))File.Delete(archive);}catch{}}
        }
        static string Quote(string s){return "\""+s.Replace("\"","")+"\"";}
        void Download(Uri url,string part,long max)
        {
            using(var wc=new StrictWebClient())
            using(var input=wc.OpenRead(url))
            using(var output=new FileStream(part,FileMode.Create,FileAccess.Write,FileShare.None)){
                byte[] buf=new byte[65536]; long total=0;int read;
                while((read=input.Read(buf,0,buf.Length))>0){
                    total+=read;if(total>max)throw new IOException("Download maior que o permitido.");
                    output.Write(buf,0,read);
                }
            }
        }
        Uri ValidateUrl(string url,Uri baseUrl)
        {
            Uri uri;
            if(!Uri.TryCreate(url,UriKind.Absolute,out uri)){
                if(baseUrl==null||!Uri.TryCreate(baseUrl,url,out uri))throw new IOException("URL invalida.");
            }
            if(uri.Scheme!=Uri.UriSchemeHttps||uri.UserInfo.Length>0)throw new IOException("Atualizacao exige HTTPS.");
            if(baseUrl!=null && (!uri.Host.Equals(baseUrl.Host,StringComparison.OrdinalIgnoreCase)||uri.Port!=baseUrl.Port
                ||!uri.AbsolutePath.StartsWith(baseUrl.AbsolutePath,StringComparison.Ordinal)))throw new IOException("URL externa ao feed.");
            return uri;
        }
        string SafeFile(string rel)
        {
            if(string.IsNullOrWhiteSpace(rel)||Path.IsPathRooted(rel)||rel.IndexOf(':')>=0 || rel.IndexOf('|')>=0)throw new IOException("Caminho invalido.");
            string[] parts=rel.Replace('\\','/').Split('/');
            foreach(string part in parts)if(part.Length==0||part=="."||part=="..")throw new IOException("Caminho fora do jogo.");
            string full=Path.GetFullPath(Path.Combine(root,rel.Replace('/',Path.DirectorySeparatorChar)));
            if(!full.StartsWith(Path.GetFullPath(root),StringComparison.OrdinalIgnoreCase))throw new IOException("Path traversal.");
            string probe=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
            foreach(string part in parts){
                probe=Path.Combine(probe,part);
                if((File.Exists(probe)||Directory.Exists(probe))&&(File.GetAttributes(probe)&FileAttributes.ReparsePoint)!=0)throw new IOException("Link nao permitido.");
            }
            return full;
        }
        void LaunchGame()
        {
            try{
                string rel=Setting("GameExecutable","Game.exe");
                string path=SafeFile(rel);
                if(!File.Exists(path))throw new IOException("Game.exe nao encontrado. Coloque o launcher na pasta Cliente.");
                Process.Start(new ProcessStartInfo(path){UseShellExecute=true,WorkingDirectory=Path.GetDirectoryName(path)});
            }catch(Exception ex){MessageBox.Show(ex.Message,"StormPT",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        }
    }
}
