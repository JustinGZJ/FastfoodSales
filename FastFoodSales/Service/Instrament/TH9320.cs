using System;
using Stylet;


namespace DAQ.Service
{
    //public class TH2882A : PortService
    //{
    //    BindableCollection<float> values = new BindableCollection<float>();
    //    public TH2882A(PlcService plc, IEventAggregator @event) : base(plc, @event)
    //    {
    //        InstName = "TH2882A";
    //        TestSpecs.Add(new TestSpecViewModel());
    //        Values.AddRange(new float[12]);
    //    }

    //    public BindableCollection<float> Values { get => values; set => values = value; }

    //    public override void Handle(EventIO message)
    //    {
    //        //switch (message.Index)
    //        //{
    //        //    case (int)IO_DEF.READ_HIP:
    //        //        Plc.WriteBool((int)IO_DEF.READ_HIP, false);
    //        //        Read();
    //        //        Plc.Pulse((int)IO_DEF.READ_HIP + 8);
    //        //        break;
    //        //}
    //    }
    //    public override void Read()
    //    {
    //        base.Read();
    //        if (Request("FETCh:MCRESult?", out string reply))
    //        {
    //            FileSaver.Process(new TLog() { Source = InstName, Log = reply });
    //            var group = reply.Split(';');
    //            if (group.Length >= 3)
    //            {
    //                for (int i = 0; i < 3; i++)
    //                {
    //                    string[] splited = group[i].Split(',');
    //                    if (splited.Length > 5)
    //                    {
    //                        var v = int.Parse(splited[0]);
    //                        //Plc.WriteBool(10 + i, v > 0);
    //                        TestSpecs[i].Result = (v > 0) ? 1 : -1;
    //                        float.TryParse(splited[1], out float n1);
    //                        float.TryParse(splited[2], out float n2);
    //                        float.TryParse(splited[3], out float n3);
    //                        float.TryParse(splited[4], out float n4);
    //                        Values[i * 4 + 0] = n1;
    //                        Values[i * 4 + 1] = n2;
    //                        Values[i * 4 + 2] = n3;
    //                        Values[i * 4 + 3] = n4;
    //                    }
    //                }
    //            }
    //            else
    //            {
    //                Events.Publish(new MsgItem() { Time = DateTime.Now, Level = "E", Value = "Error reply" });
    //            }
    //        }
    //    }
    //}


    public class TH9320 : PortService
    {
        public TH9320(PlcService plc, IEventAggregator @event) : base(plc, @event)
        {
            InstName = "TH9320";
        }
        float mainvalue;
        float subvalue;

        public float Mainvalue { get ; private set ; }
        public float Subvalue { get ; private set ; }

        public override void Handle(EventIO message)
        {
            if (message.Value && message.Index == (int)IO_DEF.绝缘数据获取开始)
            {
                Read();
                Plc.Pulse((int)IO_DEF.绝缘数据获取完成);
            }
            base.Handle(message);
        }

        public override void Read()
        {
            base.Read();
            if (Request("FETCh?", out string reply))
            {
                FileSaver.Process(new TLog() { Source = InstName, Log = reply });
                var group = reply.Split(';', ',');
                if (group.Length >= 4)
                {
                    float.TryParse(group[1], out mainvalue);
                    float.TryParse(group[2], out subvalue);
                    Mainvalue = mainvalue;
                    Subvalue = subvalue;
                    Plc.WriteIR(new float[2] { mainvalue, subvalue });
                }
                else
                {
                    Events.Publish(new MsgItem() { Time = DateTime.Now, Level = "E", Value = "Error reply" });
                }
            }

        }
    }
}
