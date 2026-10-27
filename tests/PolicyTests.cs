using System;
using System.IO;
using System.Xml.Serialization;
class PolicyTests {
 static int count;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;}
 static void Main(){
  var defaults=new Settings();
  Check(!NetView.IsTrustedNetwork(defaults,true,false,"JLU.PC",new[]{"198.51.100.1"}),"Fresh install must not authenticate before enrollment");
  var cfg=new Settings{WifiNames="JLU.PC, Lab-WiFi",Gateways="198.51.100.1",Paused=true};
  Check(NetView.IsTrustedNetwork(cfg,true,false,"JLU.PC",new[]{"198.51.100.1"}),"Enrolled Wi-Fi");
  Check(!NetView.IsTrustedNetwork(cfg,true,false,"Home",new[]{"198.51.100.1"}),"Same gateway at another Wi-Fi must not match");
  Check(!NetView.IsTrustedNetwork(cfg,true,false,"JLU.PC",new[]{"198.51.100.2"}),"Same SSID with unknown gateway must not match");
  Check(!NetView.IsTrustedNetwork(cfg,true,false,"JLU.PC",new string[0]),"No gateway");
  Check(NetView.IsTrustedNetwork(cfg,false,true,"",new[]{"198.51.100.1"}),"Enrolled Ethernet");
  Check(!NetView.IsTrustedNetwork(cfg,false,false,"",new[]{"198.51.100.1"}),"Other interface types");
  Check(!NetView.Listed(cfg.WifiNames,"JLU"),"No substring matching");
  Check(NetView.Listed(cfg.WifiNames,"Lab-WiFi"),"Trim enrollment list");
  var serializer=new XmlSerializer(typeof(Settings));using(var text=new StringWriter()){serializer.Serialize(text,cfg);using(var reader=new StringReader(text.ToString())){var restored=(Settings)serializer.Deserialize(reader);Check(restored.Paused,"Pause persists across restart");Check(restored.Gateways==cfg.Gateways,"Enrollment persists");}}
  Console.WriteLine("PASS: "+count+" offline checks; no network changes or authentication requests.");
 }
}
