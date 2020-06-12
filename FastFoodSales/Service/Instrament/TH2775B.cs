using Stylet;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace DAQ.Service
{
    public class TH2775B : PortService, IHandle<EventIO>
    {
        public float MainValue { get; set; }
        public float SubValue { get; set; }
        Timer timer;
        public override bool Connect()
        {
            if (port.IsOpen)
                port.Close();
            try
            {
                port.PortName = PortName;
                port.Open();
                port.Write("{K1}");
                IsConnected = port.IsOpen;
                timer = new Timer((s) => { 
                    _rcvbuffer = ""; }, null, 1000, -1);
                return IsConnected;
            }
            catch (Exception EX)
            {
                Events.Publish(new MsgItem() { Level = "E", Time = DateTime.Now, Value = PortName + ":" + EX.Message });
                port.Close();
                return false;
            }
        
        }
        string _rcvbuffer = "";
        public TH2775B(PlcService plc, IEventAggregator events) : base(plc, events)
        { 
            InstName = "TH2775B";
            port.BaudRate = 19200;
            port.DataReceived += (s, e) =>
            {
                while (port.BytesToRead > 0)
                {
                    var bf = port.ReadExisting();
                    Events.PublishMsg(InstName, bf);
                    _rcvbuffer += bf;
                }
            };
            for (int i = 0; i < 3; i++)
            {
                TestSpecs.Add(new TestSpecViewModel() { Name = $"L{i}" });
            }
        }
        Regex regex = new Regex(@"{[\s\d\.\-]+}");
        public override void Handle(EventIO message)
        {
            if (message.Value)
            {
                switch (message.Index)
                {
                    case (int)IO_DEF.电感数据获取开始:
                        Thread.Sleep(200);
                        Events.PublishMsg(InstName, _rcvbuffer);
                        if (_rcvbuffer.Length >= 30 * 3)
                        {
                            var matchs = regex.Matches(_rcvbuffer);
                            if (matchs.Count >= 3)
                            {
                                var cnt = matchs.Count;
                                for (int i = 0; i < 3; i++)
                                {
                                    var m = matchs[cnt-i-1].Value;
                                    float.TryParse(m.Substring(14, 6), out float main);
                                    float.TryParse(m.Substring(20, 6), out float sub);
                                    TestSpecs[i].Value = main;
                                    MainValue = main;
                                    SubValue = sub;
                                }
                            }
                            Plc.WriteLS(TestSpecs.Select(x => x.Value).ToArray());
                            Plc.Pulse((int)IO_DEF.电感数据获取完成);
                            _rcvbuffer = "";
                        }
                        else
                        {
                            Events.PublishError($"{InstName}", "电感数据格式不符合");
                        }
                        break;
                }
            }



        }
    }
}
