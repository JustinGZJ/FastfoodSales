using System;
using Stylet;


namespace DAQ.Service
{
    public class PortBService : PortService
    {
        public override string PortName => Properties.Settings.Default.PORT_B;
        public PortBService(PlcService plc, IEventAggregator @event) : base(plc, @event)
        {
            InstName = "TH";
            for (int i = 0; i < 4; i++)
            {
                TestSpecs[i].Name = "HI-POT " + i.ToString();
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
            if (Request("FETCh:MCRESult?", out string replay))
            {
                        
                var group = replay.Split(';');
                if(group.Length>=4)
                {
                    for(int i=0;i<4;i++)
                    {
                        var v = int.Parse(group[i].Split(',')[0]);
                        Plc.WriteBool(10 + i, v > 0);
                        TestSpecs[i].Result = (v > 0)?1f:-1f;
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


        public override void UpdateDatas()
        {
            for (int i = 0; i < 4; i++)
            {
                if (Plc.IsConnected)
                {
                    TestSpecs[i].Lower = Plc.GetGroupValue(i + 4, 0);
                    TestSpecs[i].Upper = Plc.GetGroupValue(i + 4, 1);
                    TestSpecs[i].Value = Plc.GetGroupValue(i + 4, 2);
                    TestSpecs[i].Result = Plc.GetGroupValue(i + 4, 3);
                }
            }
        }
    }
}
