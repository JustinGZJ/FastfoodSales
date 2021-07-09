using System;
using LiveCharts;
using Stylet;


namespace DAQ.Service
{

    public class TH2882A : PortService
    {
        string buffer="";
        float[] vs = new float[5];
        public LiveCharts.ChartValues<float> Values { get; set; }= new LiveCharts.ChartValues<float>(new float[15]);


        public TH2882A(PlcService plc, IEventAggregator @event) : base(plc, @event)
        {
        
            InstName = "TH2882";
            for (int i = 0; i < 4; i++)
            {
                TestSpecs.Add(new TestSpecViewModel { Name = $"HI-POT {i}" });
            }
        }

        public override void Handle(EventIO message)
        {
            switch (message.Index)
            {
                case (int)IO_DEF.AIP测试完成:  //zie shuj 

                    Plc.Pulse((int)IO_DEF.匝间数据1获取完成);
                    break;
            }
        }
        public override void Read()
        {
            base.Read();
            if (Request("FETCh:CRESult?", out string reply))
            {
                FileSaver.Process(new TLog() { Source = InstName, Log = reply });

                ProcessData(reply);
                buffer = reply;
            }
            else
            {
                Events.Publish(new MsgItem() { Time = DateTime.Now, Level = "E", Value = "Error reply" });
            }
        }

        public void ProcessData(string reply)
        {
            var s = reply.Split(',');
            if (s.Length > 4)
            {
                if (!float.TryParse(s[1], out vs[0]))
                {
                    vs[0] = float.MaxValue;
                }
                else
                {
                    vs[0] *= 1;
                }
                if (!float.TryParse(s[2], out vs[1]))
                {
                    vs[1] = float.MaxValue;
                }

                if (!float.TryParse(s[3], out vs[2]))
                {
                    vs[2] = float.MaxValue;
                }
       
                if (!float.TryParse(s[4], out vs[3]))
                {
                    vs[3] = float.MaxValue;
                }
                if (!float.TryParse(s[0], out vs[4]))
                {
                    vs[4] = float.MaxValue;
                }
            }
        }
        public override bool Connect()
        {
            return base.Connect();
        }

    }
}
