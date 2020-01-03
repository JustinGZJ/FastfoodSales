using System;
using Stylet;

namespace DAQ.Service
{
    public class Th9201Service : PortService
    {

        public override string PortName => Properties.Settings.Default.PORT_B;
        public Th9201Service(PlcService plc, IEventAggregator @event) : base(plc, @event)
        {
            InstName = "TH9201";
            TestSpecs[0].Name = "TH9201";
        }

        public override void Handle(EventIO message)
        {
            if (message.Value)
            {
                switch (message.Index)
                {
                    case (int)IO_DEF.READ_HIP:
                        Plc.WriteBool((int)IO_DEF.READ_HIP, false);
                        Read();
                        Plc.Pulse((int)IO_DEF.HIP_FINISH);
                        break;
                }
            }
        }
        public override void Read()
        {
            base.Read();
            if (Request(":TEST:FETCh?", out string reply))
            {
                FileSaver.Process(new TLog() { Source = InstName, Log = reply });
                if (int.TryParse(reply.Split(',')[0], out var result))
                {
                    TestSpecs[0].Result = result == 1 ? 1 : -1;
                }
                else
                {
                    Events.Publish(new MsgItem() { Time = DateTime.Now, Level = "E", Value = "Error reply" });
                }
            }
        }
        public override bool Connect()
        {
            port.BaudRate = 19200;
            return base.Connect();
        }

    }
}