using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
namespace KeyFlow;
public class Gestures {
 public int DoubleMs=300, HoldMs=650;
 public bool DownNow;
 long started, deadline;
 bool pending, second;
 public event Action<int>? Fired;
 void Fire(int n) { if(Fired!=null) Fired(n); }
 public void Down(long now) {
  if(DownNow) return;
  if(pending && now>deadline) { pending=false; Fire(0); }
  second=pending; DownNow=true; started=now;
 }
 public void Up(long now) {
  if(!DownNow) return;
  DownNow=false;
  if(now-started>=HoldMs) { pending=false; second=false; Fire(2); }
  else if(second) { pending=false; second=false; Fire(1); }
  else { pending=true; deadline=now+DoubleMs; }
 }
 public void Tick(long now) { if(pending && !DownNow && now>=deadline) { pending=false; Fire(0); } }
 public void Reset() { DownNow=false; pending=false; second=false; }
}
internal static class Native {
 public delegate IntPtr Hook(int code,IntPtr wParam,IntPtr lParam);
 [StructLayout(LayoutKind.Sequential)] public struct KBDLLHOOKSTRUCT { public uint vkCode,scanCode,flags,time;public UIntPtr dwExtraInfo; }
 [StructLayout(LayoutKind.Sequential)] public struct FLASHWINFO { public uint cbSize;public IntPtr hwnd;public uint dwFlags,uCount,dwTimeout; }
 [DllImport("user32.dll",SetLastError=true)] public static extern IntPtr SetWindowsHookEx(int id,Hook callback,IntPtr module,uint thread);
 [DllImport("kernel32.dll",CharSet=CharSet.Auto)] public static extern IntPtr GetModuleHandle(string? name);
 [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr hwnd);
 [DllImport("user32.dll")] public static extern bool UnhookWindowsHookEx(IntPtr hook);
 [DllImport("user32.dll")] public static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr w,IntPtr l);
 [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);
 [DllImport("user32.dll")] public static extern void keybd_event(byte key,byte scan,uint flags,UIntPtr extra);
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
 [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr window);
 [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr window,int cmd);
 [DllImport("user32.dll")] public static extern bool FlashWindowEx(ref FLASHWINFO info);
}
static class Startup {
 public static string? FolderOverride;
 static string LinkPath { get { return Path.Combine(FolderOverride??Environment.GetFolderPath(Environment.SpecialFolder.Startup),"KeyFlow.lnk"); } }
 public static string Command { get { return "\""+Environment.ProcessPath!+"\" --background"; } }
 static object Get(object obj,string name,params object[] args) { return obj.GetType().InvokeMember(name,BindingFlags.InvokeMethod,null,obj,args)!; }
 static object Prop(object obj,string name) { return obj.GetType().InvokeMember(name,BindingFlags.GetProperty,null,obj,null)!; }
 static void Prop(object obj,string name,object value) { obj.GetType().InvokeMember(name,BindingFlags.SetProperty,null,obj,new object[]{value}); }
 public static string? Read() {
  if(!File.Exists(LinkPath))return null;
  object? shell=null,link=null;
  try { shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")??throw new InvalidOperationException("Windows Script Host 不可用"))!;link=Get(shell,"CreateShortcut",LinkPath);return "\""+(string)Prop(link,"TargetPath")+"\" "+(string)Prop(link,"Arguments"); }
  finally { if(link!=null)Marshal.FinalReleaseComObject(link);if(shell!=null)Marshal.FinalReleaseComObject(shell); }
 }
 public static void Set(string? command) {
  if(command==null) { if(File.Exists(LinkPath))File.Delete(LinkPath);return; }
  int end=command.IndexOf('"',1);if(!command.StartsWith("\"")||end<1)throw new ArgumentException("无效的启动路径");
  string target=command.Substring(1,end-1),args=command.Substring(end+1).Trim(),temp=LinkPath+".new.lnk";
  object? shell=null,link=null;
  try {
   shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")??throw new InvalidOperationException("Windows Script Host 不可用"))!;link=Get(shell,"CreateShortcut",temp);
   Prop(link,"TargetPath",target);Prop(link,"Arguments",args);Prop(link,"WorkingDirectory",Path.GetDirectoryName(target)??"");Prop(link,"Description","KeyFlow · Copilot 键助手");Get(link,"Save");
   if(File.Exists(LinkPath))File.Replace(temp,LinkPath,null);else File.Move(temp,LinkPath);
  } finally { if(link!=null)Marshal.FinalReleaseComObject(link);if(shell!=null)Marshal.FinalReleaseComObject(shell);if(File.Exists(temp))File.Delete(temp); }
 }
}
