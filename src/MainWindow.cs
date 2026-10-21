using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.Storage.Pickers;
using WF=System.Windows.Forms;

namespace KeyFlow;
internal sealed class MainWindow : Window {
 readonly Grid root;
 readonly NavigationView nav;
 readonly FrameworkElement actions,prefs;
 readonly TextBlock title,subtitle,status;
 readonly TextBox[] targets;
 readonly NumberBox doubleMs,holdMs;
 readonly ToggleSwitch autoStart,hideTray,focus,winC,lenovoSmartKey;
 readonly SmartKeyBridge smartBridge=new();
 readonly Gestures smartGestures=new();
 long lastKeyboardDown=-1000;
 int smartPackets;
 readonly ComboBox theme;
 readonly Button pause;
 readonly Gestures gestures=new();
 readonly Stopwatch clock=Stopwatch.StartNew();
 readonly DispatcherTimer tick=new() { Interval=TimeSpan.FromMilliseconds(15) };
 Settings cfg;
 WF.NotifyIcon? tray;
 WF.ToolStripMenuItem? pauseItem;
 Native.Hook? hookCallback;
 IntPtr hook,hwnd;
 int activeKey;
 bool quitting,paused,initialized;
 readonly bool smoke=Program.Args.Contains("--smoke-test");
 public MainWindow() {
  string warning="";try { cfg=Settings.Load(); } catch(Exception ex) { cfg=new();warning="配置读取失败，已使用默认值；原文件仍保留。";Program.Log(ex); }
  Title="KeyFlow · WinUI 3";
  root=(Grid)XamlReader.Load(File.ReadAllText(Path.Combine(Program.Base,"MainView.xaml")));
  Content=root;
  nav=Find<NavigationView>("Navigation");actions=Find<FrameworkElement>("ActionsPage");prefs=Find<FrameworkElement>("PreferencesPage");
  title=Find<TextBlock>("PageTitle");subtitle=Find<TextBlock>("PageSubtitle");status=Find<TextBlock>("Status");
  targets=Enumerable.Range(0,3).Select(i=>Find<TextBox>("Target"+i)).ToArray();
  doubleMs=Find<NumberBox>("DoubleMs");holdMs=Find<NumberBox>("HoldMs");
  autoStart=Find<ToggleSwitch>("AutoStart");hideTray=Find<ToggleSwitch>("HideTray");focus=Find<ToggleSwitch>("FocusExisting");winC=Find<ToggleSwitch>("WinC");
  lenovoSmartKey=Find<ToggleSwitch>("LenovoSmartKey");lenovoSmartKey.IsOn=cfg.LenovoSmartKey;
  theme=Find<ComboBox>("Theme");pause=Find<Button>("Pause");
  for(int i=0;i<3;i++) {
   int index=i;targets[i].Text=cfg.Targets[i]??"";
   Find<Button>("Browse"+i).Click+=async(s,e)=>await Browse(index);
   Find<Button>("Test"+i).Click+=(s,e)=>Launch(targets[index].Text);
  }
  doubleMs.Value=cfg.DoubleMs;holdMs.Value=cfg.HoldMs;hideTray.IsOn=cfg.HideTray;focus.IsOn=cfg.FocusExisting;winC.IsOn=cfg.WinC;
  try { autoStart.IsOn=Startup.Read()!=null; } catch(Exception ex) { warning="无法读取自启动状态："+ex.Message;Program.Log(ex); }
  theme.SelectedIndex=cfg.Theme=="Light"?1:cfg.Theme=="Dark"?2:0;ApplyTheme();
  theme.SelectionChanged+=(s,e)=>ApplyTheme();
  Find<Button>("Save").Click+=(s,e)=>Save();Find<Button>("Hide").Click+=(s,e)=>AppWindow.Hide();
  Find<Button>("Exit").Click+=(s,e)=>Quit();pause.Click+=(s,e)=>SetPaused(!paused);
  nav.SelectionChanged+=(s,e)=>SelectPage((e.SelectedItem as NavigationViewItem)?.Tag?.ToString()=="preferences");nav.SelectedItem=nav.MenuItems[0];
  nav.PaneOpened+=(s,e)=>Find<FrameworkElement>("SideFooter").Visibility=Visibility.Visible;
  nav.PaneClosed+=(s,e)=>Find<FrameworkElement>("SideFooter").Visibility=Visibility.Collapsed;
  ExtendsContentIntoTitleBar=true;SetTitleBar(Find<Grid>("TitleBar"));
  if(Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())SystemBackdrop=new MicaBackdrop();
  AppWindow.Closing+=(s,e)=> { if(!quitting) { e.Cancel=true;AppWindow.Hide(); } };
  Closed+=(s,e)=>CleanUp();
  status.Text=warning!=""?warning:"准备就绪 · 更改后点击“保存并生效”";
 }
 T Find<T>(string name) where T:class => root.FindName(name) as T??throw new InvalidOperationException("找不到界面控件："+name);
 void ApplyTheme() { root.RequestedTheme=theme.SelectedIndex==1?ElementTheme.Light:theme.SelectedIndex==2?ElementTheme.Dark:ElementTheme.Default; }
 void SelectPage(bool preferences) { actions.Visibility=preferences?Visibility.Collapsed:Visibility.Visible;prefs.Visibility=preferences?Visibility.Visible:Visibility.Collapsed;title.Text=preferences?"偏好设置":"按键动作";subtitle.Text=preferences?"设置你的启动、后台运行和外观偏好。":"把常用的事，交给顺手的一按。"; }
 public void Initialize() {
  if(initialized)return;initialized=true;
  hwnd=WinRT.Interop.WindowNative.GetWindowHandle(this);
  var area=Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(AppWindow.Id,Microsoft.UI.Windowing.DisplayAreaFallback.Nearest).WorkArea;
  double scale=Native.GetDpiForWindow(hwnd)/96.0;
  int width=Math.Min((int)(1060*scale),area.Width-48),height=Math.Min((int)(820*scale),area.Height-48);
  AppWindow.Resize(new SizeInt32(width,height));AppWindow.Move(new PointInt32(area.X+(area.Width-width)/2,area.Y+(area.Height-height)/2));
  var menu=new WF.ContextMenuStrip();menu.Items.Add("打开设置",null,(s,e)=>DispatcherQueue.TryEnqueue(Restore));
  pauseItem=new WF.ToolStripMenuItem("暂停接管") { CheckOnClick=true };pauseItem.CheckedChanged+=(s,e)=>DispatcherQueue.TryEnqueue(()=>SetPaused(pauseItem.Checked));menu.Items.Add(pauseItem);
  menu.Items.Add("退出",null,(s,e)=>DispatcherQueue.TryEnqueue(Quit));
  tray=new WF.NotifyIcon { Icon=System.Drawing.SystemIcons.Application,Text="KeyFlow · WinUI 3",ContextMenuStrip=menu,Visible=!cfg.HideTray };
  tray.DoubleClick+=(s,e)=>DispatcherQueue.TryEnqueue(Restore);
  hookCallback=OnKey;hook=Native.SetWindowsHookEx(13,hookCallback,Native.GetModuleHandle(null),0);
  if(hook==IntPtr.Zero)status.Text="键盘接管失败："+Marshal.GetLastWin32Error();
  gestures.DoubleMs=cfg.DoubleMs;gestures.HoldMs=cfg.HoldMs;
  gestures.Fired+=n=> { status.Text="已识别："+new[]{"单击","双击","长按"}[n]+" · "+DateTime.Now.ToString("HH:mm:ss");Launch(cfg.Targets[n]); };
  smartGestures.Fired+=n=> { status.Text="联想 SmartKey："+(n==0?"单击":"双击");Launch(cfg.Targets[n]); };
  smartBridge.Click+=(n,source)=> {
   long now=clock.ElapsedMilliseconds;smartPackets++;Find<TextBlock>("SmartKeyStatus").Text="已收到兼容消息 · "+DateTime.Now.ToString("HH:mm:ss")+" · 累计 "+smartPackets+" 条（仍需确认实体按键效果）";
   if(paused||now-lastKeyboardDown<150)return;
   if(n==2) { smartGestures.Reset();status.Text="联想 SmartKey：双击";Launch(cfg.Targets[1]); }
   else { smartGestures.Down(now);smartGestures.Up(now); }
  };
  ConfigureSmartKey();
  tick.Tick+=(s,e)=> { gestures.Tick(clock.ElapsedMilliseconds);smartGestures.Tick(clock.ElapsedMilliseconds);if(Program.OpenRequest?.WaitOne(0)==true)Restore(); };tick.Start();
  if(Program.Args.Contains("--background"))AppWindow.Hide();
  if(smoke)_=SmokeTest();
 }
 void Restore() { AppWindow.Show();Activate();Native.SetForegroundWindow(hwnd); }
 async Task Browse(int index) {
  try { var picker=new FileOpenPicker();WinRT.Interop.InitializeWithWindow.Initialize(picker,hwnd);picker.FileTypeFilter.Add("*");var file=await picker.PickSingleFileAsync();if(file!=null)targets[index].Text=file.Path; }
  catch(Exception ex) { Error("无法选择文件："+ex.Message); }
 }
 void Save() {
  if(double.IsNaN(doubleMs.Value)||double.IsNaN(holdMs.Value)||doubleMs.Value<180||doubleMs.Value>700||holdMs.Value<400||holdMs.Value>2000) { Error("请输入有效的双击间隔（180–700）和长按阈值（400–2000）。");return; }
  var next=new Settings { Targets=targets.Select(t=>t.Text.Trim().Trim('"')).ToArray(),DoubleMs=(int)doubleMs.Value,HoldMs=(int)holdMs.Value,WinC=winC.IsOn,FocusExisting=focus.IsOn,HideTray=hideTray.IsOn,AutoStart=autoStart.IsOn,LenovoSmartKey=lenovoSmartKey.IsOn,Theme=theme.SelectedIndex==1?"Light":theme.SelectedIndex==2?"Dark":"System" };
  string? old=null;bool touched=false;
  try {
   old=Startup.Read();if(next.AutoStart||old!=null) { Startup.Set(next.AutoStart?Startup.Command:null);touched=true; }
   next.Save();cfg=next;gestures.Reset();gestures.DoubleMs=cfg.DoubleMs;gestures.HoldMs=cfg.HoldMs;if(tray!=null)tray.Visible=!cfg.HideTray;ConfigureSmartKey();status.Text="已保存 · 设置已生效，SmartKey 状态见偏好设置";
  } catch(Exception ex) { if(touched) { try { Startup.Set(old); } catch(Exception rollback) { Program.Log(rollback); } }Error("保存失败："+ex.Message); }
 }
 void SetPaused(bool value) { paused=value;gestures.Reset();smartGestures.Reset();ConfigureSmartKey();pause.Content=value?"恢复接管":"暂停接管";if(pauseItem!=null&&pauseItem.Checked!=value)pauseItem.Checked=value;Find<TextBlock>("Connection").Text=value?"已暂停接管":"正在接管按键";status.Text=value?"已暂停 · 按键恢复原有功能":"已恢复按键接管"; }
 void ConfigureSmartKey() {
  smartGestures.Reset();smartGestures.DoubleMs=cfg.DoubleMs;smartGestures.HoldMs=cfg.HoldMs;
  smartBridge.Dispose();if(cfg.LenovoSmartKey&&!paused)smartBridge.Start();
  Find<TextBlock>("SmartKeyStatus").Text=paused?"已暂停 · 兼容接收窗口已释放":smartBridge.Status;
 }
 void Error(string text) { status.Text=text;if(tray?.Visible==true)tray.ShowBalloonTip(4000,"KeyFlow",text,WF.ToolTipIcon.Warning); }
 void Launch(string? value) {
  if(string.IsNullOrWhiteSpace(value))return;
  string target=Environment.ExpandEnvironmentVariables(value.Trim().Trim('"'));
  try {
   bool web=Uri.TryCreate(target,UriKind.Absolute,out var uri)&&(uri.Scheme=="https"||uri.Scheme=="http");
   if(!web&&!File.Exists(target))throw new FileNotFoundException("找不到文件："+target);
   if(!web&&cfg.FocusExisting&&Path.GetExtension(target).Equals(".exe",StringComparison.OrdinalIgnoreCase)) {
    foreach(var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(target))) using(p) {
     bool same=false;try { same=string.Equals(p.MainModule?.FileName,target,StringComparison.OrdinalIgnoreCase); } catch { }
     if(same&&p.MainWindowHandle!=IntPtr.Zero) { if(Native.IsIconic(p.MainWindowHandle))Native.ShowWindow(p.MainWindowHandle,9);if(!Native.SetForegroundWindow(p.MainWindowHandle)) { var flash=new Native.FLASHWINFO { cbSize=(uint)Marshal.SizeOf<Native.FLASHWINFO>(),hwnd=p.MainWindowHandle,dwFlags=3,uCount=3 };Native.FlashWindowEx(ref flash); }return; }
    }
   }
   Process.Start(new ProcessStartInfo(target) { UseShellExecute=true,WorkingDirectory=web?"":Path.GetDirectoryName(target)??"" });
  } catch(Exception ex) { Error("无法打开："+ex.Message); }
 }
 IntPtr OnKey(int code,IntPtr message,IntPtr data) {
  if(code>=0) {
   var k=Marshal.PtrToStructure<Native.KBDLLHOOKSTRUCT>(data);int vk=(int)k.vkCode;long msg=message.ToInt64();bool down=msg==0x100||msg==0x104,up=msg==0x101||msg==0x105;
   if((k.flags&0x10)==0&&(down||up)) {
    bool trigger=activeKey!=0&&vk==activeKey;
    if(down&&!paused&&activeKey==0&&(vk==0x86||(cfg.WinC&&vk==0x43&&((Native.GetAsyncKeyState(0x5B)|Native.GetAsyncKeyState(0x5C))&0x8000)!=0))) { activeKey=vk;trigger=true;lastKeyboardDown=clock.ElapsedMilliseconds;Native.keybd_event(0xE8,0,0,UIntPtr.Zero);Native.keybd_event(0xE8,0,2,UIntPtr.Zero); }
    if(trigger) { long stamp=clock.ElapsedMilliseconds;if(up)activeKey=0;DispatcherQueue.TryEnqueue(()=> { if(paused)return;if(down)gestures.Down(stamp);else gestures.Up(stamp); });return new IntPtr(1); }
   }
  }
  return Native.CallNextHookEx(hook,code,message,data);
 }
 void CleanUp() { smartBridge.Dispose();tick.Stop();if(hook!=IntPtr.Zero) { Native.UnhookWindowsHookEx(hook);hook=IntPtr.Zero; }if(tray!=null) { tray.Visible=false;tray.Dispose();tray=null; } }
 void Quit() { quitting=true;CleanUp();Close(); }
 async Task Capture(string file) {
  var image=new RenderTargetBitmap();await image.RenderAsync(root);var buffer=await image.GetPixelsAsync();
  byte[] pixels=new byte[buffer.Length];using(var read=DataReader.FromBuffer(buffer))read.ReadBytes(pixels);
  using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);
  encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)image.PixelWidth,(uint)image.PixelHeight,96,96,pixels);await encoder.FlushAsync();
  using var reader=new DataReader(stream.GetInputStreamAt(0));await reader.LoadAsync((uint)stream.Size);byte[] result=new byte[stream.Size];reader.ReadBytes(result);File.WriteAllBytes(Path.Combine(Program.Base,file),result);
 }
 async Task SmokeTest() {
  try {
   await Task.Delay(700);SmartKeyBridge.Test();if(hook==IntPtr.Zero)throw new Exception("Keyboard hook failed");
   await Capture("preview-actions.png");nav.SelectedItem=nav.MenuItems[1];await Task.Delay(250);await Capture("preview-settings.png");
   theme.SelectedIndex=2;await Task.Delay(350);await Capture("preview-dark.png");
   bool original=tray!.Visible;tray.Visible=false;if(tray.Visible)throw new Exception("Tray hide failed");tray.Visible=true;if(!tray.Visible)throw new Exception("Tray show failed");tray.Visible=original;
   SetPaused(true);if(!paused)throw new Exception("Pause failed");SetPaused(false);
   AppWindow.Hide();using(var child=Process.Start(new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute=false,WindowStyle=ProcessWindowStyle.Hidden })) { if(child!=null)await child.WaitForExitAsync(); }
   await Task.Delay(350);if(!AppWindow.IsVisible)throw new Exception("Single-instance recovery failed");
   File.WriteAllText(Path.Combine(Program.Base,"smoke-test.txt"),"PASS: native SmartKey receiver protocol tests; WinUI 3 window and controls; keyboard hook; light/dark rendering; tray hide/show; pause/resume; second-process recovery.");
  } catch(Exception ex) { Program.Log(ex);File.WriteAllText(Path.Combine(Program.Base,"smoke-test.txt"),"FAIL: "+ex);Environment.ExitCode=1; }
  finally { Quit(); }
 }
}
