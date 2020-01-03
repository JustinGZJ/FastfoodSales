using System;
using Stylet;


namespace DAQ.Service
{
    public class Th2882Service : PortService
    {

        public override string PortName => Properties.Settings.Default.PORT_C;
        public Th2882Service(PlcService plc, IEventAggregator @event) : base(plc, @event)
        {
            InstName = "TH2882";
            TestSpecs[0].Name = "TH2882";
        }

        public override void Handle(EventIO message)
        {
            if (message.Value)
            {
                switch (message.Index)
                {
                    case (int)IO_DEF.READ_IMPULSE:
                        Plc.WriteBool((int)IO_DEF.READ_IMPULSE, false);
                        Read();
                        Plc.Pulse((int)IO_DEF.IMPULSE_FINISH);
                        break;
                }
            }
        }
        public override void Read()
        {
            base.Read();
            if (Request("FETCh:CRESult?", out string reply))
            {
                FileSaver.Process(new TLog() { Source = InstName, Log = reply });
                if (int.TryParse(reply.Split(',')[0], out var result))
                {
                    TestSpecs[0].Result = result > 0 ? 1 : -1;
                }
                else
                {
                    Events.Publish(new MsgItem() { Time = DateTime.Now, Level = "E", Value = "Error reply" });
                }
            }
        }
        public override bool Connect()
        {
            port.BaudRate = 38400;
            return base.Connect();
        }

    }
}
