using System;
using Stylet;


namespace DAQ.Service
{
    public class PortAService : PortService, IHandle<EventIO>
    {

        public PortAService(PlcService plc, IEventAggregator events) : base(plc, events)
        {
            InstName = "RM3545";
            for (int i = 0; i < 4; i++)
            {
                TestSpecs[i].Name = "RESISTANCE " + i.ToString();
                TestSpecs[i].Result = 0;
            }
            //     Events.Subscribe(this);
        }
        public override string PortName => Properties.Settings.Default.PORT_A;


        public override void Handle(EventIO message)
        {
            if (message.Value)
            {
                switch (message.Index)
                {
                    case (int)IO_DEF.READ_RES:
                        Plc.WriteBool((int)IO_DEF.READ_RES, false);
                        Read();                       
                        Plc.Pulse((int)IO_DEF.READ_RES + 8);
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
                            TestSpecs[i].Value = v;
                            TestSpecs[i].Result = Plc.Bits[2 + i] ? 1 : -1;
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
