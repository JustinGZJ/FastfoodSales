using System;
using Stylet;


namespace DAQ.Service
{
    public class PortBService : PortService
    {

        public override string PortName => Properties.Settings.Default.PORT_B;
        public PortBService(PlcService plc, IEventAggregator @event) : base(plc, @event)
        {
            InstName = "TH2883S4";
            for (int i = 0; i < 4; i++)
            {
                TestSpecs[i].Name = "HI-POT " + i.ToString();
              //  TestSpecs[i].Result = 1;
            }
           
        }

        public override void Handle(EventIO message)
        {
            switch (message.Index)
            {
                case (int)IO_DEF.READ_HIP:
                    Plc.WriteBool((int)IO_DEF.READ_HIP, false);
                    Read();
                    Plc.Pulse((int)IO_DEF.READ_HIP + 8);
                    break;
            }
        }
        public override void Read()
        {
            base.Read();
            if (Request("FETCh:MCRESult?", out string reply))
            {
                FileSaver.Process(new TLog() { Source = InstName, Log = reply });
                var group = reply.Split(';');
                if(group.Length>=4)
                {
                    for(int i=0;i<4;i++)
                    {
                        var v = int.Parse(group[i].Split(',')[0]);
                        Plc.WriteBool(10 + i, v > 0);
                        TestSpecs[i].Result = (v > 0)?1:-1;
                    }
                }
                else
                {
                    Events.Publish(new MsgItem() { Time = DateTime.Now, Level = "E", Value = "Error reply"});
                }
            }
        }
        public override bool Connect()
        {
            return base.Connect();
        }

    }
}
