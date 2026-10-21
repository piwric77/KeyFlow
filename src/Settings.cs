using System;
using System.IO;
using System.Text.Json;

namespace KeyFlow;
public sealed class Settings {
 public string[] Targets { get;set; }={"https://chat.deepseek.com","",""};
 public int DoubleMs { get;set; }=300;
 public int HoldMs { get;set; }=650;
 public bool WinC { get;set; }
 public bool FocusExisting { get;set; }=true;
 public bool HideTray { get;set; }
 public bool AutoStart { get;set; }
 public bool LenovoSmartKey { get;set; }
 public string Theme { get;set; }="System";
 public static readonly string PathName=Path.Combine(AppContext.BaseDirectory,"settings.json");
 public static Settings Load() {
  if(!File.Exists(PathName))return new();
  var value=JsonSerializer.Deserialize<Settings>(File.ReadAllText(PathName))??throw new InvalidDataException("配置为空");
  if(value.Targets==null||value.Targets.Length!=3||value.DoubleMs<180||value.DoubleMs>700||value.HoldMs<400||value.HoldMs>2000)throw new InvalidDataException("配置格式无效");
  return value;
 }
 public void Save() {
  string temp=PathName+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(this,new JsonSerializerOptions { WriteIndented=true }));
  if(File.Exists(PathName))File.Replace(temp,PathName,PathName+".bak");else File.Move(temp,PathName);
 }
}
