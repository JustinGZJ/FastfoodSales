using System;
using Stylet;
using System.IO;
using System.Linq;

namespace DAQ.Service
{


    public class RM3545 : PortService, IHandle<EventIO>
    {

        public RM3545(PlcService plc, IEventAggregator events) : base(plc, events)
        {
            InstName = "RM3545";
            for (int i = 0; i < 3; i++)
            {
                TestSpecs.Add(new TestSpecViewModel() { Name = $"RESISTANCE {i}", Result = 0 });
            }
        }
  


        public override void Handle(EventIO message)
        {
            if (message.Value)
            {
                switch (message.Index)
                {
                    case (int)IO_DEF.电阻数据获取开始:
                        Read();
                        Plc.WriteR(TestSpecs.Select(x => x.Value).ToArray());
                        Plc.Pulse((int)IO_DEF.电阻数据获取完成);
                        break;
                }
            }
        }

        public override void Read()
        {
            base.Read();
            if (Request("SCAN:DATA?", out string reply))
            {
                FileSaver.Process(new TLog() { Source = InstName, Log = reply });
                var values = reply.Split(',');
                if (values.Length > 1)
                {
                    for (int i = 0; i < values.Length; i++)
                    {
                        var a = values[i];
                        if (float.TryParse(a, out float v))
                        {
                            TestSpecs[i].Value = v*1000;
                        }
                        else
                        {
                            Events.Publish(new MsgItem
                            {
                                Level = "E",
                                Time = DateTime.Now,
                                Value = "Resistance value parse fail"
                            });
                        }
                    }
                }
            }

        }

    }
}
