using System;
using System.IO;
using System.Text.Json;
namespace KeyFlow;
static class Tests {
 public static void Run() {
  int count=0;
  void Check(string name,Action<Gestures> run,string want) { string got="";var g=new Gestures();g.Fired+=n=>got+=n.ToString();run(g);if(got!=want)throw new Exception(name+": "+got+" != "+want);count++; }
  Check("single waits",g=>{g.Down(0);g.Up(50);g.Tick(349);},"");
  Check("single",g=>{g.Down(0);g.Up(50);g.Tick(350);},"0");
  Check("double",g=>{g.Down(0);g.Up(50);g.Down(170);g.Up(220);g.Tick(800);},"1");
  Check("hold",g=>{g.Down(0);g.Down(300);g.Up(650);g.Tick(1000);},"2");
  Check("separate singles",g=>{g.Down(0);g.Up(50);g.Down(400);g.Up(450);g.Tick(800);},"00");
  Check("tap then hold",g=>{g.Down(0);g.Up(50);g.Down(150);g.Tick(800);g.Up(900);},"2");
  Check("reset",g=>{g.Down(0);g.Up(50);g.Reset();g.Tick(900);},"");
  Check("autorepeat",g=>{g.Down(0);g.Down(50);g.Down(100);g.Up(150);g.Tick(500);},"0");
  Check("triple",g=>{g.Down(0);g.Up(30);g.Down(80);g.Up(110);g.Down(170);g.Up(200);g.Tick(500);},"10");
  var settings=JsonSerializer.Deserialize<Settings>("{\"Targets\":[\"https://chat.deepseek.com\",\"\",\"\"],\"DoubleMs\":300,\"HoldMs\":650,\"WinC\":false,\"FocusExisting\":true}")!;
  if(settings.Theme!="System"||settings.HideTray||settings.DoubleMs!=300)throw new Exception("Legacy migration failed");count++;
  var roundtrip=JsonSerializer.Deserialize<Settings>(JsonSerializer.Serialize(new Settings { HideTray=true,AutoStart=true,Theme="Dark" }))!;
  if(!roundtrip.HideTray||!roundtrip.AutoStart||roundtrip.Theme!="Dark")throw new Exception("New settings round trip failed");count++;
  string folder=Path.Combine(Program.Base,"test-startup");Directory.CreateDirectory(folder);Startup.FolderOverride=folder;
  try { Startup.Set(Startup.Command);if(Startup.Read()!=Startup.Command)throw new Exception("Startup round trip failed");Startup.Set(null);if(Startup.Read()!=null)throw new Exception("Startup removal failed");count++; }
  finally { Startup.FolderOverride=null;if(Directory.GetFiles(folder).Length==0)Directory.Delete(folder); }
  SmartKeyBridge.Test();count++;
  File.WriteAllText(Path.Combine(Program.Base,"self-test.txt"),"PASS: "+count+" tests (gestures, settings, isolated startup shortcut, three native SmartKey receivers, message filtering, conflicts and cleanup). Hardware validation pending.");
 }
}
