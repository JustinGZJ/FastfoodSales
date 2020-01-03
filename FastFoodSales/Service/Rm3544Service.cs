using System;
using Stylet;


namespace DAQ.Service
{

     public class Rm3544Service : PortService
    {

        public Rm3544Service(PlcService plc, IEventAggregator events) : base(plc, events)
        {
            InstName = "RM3544";
            TestSpecs[0].Name = "RM3544";
            TestSpecs[0].Result = 0;
            Events.Subscribe(this);
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
                        Plc.Pulse((int)IO_DEF.RESISTANCE_FINISH);
                        break;
                }
            }
        }

        public override void Read()
        {
            base.Read();
            if (Request("FETCH?", out string reply))
            {
                FileSaver.Process(new TLog() { Source = InstName, Log = reply });
                if (float.TryParse(reply, out float v))
                {
                    TestSpecs[0].Value = v;
                }
                TestSpecs[0].Result = Plc.Bits[(int)IO_DEF.RESISTANCE_RESULT] ? 1 : -1;
            }
        }
    }
}
