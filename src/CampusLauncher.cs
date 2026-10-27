using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
class Launcher {
 delegate bool Callback(IntPtr h,IntPtr l);
 [DllImport("user32.dll")] static extern bool EnumWindows(Callback c,IntPtr l);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr h,StringBuilder b,int n);
 [DllImport("user32.dll",SetLastError=true)] static extern bool PostMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
 [STAThread] static void Main(){bool found=false;EnumWindows(delegate(IntPtr h,IntPtr l){var b=new StringBuilder(128);GetWindowText(h,b,128);if(b.ToString()=="JLUConnect · 校园网助手"){found=PostMessage(h,0x8001,IntPtr.Zero,IntPtr.Zero);}return true;},IntPtr.Zero);if(found)return;using(var p=Process.Start(new ProcessStartInfo("schtasks.exe","/Run /TN JLUConnect.Open"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true})){p.StandardOutput.ReadToEnd();p.StandardError.ReadToEnd();p.WaitForExit();if(p.ExitCode!=0)MessageBox.Show("启动任务不可用，请重新运行安装脚本。","JLUConnect · 校园网助手");}}
}
