using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace KeyFlow;
// Compatibility receiver for the specific Lenovo 2.0.13.10 user-mode protocol.
// It leaves the OEM service, HID handling and all unrelated Fn actions intact.
internal sealed class SmartKeyBridge : IDisposable {
 internal static readonly (string Name,uint Message)[] Protocols={
  ("_LENOVOSUPERKEY_WINDOW_CLASS_NAME_3_0_",0x13A2),
  ("_LENOVOSMARTENGINE_WINDOW_CLASS_NAME_4_0_",0x13A2),
  ("_LENOVOCUSTMOMIZEKEY_WINDOW_CLASS_NAME_3_0_",0x13A3)
 };
 readonly List<Endpoint> endpoints=new();
 internal event Action<int,string>? Click;
 internal string Status { get;private set; }="未启用";
 internal bool Active => endpoints.Count==Protocols.Length;
 internal static bool Decode(uint received,uint expected,IntPtr w,IntPtr l,out int code) { code=0;if(received!=expected||l!=IntPtr.Zero)return false;long n=w.ToInt64();if(n!=1&&n!=2)return false;code=(int)n;return true; }
 internal void Start(string prefix="") {
  Dispose();
  foreach(var protocol in Protocols) {
   if(Api.FindWindow(prefix+protocol.Name,null)!=IntPtr.Zero) { Status="原厂 SmartKey 窗口正在使用接口，请先关闭 SmartKey 界面后重新启用；保留 Fn 服务。";return; }
  }
  try {
   foreach(var p in Protocols) {
    var endpoint=new Endpoint(prefix+p.Name,p.Message,(n)=>Click?.Invoke(n,p.Name));endpoints.Add(endpoint);
    if(Api.FindWindow(prefix+p.Name,null)!=endpoint.Handle)throw new InvalidOperationException("SmartKey 窗口发生竞争，请关闭原厂 SmartKey 界面后重试。");
   }
   Status="已接入 3 条兼容接口，等待实体 SmartKey 验证。";
  } catch(Exception ex) { Dispose();Status="接入失败："+ex.Message; }
 }
 public void Dispose() { for(int i=endpoints.Count-1;i>=0;i--)endpoints[i].Dispose();endpoints.Clear();Status="未启用"; }
 internal static void Test() {
  string prefix="KeyFlow.Test."+Guid.NewGuid().ToString("N");int singles=0,doubles=0;
  using(var bridge=new SmartKeyBridge()) {
   bridge.Click+=(n,s)=> { if(n==1)singles++;else doubles++; };bridge.Start(prefix);if(!bridge.Active)throw new Exception(bridge.Status);
   foreach(var p in Protocols) {
    IntPtr h=Api.FindWindow(prefix+p.Name,null);if(h==IntPtr.Zero)throw new Exception("Compatibility window not discoverable");
    Api.SendMessage(h,p.Message,new IntPtr(1),IntPtr.Zero);Api.SendMessage(h,p.Message,new IntPtr(2),IntPtr.Zero);
    Api.SendMessage(h,p.Message,new IntPtr(3),IntPtr.Zero);Api.SendMessage(h,p.Message+1,new IntPtr(1),IntPtr.Zero);Api.SendMessage(h,p.Message,new IntPtr(1),new IntPtr(1));
   }
   if(singles!=3||doubles!=3)throw new Exception("Protocol message filtering failed");
   using var conflict=new SmartKeyBridge();conflict.Start(prefix);if(conflict.Active)throw new Exception("Receiver conflict was not detected");
  }
  foreach(var p in Protocols)if(Api.FindWindow(prefix+p.Name,null)!=IntPtr.Zero)throw new Exception("Compatibility window cleanup failed");
 }
 sealed class Endpoint : IDisposable {
  internal IntPtr Handle;
  readonly string name;readonly uint message;readonly Action<int> click;readonly Api.WindowProc callback;readonly IntPtr module;
  public Endpoint(string name,uint message,Action<int> click) {
   this.name=name;this.message=message;this.click=click;module=Native.GetModuleHandle(null);callback=WndProc;
   var cls=new Api.WNDCLASSEX { Size=(uint)Marshal.SizeOf<Api.WNDCLASSEX>(),Proc=Marshal.GetFunctionPointerForDelegate(callback),Instance=module,ClassName=name };
   if(Api.RegisterClassEx(ref cls)==0)throw new Win32Exception(Marshal.GetLastWin32Error());
   // FindWindow cannot find message-only windows. This is an invisible top-level tool window.
   Handle=Api.CreateWindowEx(0x08000080,name,"KeyFlow SmartKey receiver",0x80000000,0,0,0,0,IntPtr.Zero,IntPtr.Zero,module,IntPtr.Zero);
   if(Handle==IntPtr.Zero) { int error=Marshal.GetLastWin32Error();Api.UnregisterClass(name,module);throw new Win32Exception(error); }
  }
  IntPtr WndProc(IntPtr h,uint m,IntPtr w,IntPtr l) {
   if(Decode(m,message,w,l,out int n)) { try { click(n); } catch(Exception ex) { Program.Log(ex); }return IntPtr.Zero; }
   return Api.DefWindowProc(h,m,w,l);
  }
  public void Dispose() { if(Handle!=IntPtr.Zero) { Api.DestroyWindow(Handle);Handle=IntPtr.Zero;Api.UnregisterClass(name,module); }GC.KeepAlive(callback); }
 }
 static class Api {
  internal delegate IntPtr WindowProc(IntPtr h,uint message,IntPtr w,IntPtr l);
  [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]internal struct WNDCLASSEX { internal uint Size,Style;internal IntPtr Proc;internal int ClassExtra,WindowExtra;internal IntPtr Instance,Icon,Cursor,Background;internal string? MenuName,ClassName;internal IntPtr SmallIcon; }
  [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)]internal static extern ushort RegisterClassEx(ref WNDCLASSEX cls);
  [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)]internal static extern IntPtr CreateWindowEx(uint exStyle,string cls,string title,uint style,int x,int y,int width,int height,IntPtr parent,IntPtr menu,IntPtr instance,IntPtr param);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]internal static extern IntPtr DefWindowProc(IntPtr h,uint message,IntPtr w,IntPtr l);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]internal static extern IntPtr FindWindow(string cls,string? title);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]internal static extern IntPtr SendMessage(IntPtr h,uint message,IntPtr w,IntPtr l);
  [DllImport("user32.dll")]internal static extern bool DestroyWindow(IntPtr h);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]internal static extern bool UnregisterClass(string cls,IntPtr instance);
 }
}
