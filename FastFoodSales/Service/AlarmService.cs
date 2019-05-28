using HslCommunication.Profinet.Omron;
using Stylet;
using StyletIoC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAQ.Service
{
    public class AlarmManager
    {
        const string ContentTitle = "TextContent";
        const string AddrTitle = "TrigAddr";
        int ContentIndex;
        int AddrIndex;
       public List<AlarmItem> alarms = new List<AlarmItem>();


        public AlarmManager(string FileName = "EventLib.csv")
        {
            if (!File.Exists(FileName))
                return;
            string[] lines = File.ReadAllLines(FileName);
            if (lines.Length > 3)
            {
                var ss = lines[1].Split('\t');
                for (int i = 0; i < ss.Length; i++)
                {
                    if (ss[i].Contains(ContentTitle))
                    {
                        ContentIndex = i;
                    }
                    if (ss[i].Trim('"')==(AddrTitle))
                    {
                        AddrIndex = i;
                    }
                    if (AddrIndex != 0 && ContentIndex != 0)
                        break;
                }
                for (int i = 2; i < lines.Length; i++)
                {
                    var ts = lines[i].Split('\t');
                    if(ts.Length>(ContentIndex+1)&&(ts.Length>AddrIndex+1))
                    if (!string.IsNullOrWhiteSpace(ts[ContentIndex].Trim('"')))
                    {
                            var s = ts[AddrIndex].Trim('"');
                            if (!s.Contains("."))
                                s += ".00";
                        alarms.Add(new AlarmItem
                        {
                            Address = "C" + s,
                            Content = ts[ContentIndex].Trim('"')
                        });
                    }
                }
            }
        }
    }
    public class AlarmItem:PropertyChangedBase
    {
        public string Address { get; set; }
        public string Content { get; set; }
        public bool Value { get; set; }
    }
    public class AlarmService:PropertyChangedBase
    {

        [Inject]
        public IEventAggregator Events { get; set; }
        OmronFinsNet omr;
        public bool IsConnected { get; set; }
        AlarmManager AM = new AlarmManager();
        BindableCollection<AlarmItem> alarms = new BindableCollection<AlarmItem>();
        public AlarmService()
        {
           foreach(var a in AM.alarms)
            {
                alarms.Add(a);
            }
        }
        public bool Connect()
        {
            if (omr != null)
            {
                omr.ConnectClose();
                omr = null;
            }
            omr = new OmronFinsNet(Properties.Settings.Default.PLC_IP, Properties.Settings.Default.PLC_PORT)
            {
                SA1 = 0,
                DA1 = 0
            };
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var op = omr.ConnectServer();
                    IsConnected = op.IsSuccess;
                    if (!op.IsSuccess)
                        Events.Publish(new MsgItem { Time = DateTime.Now, Level = "E", Value = op.Message });
                    if (IsConnected)
                    {
                        foreach(var v in AM.alarms)
                        {
                            
                             var b=omr.ReadBool(v.Address);
                            if(b.IsSuccess)
                            {
                                if (v.Value !=b.Content)
                                {
                                    v.Value = b.Content;
                                    Events.Publish(v);
                                }
                            }                            
                        }
                        omr.ConnectClose();
                    }
                    System.Threading.Thread.Sleep(10);
                }
            });
            return IsConnected;
        }


    }
}
