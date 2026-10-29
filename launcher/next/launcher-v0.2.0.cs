using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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
        public string Relative, Hash, Url;
        public long Size;
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
        readonly Label status=new Label();
        readonly Label detail=new Label();
        readonly ProgressBar progress=new ProgressBar();
        readonly Button play=new Button();
        readonly Button verify=new Button();
        readonly Button folder=new Button();
        bool busy;
        readonly string root=AppDomain.CurrentDomain.BaseDirectory;
        readonly string localIni;
        public Launcher()
        {
            localIni=Path.Combine(root,"launcher.ini");
            string previous=Path.Combine(root,"StormPTLauncher.ini");
            if(!File.Exists(localIni) && File.Exists(previous)) File.Copy(previous,localIni);
            Text="STORMPT - LAUNCHER";
            ClientSize=new Size(960,570); MinimumSize=new Size(960,570); MaximumSize=new Size(960,570);
            StartPosition=FormStartPosition.CenterScreen;
            FormBorderStyle=FormBorderStyle.FixedSingle; MaximizeBox=false;
            BackColor=Color.FromArgb(10,14,28); ForeColor=Color.White; Font=new Font("Segoe UI",10);
            var hero=new GradientPanel(){Location=new Point(0,0),Size=new Size(960,265)};
            var eyebrow=new Label(){Text="STORMPT / PRISTON TALE",ForeColor=Color.FromArgb(167,211,255),Font=new Font("Segoe UI",10,FontStyle.Bold),Location=new Point(46,31),Size=new Size(500,25),BackColor=Color.Transparent};
            var brand=new Label(){Text="STORMPT",ForeColor=Color.White,Font=new Font("Segoe UI",43,FontStyle.Bold),Location=new Point(40,70),Size=new Size(630,80),BackColor=Color.Transparent};
            var subtitle=new Label(){Text="SEU MUNDO. SUA AVENTURA.",ForeColor=Color.FromArgb(205,220,238),Font=new Font("Segoe UI",12),Location=new Point(46,157),Size=new Size(600,35),BackColor=Color.Transparent};
            var version=new Label(){Text="LAUNCHER  /  v0.2",ForeColor=Color.FromArgb(213,227,246),Font=new Font("Segoe UI",10,FontStyle.Bold),Location=new Point(732,31),Size=new Size(188,25),TextAlign=ContentAlignment.MiddleRight,BackColor=Color.Transparent};
            var ornament=new Label(){Text="S",ForeColor=Color.FromArgb(100,170,246),Font=new Font("Segoe UI",96,FontStyle.Bold),Location=new Point(733,80),Size=new Size(180,150),BackColor=Color.Transparent,TextAlign=ContentAlignment.MiddleCenter};
            hero.Controls.Add(eyebrow);hero.Controls.Add(brand);hero.Controls.Add(subtitle);hero.Controls.Add(version);hero.Controls.Add(ornament);
            var caption=new Label(){Text="ATUALIZACAO AUTOMATICA",ForeColor=Color.FromArgb(140,160,186),Font=new Font("Segoe UI",10,FontStyle.Bold),Location=new Point(47,283),Size=new Size(650,26)};
            status.Text="Verificando atualizacoes...";
            status.Font=new Font("Segoe UI",19,FontStyle.Bold);
            status.Location=new Point(45,316);status.Size=new Size(850,45);
            detail.Text="Seu jogo sera verificado antes de iniciar.";
            detail.ForeColor=Color.FromArgb(177,195,219);
            detail.Location=new Point(48,362);detail.Size=new Size(850,35);
            progress.Location=new Point(48,411);progress.Size=new Size(862,13);progress.Maximum=100;progress.Style=ProgressBarStyle.Continuous;
            Style(play,"JOGAR STORMPT",new Point(48,450),new Size(345,65),Color.FromArgb(27,148,235));
            Style(verify,"VERIFICAR / REPARAR",new Point(408,450),new Size(294,65),Color.FromArgb(36,58,91));
            Style(folder,"ABRIR PASTA",new Point(717,450),new Size(193,65),Color.FromArgb(36,58,91));
            play.Font=new Font("Segoe UI",13,FontStyle.Bold);
            play.Click+=(s,e)=>StartUpdate(true);
            verify.Click+=(s,e)=>StartUpdate(false);
            folder.Click+=(s,e)=>Process.Start("explorer.exe",root);
            Controls.Add(hero);Controls.Add(caption);Controls.Add(status);Controls.Add(detail);Controls.Add(progress);Controls.Add(play);Controls.Add(verify);Controls.Add(folder);
            Shown+=(s,e)=>{
                if(!File.Exists(localIni))File.WriteAllText(localIni,"# ManifestURL=https://seu-dominio/updates/manifest.txt\r\nManifestURL=\r\nGameExecutable=Game.exe\r\n",Encoding.UTF8);
                StartUpdate(false);
            };
        }
        internal sealed class GradientPanel:Panel
        {
            protected override void OnPaintBackground(PaintEventArgs e)
            {
                using(var brush=new System.Drawing.Drawing2D.LinearGradientBrush(ClientRectangle,Color.FromArgb(18,46,80),Color.FromArgb(38,24,89),0f))
                    e.Graphics.FillRectangle(brush,ClientRectangle);
            }
        }
        void Style(Button b,string title,Point at,Size size,Color color)
        {
            b.Text=title;b.Location=at;b.Size=size;b.BackColor=color;b.ForeColor=Color.White;b.Font=new Font("Segoe UI",10,FontStyle.Bold);
            b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderSize=0;b.UseVisualStyleBackColor=false;b.Cursor=Cursors.Hand;
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
            if(InvokeRequired){BeginInvoke((MethodInvoker)(()=>Report(message,percent)));return;}
            status.Text=message; progress.Value=Math.Max(0,Math.Min(100,percent));
        }
        void StartUpdate(bool playAfterUpdate)
        {
            if(busy)return;
            busy=true;verify.Enabled=false;play.Enabled=false;progress.Value=0;
            detail.Text="Consultando o manifesto e verificando arquivos...";
            Task.Factory.StartNew(()=>CheckAndUpdate()).ContinueWith(t=>{
                if(IsDisposed)return;
                BeginInvoke((MethodInvoker)(()=>{
                    busy=false;verify.Enabled=true;play.Enabled=true;
                    if(t.IsFaulted)
                    {
                        status.Text="Atualizacao indisponivel";
                        detail.Text=t.Exception.GetBaseException().Message;
                    }
                    else if(t.Result)
                    {
                        status.Text="Tudo pronto para jogar";
                        detail.Text="Arquivos atualizados e verificados com assinatura digital.";
                        progress.Value=100;
                        if(playAfterUpdate)LaunchGame();
                    }
                    else
                    {
                        status.Text="Atualizacao nao configurada";
                        detail.Text="O servidor de atualizacoes ainda precisa ser publicado.";
                        if(playAfterUpdate)LaunchGame();
                    }
                }));
            });
        }
        bool CheckAndUpdate()
        {
            string manifestString=Setting("ManifestURL","");
            if(string.IsNullOrWhiteSpace(manifestString)){Report("Verificando atualizacoes...",0);return false;}
            if(Process.GetProcessesByName("Game").Length>0||Process.GetProcessesByName("Game33").Length>0)
                throw new IOException("Feche o jogo para verificar atualizacoes.");
            Uri manifestUrl=ValidateUrl(manifestString,null);
            if(!manifestUrl.AbsolutePath.EndsWith("/manifest.txt",StringComparison.OrdinalIgnoreCase))
                throw new IOException("Feed deve terminar com /manifest.txt.");
            Uri baseUrl=new Uri(manifestUrl,".");
            Report("Verificando assinatura...",5);
            byte[] textBytes;string sigText;
            using(var wc=new StrictWebClient()){
                textBytes=wc.DownloadData(manifestUrl);
                if(textBytes.Length>2*1024*1024)throw new IOException("Manifesto muito grande.");
                sigText=wc.DownloadString(new Uri(baseUrl,"manifest.sig"));
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
                if(parts.Length!=5 || parts[0]!="FILE")throw new IOException("Entrada invalida no manifesto.");
                var f=new ManifestItem(){Relative=parts[1],Hash=parts[3],Url=parts[4]};
                if(!long.TryParse(parts[2],out f.Size)||f.Size<0||f.Size>4L*1024*1024*1024 || f.Hash.Length!=64)throw new IOException("Tamanho/hash invalido.");
                foreach(char c in f.Hash)if(!Uri.IsHexDigit(c))throw new IOException("Hash invalido.");
                SafeFile(f.Relative);
                if(!names.Add(f.Relative.Replace('\\','/')))throw new IOException("Arquivo repetido.");
                ValidateUrl(f.Url,baseUrl);
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
                Download(ValidateUrl(f.Url,baseUrl),part,f.Size);
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
