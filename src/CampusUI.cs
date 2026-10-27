using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;

public partial class App {
 readonly Color bg=Color.FromArgb(31,31,31), surface=Color.FromArgb(37,37,38), rail=Color.FromArgb(24,24,24), ink=Color.FromArgb(230,230,230), muted=Color.FromArgb(154,160,166), accent=Color.FromArgb(0,122,204), line=Color.FromArgb(55,55,59);
 Panel[] pages;Button[] navigation;TextBox wifiInput,gatewayInput,pathInput,diagnosticText;Label pageCaption;ToolTip tips;
 [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr h,int attr,ref int value,int size);
 [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr h);
 static Icon MakeIcon(){using(var b=new Bitmap(32,32))using(var g=Graphics.FromImage(b)){g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;g.Clear(Color.Transparent);using(var brush=new SolidBrush(Color.FromArgb(0,122,204)))g.FillEllipse(brush,1,1,30,30);using(var pen=new Pen(Color.White,2.5f)){g.DrawArc(pen,6,9,20,15,210,120);g.DrawArc(pen,10,14,12,9,210,120);}g.FillEllipse(Brushes.White,14,21,4,4);IntPtr h=b.GetHicon();try{return (Icon)Icon.FromHandle(h).Clone();}finally{DestroyIcon(h);}}}
 Label Caption(string text,int x,int y,int w,int h,float size,Color color){return new Label{Text=text,Location=new Point(x,y),Size=new Size(w,h),Font=new Font("Microsoft YaHei UI",size),ForeColor=color,BackColor=Color.Transparent};}
 Button ActionButton(string text,int x,int y,int width,Action action,bool primary){var b=new Button{Text=text,Location=new Point(x,y),Size=new Size(width,40),FlatStyle=FlatStyle.Flat,BackColor=primary?accent:surface,ForeColor=ink,Cursor=Cursors.Hand,Font=new Font("Microsoft YaHei UI",10),UseVisualStyleBackColor=false};b.FlatAppearance.BorderColor=primary?accent:line;b.FlatAppearance.MouseOverBackColor=primary?Color.FromArgb(20,140,220):Color.FromArgb(48,48,52);b.Click+=delegate{try{action();}catch(Exception e){MessageBox.Show(this,e.Message,"操作未完成");}};return b;}
 void BuildShell(){
  Text="JLUConnect · 校园网助手";Font=new Font("Microsoft YaHei UI",10);AutoScaleMode=AutoScaleMode.Dpi;AutoScaleDimensions=new SizeF(96,96);ClientSize=new Size(760,510);BackColor=bg;ForeColor=ink;StartPosition=FormStartPosition.CenterScreen;FormBorderStyle=FormBorderStyle.FixedSingle;MaximizeBox=false;Icon=MakeIcon();
  HandleCreated+=delegate{try{int dark=1;DwmSetWindowAttribute(Handle,20,ref dark,4);}catch{}};
  tips=new ToolTip{ShowAlways=true};
  var side=new Panel{Dock=DockStyle.Left,Width=60,BackColor=rail};Controls.Add(side);
  var footer=new Panel{Dock=DockStyle.Bottom,Height=28,BackColor=rail};Controls.Add(footer);footer.Controls.Add(Caption("JLU  /  校园网助手",78,5,300,20,8,muted));footer.Controls.Add(Caption("关闭窗口后，继续在托盘运行",480,5,270,20,8,muted));
  pageCaption=Caption("概览",92,25,560,30,11,muted);Controls.Add(pageCaption);
  pages=new Panel[3];for(int i=0;i<3;i++){pages[i]=new Panel{Location=new Point(92,72),Size=new Size(632,390),BackColor=bg,Visible=i==0};Controls.Add(pages[i]);}
  navigation=new Button[3];string[] names={"概览","网络设置","诊断"};string[] glyphs={"\uE80F","\uE713","\uE9D9"};
  for(int i=0;i<3;i++){int index=i;var b=new Button{Text=glyphs[i],AccessibleName=names[i],Location=new Point(0,16+i*61),Size=new Size(60,54),FlatStyle=FlatStyle.Flat,BackColor=rail,ForeColor=i==0?ink:muted,Font=new Font("Segoe MDL2 Assets",20),Cursor=Cursors.Hand,TabIndex=i};b.FlatAppearance.BorderSize=0;b.FlatAppearance.MouseOverBackColor=surface;b.Click+=delegate{ShowPage(index);};b.Paint+=delegate(object s,PaintEventArgs e){if(pages[index].Visible)using(var brush=new SolidBrush(accent))e.Graphics.FillRectangle(brush,0,8,3,38);};tips.SetToolTip(b,names[i]);navigation[i]=b;side.Controls.Add(b);}
  var home=pages[0];home.Controls.Add(Caption("●  连接状态",0,0,580,25,9,Color.FromArgb(91,190,139)));
  state=Caption("正在检查校园网…",0,36,620,64,23,ink);network=Caption("",0,108,600,28,11,muted);home.Controls.Add(state);home.Controls.Add(network);
  home.Controls.Add(ActionButton("隐藏到托盘   ↘",0,156,220,delegate{Hide();},true));home.Controls.Add(Caption("后台继续连接，需要时从任务栏托盘打开",0,207,600,25,9,muted));
  home.Controls.Add(new Panel{Location=new Point(0,250),Size=new Size(628,1),BackColor=line});
  paused=new CheckBox{Text="暂停自动连接",Location=new Point(0,272),Size=new Size(250,28),ForeColor=ink,Checked=cfg.Paused,FlatStyle=FlatStyle.Standard,Cursor=Cursors.Hand};
  startup=new CheckBox{Text="登录 Windows 后运行",Location=new Point(310,272),Size=new Size(315,28),ForeColor=ink,Checked=StartupEnabled(),Cursor=Cursors.Hand};home.Controls.Add(paused);home.Controls.Add(startup);
  home.Controls.Add(ActionButton("打开官方客户端",0,326,190,OpenOfficial,false));home.Controls.Add(ActionButton("退出助手",208,326,140,Quit,false));
  tips.SetToolTip(paused,"暂停后保留当前连接；取消勾选恢复自动连接。");tips.SetToolTip(startup,"管理助手的登录启动任务。");
  BuildSettings();BuildDiagnostics();
 }
 TextBox Field(Panel parent,string label,int y,string value){parent.Controls.Add(Caption(label,0,y,620,23,10,ink));var t=new TextBox{Location=new Point(0,y+29),Size=new Size(618,30),Text=value,BackColor=surface,ForeColor=ink,BorderStyle=BorderStyle.FixedSingle};parent.Controls.Add(t);return t;}
 void BuildSettings(){var p=pages[1];p.Controls.Add(Caption("网络与兼容",0,0,620,38,20,ink));p.Controls.Add(Caption("仅在你确认的校园网络中自动登录。多个值用逗号分隔。",0,48,620,28,9,muted));
  wifiInput=Field(p,"Wi-Fi 名称（有线连接不使用此项）",88,cfg.WifiNames);gatewayInput=Field(p,"校园网网关",158,cfg.Gateways);pathInput=Field(p,"官方客户端路径",228,cfg.ClientPath);pathInput.Width=510;
  p.Controls.Add(ActionButton("浏览…",524,253,94,delegate{using(var dialog=new OpenFileDialog{Filter="官方客户端|DrMain.exe",Title="选择你安装的官方 DrMain.exe"}){if(dialog.ShowDialog(this)==DialogResult.OK)pathInput.Text=dialog.FileName;}},false));
  p.Controls.Add(ActionButton("保存设置",0,323,150,delegate{if(!File.Exists(pathInput.Text.Trim())||!Path.GetFileName(pathInput.Text.Trim()).Equals("DrMain.exe",StringComparison.OrdinalIgnoreCase)){MessageBox.Show(this,"请选择已安装的 DrMain.exe。","路径无效");return;}cfg.WifiNames=wifiInput.Text.Trim();cfg.Gateways=gatewayInput.Text.Trim();cfg.ClientPath=pathInput.Text.Trim();Save();netKey=null;pageCaption.Text="网络设置 · 已保存";},true));
  p.Controls.Add(ActionButton("识别当前网络",174,323,180,DetectNetwork,false));
  p.Controls.Add(Caption("账号密码请在官方客户端中填写",370,334,250,25,8,muted));
 }
 void DetectNetwork(){var candidates=NetView.DetectCandidates();if(candidates.Count==0){MessageBox.Show(this,"没有识别到带网关的 Wi-Fi 或有线接口。请先连接校园网；若 VPN 隐藏了物理网络，可手动填写，助手不会关闭 VPN。","未找到校园网络");return;}
  using(var f=new Form{Text="确认校园网络",ClientSize=new Size(580,300),Font=Font,BackColor=bg,ForeColor=ink,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false}){
   var list=new ListBox{Location=new Point(20,75),Size=new Size(540,125),BackColor=surface,ForeColor=ink};foreach(var item in candidates)list.Items.Add(item);list.SelectedIndex=0;
   f.Controls.Add(Caption("请选择你确认属于校园网的接口。识别结果不是身份认证。",20,20,540,45,10,ink));f.Controls.Add(list);
   f.Controls.Add(ActionButton("填入设置",380,232,180,delegate{var item=(CampusNetworkCandidate)list.SelectedItem;if(item.Wireless)wifiInput.Text=item.Ssid;gatewayInput.Text=String.Join(",",item.Gateways);f.DialogResult=DialogResult.OK;pageCaption.Text="网络设置 · 已填入，请检查后保存";},true));f.ShowDialog(this);
  }
 }
 void BuildDiagnostics(){var p=pages[2];p.Controls.Add(Caption("连接诊断",0,0,620,38,20,ink));proxy=Caption("",0,48,620,30,9,muted);p.Controls.Add(proxy);
  diagnosticText=new TextBox{Location=new Point(0,90),Size=new Size(618,210),Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BackColor=surface,ForeColor=ink,BorderStyle=BorderStyle.FixedSingle,Font=new Font("Microsoft YaHei UI",9)};p.Controls.Add(diagnosticText);
  p.Controls.Add(ActionButton("刷新状态",0,323,140,RefreshDiagnostics,false));p.Controls.Add(ActionButton("连接 / 恢复自动",156,323,190,ManualConnect,false));p.Controls.Add(ActionButton("注销并暂停",362,323,155,Logout,false));
 }
 void RefreshDiagnostics(){var n=NetView.Read(cfg);var v=ClientView.Read();proxy.Text=n.Proxy;diagnosticText.Text="网络\r\n"+n.Detail+"\r\n\r\n官方客户端："+(v.Online?"已认证":v.Ready?"可登录":"状态待确认")+"\r\n自动连接："+(cfg.Paused?"已暂停":"已开启")+"\r\n\r\n日志位置\r\n"+Data+"\r\n\r\n助手不修改代理，也不记录账号密码。";}
 void ShowPage(int index){for(int i=0;i<pages.Length;i++){pages[i].Visible=i==index;navigation[i].ForeColor=i==index?ink:muted;navigation[i].Invalidate();}pageCaption.Text=new string[]{"概览","网络设置","诊断"}[index];if(index==2)RefreshDiagnostics();}
}
