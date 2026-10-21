using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.UI.Xaml;

namespace KeyFlow;
internal static class Program {
 internal static string[] Args=Array.Empty<string>();
 internal static EventWaitHandle? OpenRequest;
 internal static string Base=AppContext.BaseDirectory;
 internal static void Trace(string text) { if(Args.Contains("--smoke-test"))File.AppendAllText(Path.Combine(Base,"startup-test.log"),text+Environment.NewLine); }
 internal static void Log(Exception ex) { File.AppendAllText(Path.Combine(Base,"error.log"),DateTime.Now+" "+ex+Environment.NewLine); }
 [STAThread] static void Main(string[] args) {
  Args=args;
  try {
   if(args.Contains("--self-test")) { Tests.Run();return; }
   bool created;using var mutex=new Mutex(true,"Local\\KeyFlow.Copilot.v1",out created);
   if(!created) { if(!args.Contains("--background")) { try { using var ev=EventWaitHandle.OpenExisting("Local\\KeyFlow.Open.v3");ev.Set(); } catch { System.Windows.Forms.MessageBox.Show("请先退出旧版 KeyFlow，再打开 WinUI 版。","KeyFlow"); } }return; }
   using var request=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\KeyFlow.Open.v3");OpenRequest=request;
   Trace("Initialize COM wrappers");WinRT.ComWrappersSupport.InitializeComWrappers();
   Trace("Start WinUI application");Application.Start(p=> { Trace("Application factory");var context=new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());SynchronizationContext.SetSynchronizationContext(context);new App(); });
  } catch(Exception ex) { Log(ex);Environment.ExitCode=1;if(!args.Any(a=>a.StartsWith("--")))System.Windows.Forms.MessageBox.Show("启动失败："+ex.Message+"\n详情见程序旁的 error.log。","KeyFlow"); }
 }
}
public sealed partial class App : Application {
 MainWindow? window;
 public App() { Program.Trace("App constructor");UnhandledException+=(s,e)=>Program.Log(e.Exception);InitializeComponent();Program.Trace("App resources initialized"); }
 protected override void OnLaunched(LaunchActivatedEventArgs args) { Program.Trace("Create main window");window=new MainWindow();Program.Trace("Activate main window");window.Activate();Program.Trace("Initialize keyboard and tray");window.Initialize(); }
}
