using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Xml.Serialization;
using Microsoft.Win32;
using System.Threading;

[Serializable] public class Settings {
 public string ClientPath = @"C:\Drcom\DrUpdateClient\DrMain.exe";
 public string WifiNames = "JLU.PC";
 public string Gateways = "";
 public bool Paused = false;
}
public class Native {
 public delegate bool EnumProc(IntPtr h, IntPtr p);
 [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc c, IntPtr p);
 [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr h, EnumProc c, IntPtr p);
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint p);
 [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr h,StringBuilder s,int n);
 [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr h,StringBuilder s,int n);
 [DllImport("user32.dll")] public static extern IntPtr GetParent(IntPtr h);
 [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr h,int n);
 [DllImport("user32.dll")] public static extern bool IsWindowEnabled(IntPtr h);
 [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll")] public static extern int GetDlgCtrlID(IntPtr h);
 [DllImport("user32.dll", SetLastError=true)] public static extern IntPtr SendMessageTimeout(IntPtr h,uint m,IntPtr w,IntPtr l,uint flags,uint timeout,out IntPtr result);
 [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h,int cmd);
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
 [DllImport("user32.dll",SetLastError=true)] public static extern bool PostMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
 [DllImport("user32.dll")] public static extern bool ChangeWindowMessageFilterEx(IntPtr h,uint m,uint action,IntPtr change);
 public delegate void EventProc(IntPtr hook,uint evt,IntPtr hwnd,int obj,int child,uint thread,uint time);
 [DllImport("user32.dll")] public static extern IntPtr SetWinEventHook(uint min,uint max,IntPtr module,EventProc callback,uint pid,uint tid,uint flags);
 [DllImport("user32.dll")] public static extern bool UnhookWinEvent(IntPtr hook);
 [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool QueryFullProcessImageName(IntPtr h,uint flags,StringBuilder path,ref int size);
 [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
 public static string ProcessPath(int pid){IntPtr h=OpenProcess(0x1000,false,pid);if(h==IntPtr.Zero)return "";try{var b=new StringBuilder(1024);int n=1024;return QueryFullProcessImageName(h,0,b,ref n)?b.ToString():"";}finally{CloseHandle(h);}}
 [DllImport("wlanapi.dll")] static extern uint WlanOpenHandle(uint v,IntPtr r,out uint ver,out IntPtr h);
 [DllImport("wlanapi.dll")] static extern uint WlanEnumInterfaces(IntPtr h,IntPtr r,out IntPtr p);
 [DllImport("wlanapi.dll")] static extern uint WlanQueryInterface(IntPtr h,ref Guid id,int op,IntPtr r,out uint size,out IntPtr data,out int type);
 [DllImport("wlanapi.dll")] static extern void WlanFreeMemory(IntPtr p);
 [DllImport("wlanapi.dll")] static extern uint WlanCloseHandle(IntPtr h,IntPtr r);
 public static string Text(IntPtr h) { var b=new StringBuilder(256);GetWindowText(h,b,256);return b.ToString(); }
 public static string Class(IntPtr h) { var b=new StringBuilder(256);GetClassName(h,b,256);return b.ToString(); }
 public static bool OwnVisible(IntPtr h) {return (GetWindowLong(h,-16)&0x10000000)!=0;}
 public static string Wifi(Guid id) {
  IntPtr h=IntPtr.Zero,d=IntPtr.Zero; uint ver,size;int type;
  try {if(WlanOpenHandle(2,IntPtr.Zero,out ver,out h)!=0)return "";
   if(WlanQueryInterface(h,ref id,7,IntPtr.Zero,out size,out d,out type)!=0 || size<568)return "";
   int state=Marshal.ReadInt32(d,0),len=Marshal.ReadInt32(d,520); if(state!=1||len<0||len>32)return "";
   byte[] bytes=new byte[len];Marshal.Copy(IntPtr.Add(d,524),bytes,0,len);return Encoding.UTF8.GetString(bytes);
  } catch{return "";} finally {if(d!=IntPtr.Zero)WlanFreeMemory(d);if(h!=IntPtr.Zero)WlanCloseHandle(h,IntPtr.Zero);}
 }
}
public class ClientView {
 public IntPtr Main, Login, Logout, LogoutConfirm;
 public bool Online, Ready, Dialog;
 public int Pid;
 public static ClientView Read() {
  var v=new ClientView();var ids=Process.GetProcessesByName("DrClient").Select(p=>p.Id).ToArray();IntPtr formLogin=IntPtr.Zero,loginForm=IntPtr.Zero;int loginPid=0;
  Native.EnumWindows(delegate(IntPtr h,IntPtr l){uint pid;Native.GetWindowThreadProcessId(h,out pid);if(!ids.Contains((int)pid)||Native.Class(h)!="#32770")return true;
   IntPtr login=IntPtr.Zero,logout=IntPtr.Zero;int branches=0;bool isLoginForm=Native.Text(h)=="吉林大学校园网认证客户端";
   Native.EnumChildWindows(h,delegate(IntPtr c,IntPtr p){if(Native.Class(c)=="Button") {string t=Native.Text(c);if(Native.Class(Native.GetParent(c))=="NetStateWin"){branches++;if(Native.OwnVisible(c)&&Native.OwnVisible(Native.GetParent(c))){if(t=="登录")login=c;if(t=="注销")logout=c;}}if(isLoginForm&&Native.GetDlgCtrlID(c)==1002&&t=="登录"&&Native.OwnVisible(c)){formLogin=c;loginForm=h;loginPid=(int)pid;}}return true;},IntPtr.Zero);
   if(branches>=2){v.Main=h;v.Login=login;v.Logout=logout;v.Pid=(int)pid;v.Online=logout!=IntPtr.Zero;v.Ready=login!=IntPtr.Zero&&Native.IsWindowEnabled(login);}else if(Native.IsWindowVisible(h) && Native.Text(h)!="校园网之窗"&&!isLoginForm){v.Dialog=true;if(Native.Text(h)=="注销"){bool exact=false;IntPtr ok=IntPtr.Zero;Native.EnumChildWindows(h,delegate(IntPtr c,IntPtr p){string cls=Native.Class(c);if(cls=="Static"&&Native.Text(c)=="确定要注销的网络吗？")exact=true;if(cls=="Button"&&Native.Text(c)=="确定")ok=c;return true;},IntPtr.Zero);if(exact)v.LogoutConfirm=ok;}}
   return true;},IntPtr.Zero);if(!v.Online&&formLogin!=IntPtr.Zero){v.Main=loginForm;v.Login=formLogin;v.Pid=loginPid;v.Ready=Native.IsWindowEnabled(formLogin);}return v;
 }
}
public class CampusNetworkCandidate {
 public string Name,Ssid;public string[] Gateways;public bool Wireless;
 public override string ToString(){return Name+" · "+(Wireless?Ssid:"有线")+" · "+String.Join(",",Gateways);}
}
public class NetView {
 public static System.Collections.Generic.List<CampusNetworkCandidate> DetectCandidates(){var result=new System.Collections.Generic.List<CampusNetworkCandidate>();foreach(var ni in NetworkInterface.GetAllNetworkInterfaces()){
  bool wireless=ni.NetworkInterfaceType==NetworkInterfaceType.Wireless80211;
  if(ni.OperationalStatus!=OperationalStatus.Up||(!wireless&&ni.NetworkInterfaceType!=NetworkInterfaceType.Ethernet))continue;
  if(!wireless&&System.Text.RegularExpressions.Regex.IsMatch(ni.Description,"virtual|tunnel|tap-|vpn|wireguard|hyper-v",System.Text.RegularExpressions.RegexOptions.IgnoreCase))continue;
  var gateways=ni.GetIPProperties().GatewayAddresses.Select(g=>g.Address.ToString()).Where(g=>g!="0.0.0.0"&&g!="::").ToArray();if(gateways.Length==0)continue;
  Guid id;string ssid=wireless&&Guid.TryParse(ni.Id,out id)?Native.Wifi(id):"";
  if(wireless&&String.IsNullOrEmpty(ssid))continue;
  result.Add(new CampusNetworkCandidate{Name=ni.Name,Ssid=ssid,Gateways=gateways,Wireless=wireless});
 }return result;}
 public string Key="",Label="无可用校园网络", Proxy="", Detail="";
 public bool Trusted;
 public static bool Listed(string items,string value) {return Listed(items,value,StringComparison.OrdinalIgnoreCase);}
 public static bool Listed(string items,string value,StringComparison comparison) {return !String.IsNullOrWhiteSpace(items)&&!String.IsNullOrWhiteSpace(value)&&items.Split(new char[]{',',';','\r','\n'},StringSplitOptions.RemoveEmptyEntries).Any(s=>s.Trim().Equals(value,comparison));}
 // SSIDs are case-sensitive: a similarly named network must not inherit enrollment.
 public static bool IsTrustedNetwork(Settings cfg,bool wireless,bool ethernet,string ssid,string[] gateways){return cfg!=null&&gateways!=null&&gateways.Any(g=>Listed(cfg.Gateways,g))&&(wireless?Listed(cfg.WifiNames,ssid,StringComparison.Ordinal):ethernet);}
 public static NetView Read(Settings cfg) {
  var n=new NetView();var keys=new System.Collections.Generic.List<string>();var details=new System.Collections.Generic.List<string>();
  foreach(var ni in NetworkInterface.GetAllNetworkInterfaces()) {
   if(ni.OperationalStatus!=OperationalStatus.Up || ni.NetworkInterfaceType==NetworkInterfaceType.Loopback)continue;
   var ip=ni.GetIPProperties();var gateways=ip.GatewayAddresses.Select(g=>g.Address.ToString()).Where(g=>g!="0.0.0.0"&&g!="::").ToArray();
   if(gateways.Length==0)continue;
   bool wireless=ni.NetworkInterfaceType==NetworkInterfaceType.Wireless80211;Guid id;string ssid=wireless&&Guid.TryParse(ni.Id,out id)?Native.Wifi(id):"";
   var addresses=ip.UnicastAddresses.Select(a=>a.Address.ToString()).OrderBy(s=>s).ToArray();
   keys.Add(ni.Id+"|"+ssid+"|"+String.Join(",",gateways)+"|"+String.Join(",",addresses));
   details.Add(ni.Name+" / "+(wireless?(ssid==""?"Wi-Fi 名称不可读":ssid):"有线或虚拟接口")+" / 网关 "+String.Join(",",gateways));
   if(IsTrustedNetwork(cfg,wireless,ni.NetworkInterfaceType==NetworkInterfaceType.Ethernet,ssid,gateways)) {n.Trusted=true;n.Label=ni.Name+" · "+(wireless?ssid:"已登记有线网络");}
  }
  n.Key=String.Join(";",keys.OrderBy(s=>s));n.Detail=String.Join(Environment.NewLine,details);
  try {using(var k=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings")) {n.Proxy=Convert.ToInt32(k.GetValue("ProxyEnable",0))!=0?"系统代理已开启（由原工具管理）":"系统代理未开启";if(!String.IsNullOrEmpty(Convert.ToString(k.GetValue("AutoConfigURL",""))))n.Proxy+="；存在 PAC";}}catch{n.Proxy="代理状态未知";}
  return n;
 }
}
public partial class App : Form {
 public static readonly string Data=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"JLUConnect");
 static string ConfigFile=Path.Combine(Data,"settings.xml");
 Settings cfg; NotifyIcon tray;Label state,network,proxy;CheckBox paused,startup;System.Windows.Forms.Timer timer;
 DateTime next=DateTime.MinValue,stable=DateTime.UtcNow,grace=DateTime.UtcNow.AddSeconds(15),lastWake=DateTime.MinValue;
 string netKey=null,lastStatus="",hashCache=""; int attempts=0;bool busy=false,quitting=false,lastOnline=false,initialHidden;
 DateTime hashStamp=DateTime.MinValue; bool verified=false;
 DateTime ownLogoutUntil=DateTime.MinValue;Native.EventProc eventCallback;IntPtr eventHook;
 public static Settings LoadSettings(){try{using(var f=File.OpenRead(ConfigFile))return (Settings)new XmlSerializer(typeof(Settings)).Deserialize(f);}catch{return new Settings();}}
 void Save(){Directory.CreateDirectory(Data);string tmp=ConfigFile+".tmp";using(var f=File.Create(tmp))new XmlSerializer(typeof(Settings)).Serialize(f,cfg);if(File.Exists(ConfigFile))File.Replace(tmp,ConfigFile,null);else File.Move(tmp,ConfigFile);}
 public static void Log(string s){try{Directory.CreateDirectory(Data);var p=Path.Combine(Data,"assistant.log");if(File.Exists(p)&&new FileInfo(p).Length>512000){File.Copy(p,p+".1",true);File.Delete(p);}File.AppendAllText(p,DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")+" "+s+Environment.NewLine,Encoding.UTF8);}catch{}}
 public App(bool hidden, bool preview=false){initialHidden=hidden;cfg=LoadSettings();BuildShell();
  paused.CheckedChanged+=delegate{cfg.Paused=paused.Checked;Save();if(!cfg.Paused){attempts=0;next=DateTime.MinValue;grace=DateTime.UtcNow.AddSeconds(3);}Tick();};
  startup.CheckedChanged+=delegate{try{if(File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"installed.flag"))){int code=TaskCommand("/Change /TN JLUConnect "+(startup.Checked?"/Enable":"/Disable"));if(code!=0)throw new Exception("计划任务更新失败，请使用桌面快捷方式打开助手后重试。");using(var k=Registry.CurrentUser.CreateSubKey(@"Software\JLUConnect"))k.SetValue("Startup",startup.Checked?1:0);}else{using(var k=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")){if(startup.Checked)k.SetValue("JLUConnect","\""+Application.ExecutablePath+"\" --background");else k.DeleteValue("JLUConnect",false);}}}catch(Exception ex){MessageBox.Show(ex.Message,"自启动设置失败");}};
  if(preview){state.Text="校园网已连接";network.Text="JLU.PC";return;}
  var menu=new ContextMenuStrip();menu.Items.Add("打开主页面",null,delegate{ShowMain();});menu.Items.Add("连接 / 恢复自动",null,delegate{ManualConnect();});menu.Items.Add("注销并暂停",null,delegate{Logout();});menu.Items.Add("暂停自动连接",null,delegate{paused.Checked=true;});menu.Items.Add("设置",null,delegate{Configure();});menu.Items.Add("退出（保留网络连接）",null,delegate{Quit();});
  tray=new NotifyIcon{Icon=Icon,Text="JLUConnect · 校园网助手",Visible=true,ContextMenuStrip=menu};tray.MouseClick+=delegate(object s,MouseEventArgs e){if(e.Button==MouseButtons.Left)ShowMain();};
  timer=new System.Windows.Forms.Timer{Interval=3000};timer.Tick+=delegate{Tick();};timer.Start();
  NetworkChange.NetworkAddressChanged+=NetworkChanged;SystemEvents.PowerModeChanged+=PowerChanged;
  FormClosing+=delegate(object s,FormClosingEventArgs e){if(!quitting){e.Cancel=true;Hide();}};
  Shown+=delegate{Native.ChangeWindowMessageFilterEx(Handle,0x8001,1,IntPtr.Zero);if(initialHidden)Hide();else if(String.IsNullOrWhiteSpace(cfg.Gateways)){ShowPage(1);pageCaption.Text="首次设置 · 请确认你的校园网络";}Tick();};
  eventCallback=delegate(IntPtr h,uint evt,IntPtr hwnd,int obj,int child,uint thread,uint time){if(hwnd!=IntPtr.Zero&&Native.Class(hwnd)=="#32770"){uint pid;Native.GetWindowThreadProcessId(hwnd,out pid);if(Process.GetProcessesByName("DrClient").Any(p=>p.Id==pid))Dispatch(delegate{Tick();});}};
  eventHook=Native.SetWinEventHook(0x8002,0x8002,IntPtr.Zero,eventCallback,0,0,0);
  Log("启动 0.1.0-preview.1；不保存账号密码");
 }
 void AddButton(string text,int x,int y,Action action){var b=new Button{Text=text,Left=x,Top=y,Width=145,Height=36};b.Click+=delegate{try{action();}catch(Exception e){Log("操作失败 "+e.GetType().Name);MessageBox.Show(e.Message,"操作未完成");}};Controls.Add(b);}
 public void ShowMain(){Show();WindowState=FormWindowState.Normal;Activate();}
 void Dispatch(Action a){try{if(IsHandleCreated&&!IsDisposed)BeginInvoke(a);}catch{}}
 void NetworkChanged(object s,EventArgs e){Dispatch(delegate{next=DateTime.MinValue;grace=DateTime.UtcNow.AddSeconds(10);attempts=0;timer.Interval=3000;Log("网络变化，等待稳定");});}
 void PowerChanged(object s,PowerModeChangedEventArgs e){if(e.Mode==PowerModes.Resume)Dispatch(delegate{lastWake=DateTime.UtcNow;grace=DateTime.UtcNow.AddSeconds(15);next=DateTime.MinValue;attempts=0;timer.Interval=3000;Log("系统恢复，等待网络就绪");});}
 static bool KnownFile(string path,string expected){if(!File.Exists(path))return false;using(var f=File.OpenRead(path))using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(f)).Replace("-","").Equals(expected,StringComparison.OrdinalIgnoreCase);}
 bool Verify(ClientView view){string p=Path.Combine(Path.GetDirectoryName(cfg.ClientPath),"DrClient.exe");if(!File.Exists(p)||!File.Exists(cfg.ClientPath))return false;var stamp=File.GetLastWriteTimeUtc(p).AddTicks(File.GetLastWriteTimeUtc(cfg.ClientPath).Ticks%100000000);if(hashCache!=p||hashStamp!=stamp){verified=KnownFile(p,"3DB4A5DEB5555B51C3DBB97CEC17FA7C2B2ABC902D62B47FA731DB867624E9ED")&&KnownFile(cfg.ClientPath,"03AC3A5FFE33EEA45663B454ADD4527D37C0CC7A617F4D5ED0F3A45E5DEB3627");hashStamp=stamp;hashCache=p;}if(!verified)return false;
  if(view.Pid!=0){try{string running=Native.ProcessPath(view.Pid);if(!Path.GetFullPath(running).Equals(Path.GetFullPath(p),StringComparison.OrdinalIgnoreCase))return false;}catch{SetStatus("需要相同权限才能控制官方客户端",true);return false;}}return true;}
 void SetStatus(string s,bool attention){state.Text=s;tray.Text="校园网助手 · "+(s.Length>45?s.Substring(0,45):s);tray.Icon=attention?SystemIcons.Warning:Icon;if(lastStatus!=s){Log(s);lastStatus=s;}}
 void Tick(){if(busy)return;busy=true;try{var n=NetView.Read(cfg);var v=ClientView.Read();network.Text=n.Label;proxy.Text=n.Proxy;
   var now=DateTime.UtcNow;if(netKey!=n.Key){netKey=n.Key;stable=now;attempts=0;next=DateTime.MinValue;}
   if(v.LogoutConfirm!=IntPtr.Zero){if(now<ownLogoutUntil){Native.PostMessage(v.LogoutConfirm,0xF5,IntPtr.Zero,IntPtr.Zero);ownLogoutUntil=DateTime.MinValue;Log("确认用户要求的注销");}else if(!cfg.Paused){cfg.Paused=true;Save();paused.Checked=true;Log("检测到官方注销确认，暂停自动连接");}}
   if(cfg.Paused){SetStatus(v.Online?"已暂停自动连接 · 当前仍在线":"已暂停自动连接",false);timer.Interval=now<ownLogoutUntil?1000:15000;lastOnline=v.Online;return;}
   if(!n.Trusted){SetStatus("等待已登记的校园网络",false);timer.Interval=15000;lastOnline=v.Online;return;}
   if(v.Online){SetStatus("校园网已认证",false);attempts=0;next=DateTime.MinValue;timer.Interval=15000;lastOnline=true;return;}
   timer.Interval=3000;
   if(v.Dialog){SetStatus("官方客户端有提示，请查看处理",true);next=now.AddMinutes(5);return;}
   if(now<grace || (now-stable).TotalSeconds<8){SetStatus("等待网络恢复稳定…",false);return;}
   if(now<next){SetStatus(attempts>=4?"暂未连上，冷却后再试":"等待登录结果 / 重试间隔",false);return;}
   if(!Verify(v)){SetStatus("客户端权限或版本未通过验证",true);next=now.AddMinutes(5);return;}
   if(attempts>=4){attempts=0;next=now.AddMinutes(5);SetStatus("暂未连上，冷却后再试",true);return;}
   if(v.Main==IntPtr.Zero){if(Process.GetProcessesByName("DrMain").Length==0&&Process.GetProcessesByName("DrClient").Length==0){Process.Start(new ProcessStartInfo(cfg.ClientPath){WorkingDirectory=Path.GetDirectoryName(cfg.ClientPath),UseShellExecute=true});Log("启动官方客户端");attempts++;next=now.AddSeconds(30);}else{SetStatus("等待官方客户端就绪",true);next=now.AddSeconds(30);}return;}
   if(!v.Ready){SetStatus("客户端状态暂不明确，等待确认",true);next=now.AddSeconds(15);return;}
   bool sent=Native.PostMessage(Native.GetParent(v.Login),0x111,new IntPtr(Native.GetDlgCtrlID(v.Login)),v.Login);attempts++;lastOnline=false;next=now.AddSeconds(new int[]{15,30,60,120}[attempts-1]);Log("触发官方登录；消息结果="+sent+"；错误="+Marshal.GetLastWin32Error());SetStatus(sent?"已触发官方登录，等待结果":"登录消息未送达，请检查权限",!sent);
  }catch(Exception ex){Log("检查失败 "+ex.GetType().Name);SetStatus("检查遇到问题，可查看诊断",true);next=DateTime.UtcNow.AddSeconds(30);}finally{busy=false;}}
 void ManualConnect(){if(paused.Checked)paused.Checked=false;attempts=0;next=DateTime.MinValue;grace=DateTime.UtcNow;Tick();}
 void Logout(){paused.Checked=true;Save();var v=ClientView.Read();if(v.Online&&Verify(v)){ownLogoutUntil=DateTime.UtcNow.AddSeconds(10);Native.PostMessage(Native.GetParent(v.Logout),0x111,new IntPtr(Native.GetDlgCtrlID(v.Logout)),v.Logout);timer.Interval=1000;Log("用户要求注销，已持久暂停");}Tick();}
 void OpenOfficial(){var v=ClientView.Read();if(!Verify(v)){MessageBox.Show("官方客户端版本或路径未通过验证。");return;}if(v.Main!=IntPtr.Zero){Native.ShowWindow(v.Main,5);Native.SetForegroundWindow(v.Main);}else Process.Start(new ProcessStartInfo(cfg.ClientPath){UseShellExecute=true,WorkingDirectory=Path.GetDirectoryName(cfg.ClientPath)});}
 static int TaskCommand(string args){using(var p=Process.Start(new ProcessStartInfo("schtasks.exe",args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true})){p.StandardOutput.ReadToEnd();p.StandardError.ReadToEnd();if(!p.WaitForExit(5000))return -1;return p.ExitCode;}}
 bool StartupEnabled(){if(File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"installed.flag"))){using(var k=Registry.CurrentUser.OpenSubKey(@"Software\JLUConnect"))return k==null||Convert.ToInt32(k.GetValue("Startup",1))!=0;}using(var k=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))return k!=null&&k.GetValue("JLUConnect")!=null;}
 void Configure(){ShowMain();ShowPage(1);}
 void Diagnostics(){ShowMain();ShowPage(2);}
 void Quit(){quitting=true;timer.Stop();NetworkChange.NetworkAddressChanged-=NetworkChanged;SystemEvents.PowerModeChanged-=PowerChanged;if(eventHook!=IntPtr.Zero)Native.UnhookWinEvent(eventHook);tray.Visible=false;tray.Dispose();Log("退出助手，保留当前网络连接");Application.Exit();}
 protected override void WndProc(ref Message m){if(m.Msg==0x8001)ShowMain();base.WndProc(ref m);}
 [STAThread] public static void Main(string[] args){
  if(args.Contains("--preview")){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);using(var f=new App(false,true)){using(var iconFile=File.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Campus.ico")))f.Icon.Save(iconFile);f.Show();Application.DoEvents();using(var bitmap=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(bitmap,new Rectangle(0,0,f.Width,f.Height));bitmap.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"界面预览.png"));}}return;}
  bool created;using(var mutex=new Mutex(true,"Local\\JLUConnect",out created)){if(!created){Native.EnumWindows(delegate(IntPtr h,IntPtr l){if(Native.Text(h)=="JLUConnect · 校园网助手"){IntPtr r;Native.SendMessageTimeout(h,0x8001,IntPtr.Zero,IntPtr.Zero,2,1000,out r);}return true;},IntPtr.Zero);return;}Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new App(args.Contains("--background")));}
 }
}
