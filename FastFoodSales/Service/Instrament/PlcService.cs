using HslCommunication;
using HslCommunication.Core;
using HslCommunication.Profinet.Siemens;
using Stylet;
using StyletIoC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DAQ.Service
{
    public class EventIO
    {
        public int Index { get; set; }
        public bool Value { get; set; }
    }

    public class ChaPin
    {
        public ChaPin()
        {
        }

        private bool _triger;

        public bool Done { get; set; }

        public void Enter(bool triger, float value)
        {
            if (triger && !_triger)
            {
                _triger = triger;
            }
        }
    }


    public class PlcService : PropertyChangedBase
    {
        private SiemensS7Net _rw;
        private SiemensS7Net _rw2;
        private SiemensS7Net _rw3;
        private string addr = "DB3000.0";
        public bool IsConnected { get; set; }

        private readonly bool[] Bits = new bool[32];
        private readonly string[] BitTags = Enum.GetNames(typeof(IO_DEF));
        public BindableCollection<KV<bool>> KVBits { get; set; }

        public BindableCollection<KV<float>> KVFloats { get; set; } = new BindableCollection<KV<float>>(Enumerable.Range(0, 32).Select(x => new KV<float>() { Index = x, Time = DateTime.Now }));
        private MsgFileSaver<PLC_FINAL_DATA> saver;

        public IEventAggregator Events { get; set; }
        public BindableCollection<PLC_FINAL_DATA> PLC_FINAL_DATAS { get; set; } = new BindableCollection<PLC_FINAL_DATA>();
        public LiveCharts.ChartValues<int> PassValues { get; set; } = new LiveCharts.ChartValues<int>(new int[8]);
        public LiveCharts.ChartValues<int> FailValues { get; set; } = new LiveCharts.ChartValues<int>(new int[8]);

        public PlcService([Inject] IEventAggregator eventAggregator, [Inject] MsgFileSaver<PLC_FINAL_DATA> saver)
        {
            this.saver = saver;
            this.Events = eventAggregator;
            KVBits = new BindableCollection<KV<bool>>(Enumerable.Range(0, 32).Select(x => new KV<bool>() { Index = x, Key = BitTags[x], Time = DateTime.Now }));
            KVFloats[0].Key = "电阻1";
            KVFloats[1].Key = "电阻2";
            KVFloats[2].Key = "最大压力";
        }

        public void AddPLCData(PLC_FINAL_DATA data)
        {
            if (PLC_FINAL_DATAS.Count > 500)
            {
                PLC_FINAL_DATAS.RemoveAt(0);
            }
            PLC_FINAL_DATAS.Add(data);
        }

        public bool Connect()
        {
            if (_rw != null)
            {
                _rw.ConnectClose();
                _rw = null;
            }
          //  _rw = new SiemensS7Net(SiemensPLCS.S1200, "127.0.0.1");
            _rw = new SiemensS7Net(SiemensPLCS.S1200, "192.168.0.139");
            _rw2 = new SiemensS7Net(SiemensPLCS.S1200, "192.168.0.1");
            _rw3 = new SiemensS7Net(SiemensPLCS.S1200, "192.168.0.81");
            Task.Factory.StartNew(() =>
            {
                _rw.ConnectServer();
                while (true)
                {
                    var rop = _rw.Read(addr, 70);
                    IsConnected = rop.IsSuccess;
                    if (!rop.IsSuccess)
                        Events.Publish(new MsgItem { Time = DateTime.Now, Level = "E", Value = rop.Message });
                    if (IsConnected)
                    {
                        for (int i = 0; i < 32; i++)
                        {
                            bool v = (rop.Content[i / 8] & (1 << (i % 8))) > 0;
                            if (Bits[i] != v)
                            {
                                Bits[i] = v;
                                KVBits[i].Value = v;
                                if (v)
                                {
                                    Events.Publish(new EventIO
                                    {
                                        Index = i,
                                        Value = v
                                    });
                                }
                                if ((i == (int)IO_DEF.通讯建立开始) && v)
                                {
                                    Events.PublishMsg("PLC", "通信建立连接");
                                    Pulse(((int)IO_DEF.通讯建立完成));
                                }
                                if (i == (int)IO_DEF.OK数据上传开始 && v)
                                {
                                    var DATA = ReadTestData("DB3010.0");
                                    Events.PublishMsg("PLC", "OK数据上传");
                                    AddPLCData(DATA);
                                    if (saver.CanProcess())
                                    {
                                        saver.Process(DATA);
                                        WriteBool(0, false);
                                    }
                                    else
                                    {
                                        WriteBool(0, true);
                                    }
                                    Pulse((int)IO_DEF.OK数据上传完成);
                                }
                                if (i == (int)IO_DEF.NG数据上传开始 && v)
                                {
                                    var DATA = ReadTestData("DB3011.0");
                                    Events.PublishMsg("PLC", "NG数据上传");
                                    AddPLCData(DATA);
                                    if (saver.CanProcess())
                                    {
                                        saver.Process(DATA);
                                        WriteBool(0, false);
                                    }
                                    else
                                    {
                                        WriteBool(0, true);
                                    }
                                    Pulse((int)IO_DEF.NG数据上传完成);
                                }

                            }
                        }
                        for (int i = 0; i < 8; i++)
                        {
                            PassValues[i] = _rw.ByteTransform.TransInt32(rop.Content, 4 + i * 8);
                            FailValues[i] = _rw.ByteTransform.TransInt32(rop.Content, 8 + i * 8);
                        }
                        if (_rw.ByteTransform.TransInt16(rop.Content, 68) > 5)
                        {
                            _rw.Write("DB3000.68", (short)0);
                        }
                    }
                    Thread.Sleep(10);
                }
            }, TaskCreationOptions.LongRunning);

            Task.Factory.StartNew(() =>
            {
                _rw2.ConnectServer();
                while (true)
                {
                    KVFloats[0].Value = _rw2.ReadFloat("M90").Content;
                    KVFloats[1].Value = _rw2.ReadFloat("M94").Content;
                    var trigger = _rw2.ReadBool("M99.0").Content;
                    if (trigger)
                    {
                        var dictitionary = new Dictionary<string, string>();
                        dictitionary["时间"] = DateTime.Now.ToString();
                        dictitionary["电阻1"] = KVFloats[0].Value.ToString("f2");
                        dictitionary["电阻2"] = KVFloats[1].Value.ToString("f2");
                        Utils.SaveFile(Path.Combine("../DaqData", DateTime.Now.ToString("yyyyMMdd"), "熔接电阻.csv"), dictitionary);
                        _rw2.Write("M99.1", true);
                    }
                    Thread.Sleep(100);
                }
            }, TaskCreationOptions.LongRunning);

            Task.Factory.StartNew(() =>
            {
                _rw3.ConnectServer();
                while (true)
                {
                    var rd = _rw3.ReadInt32("M90");
                    if (!rd.IsSuccess)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    KVFloats[2].Value = rd.Content;
                    var trigger = _rw3.ReadBool("M99.0").Content;
                    if (trigger)
                    {
                        var dictitionary = new Dictionary<string, string>();
                        dictitionary["时间"] = DateTime.Now.ToString();
                        dictitionary["最大压力"] = (KVFloats[2].Value / 100).ToString("f2");
                        Utils.SaveFile(Path.Combine("../DaqData", DateTime.Now.ToString("yyyyMMdd"), "插PIN压力.csv"), dictitionary);
                        _rw3.Write("M99.1", true);
                    }
                    Thread.Sleep(100);
                }
            }, TaskCreationOptions.LongRunning);
            return IsConnected;
        }

        public bool WriteBool(int index, bool value)
        {
            return _rw.Write($"DB3000.{index / 8}.{index % 8}", value).IsSuccess;
        }

        public void Pulse(int bitIndex, int Delayms = 500)
        {
            Task.Factory.StartNew(() =>
            {
                WriteBool(bitIndex, true);
                Thread.Sleep(Delayms);
                WriteBool(bitIndex, false);
            });
        }

        public PLC_FINAL_DATA ReadTestData(string Address)
        {
            var r = _rw.ReadCustomer<PLC_FINAL_DATA>(Address);
            if (r.IsSuccess)
            {
                return r.Content;
            }
            else
                return null;
        }

        public bool WriteTestData(string address, PLC_FINAL_DATA data)
        {
            return _rw.WriteCustomer(address, data).IsSuccess;
        }


    }

    public class PLC_FINAL_DATA : ISource, IDataTransfer
    {
        //读码数据
        public string 读码数据 { get; set; }

        //读码数据结果
        public short 读码数据结果 { get; set; }

        //OK数据结果  1
        public short OK数据结果 { get; set; }

        //NG数据结果 1
        public short NG数据结果 { get; set; }

        //绝缘数据结果
        public short 绝缘数据结果 { get; set; }

        //匝间数据结果
        public short 匝间数据结果 { get; set; }

        //电感数据结果
        public short 电感数据结果 { get; set; }

        //电阻数据结果
        public short 电阻数据结果 { get; set; }

        //相机数据结果
        public short 相机数据结果 { get; set; }

        //通规数据结果
        public short 通规数据结果 { get; set; }

        //绝缘数据1
        public float 耐压mA { get; set; }

        //绝缘数据2
        public float 绝缘电阻Mohm { get; set; }

        ////绝缘数据3
        //public float 线圈3绝缘数据 { get; set; }

        //匝间测试数据1-1
        public string 线圈1匝间 { get; set; }

        public float 线圈1匝间结果 { get; set; }

        //匝间测试数据2-1
        public string 线圈2匝间 { get; set; }


        public float 线圈2匝间结果 { get; set; }

        //匝间测试数据3-1
        public string 线圈3匝间 { get; set; }

        //匝间预留3
        public float 线圈3匝间结果 { get; set; }

        //电感测试数据1
        public float 线圈1电感uH { get; set; }

        public Int16 线圈1电感结果 { get; set; }
        public float 电感1平衡度 { get; set; }

        //电感测试数据2
        public float 线圈2电感uH { get; set; }
        public Int16 线圈2电感结果 { get; set; }
        public float 电感2平衡度 { get; set; }

        //电感测试数据3
        public float 线圈3电感uH { get; set; }
        public Int16 线圈3电感结果 { get; set; }
        public float 电感3平衡度 { get; set; }

        //电阻测试数据1
        public float 线圈1电阻mohm { get; set; }
        public Int16 线圈1电阻结果 { get; set; }


        //电阻测试数据2
        public float 线圈2电阻mohm { get; set; }
        public Int16 线圈2电阻结果 { get; set; }


        //电阻测试数据3
        public float 线圈3电阻mohm { get; set; }
        public Int16 线圈3电阻结果 { get; set; }
        public float 电阻平衡度 { get; set; }

        //图像1数据1
        public float 线圈1位置度_F { get; set; }

        //图像1数据2
        public float 线圈1位置度_GP { get; set; }

        //图像1数据3
        public float 线圈1位置度_S { get; set; }

        //图像1数据4
        public float 线圈2位置度_F { get; set; }

        //图像1数据5
        public float 线圈2位置度_GP { get; set; }

        //图像1数据6
        public float 线圈2位置度_S { get; set; }

        //图像1数据7
        public float 线圈3位置度_F { get; set; }

        //图像1数据8
        public float 线圈3位置度_GP { get; set; }

        //图像1数据9
        public float 线圈3位置度_S { get; set; }

        //图像2数据1
        public float 线圈1高度_F { get; set; }

        //图像2数据2
        public float 线圈1高度_GP { get; set; }

        //图像2数据3
        public float 线圈1高度_S { get; set; }

        //图像2数据4
        public float 线圈2高度_F { get; set; }

        //图像2数据5
        public float 线圈2高度_GP { get; set; }

        //图像2数据6
        public float 线圈2高度_S { get; set; }

        //图像2数据7
        public float 线圈3高度_F { get; set; }

        //图像2数据8
        public float 线圈3高度_GP { get; set; }

        //图像2数据9
        public float 图像2数据9 { get; set; }

        //通规数据
        public float 线圈3高度_S { get; set; }

        //止规数据
        public float 通规数据 { get; set; }

        public string Source { get; set; } = "生产数据";

        public ushort ReadCount { get; } = 310;

        private ReverseBytesTransform transform = new ReverseBytesTransform();

        public void ParseSource(byte[] Content)
        {
            读码数据结果 = transform.TransInt16(Content, 2);
            //OK数据结果  1
            OK数据结果 = transform.TransInt16(Content, 4);
            //NG数据结果 1
            NG数据结果 = transform.TransInt16(Content, 6);
            //绝缘数据结果
            绝缘数据结果 = transform.TransInt16(Content, 24);
            //匝间数据结果
            匝间数据结果 = transform.TransInt16(Content, 26);
            //电感数据结果
            电感数据结果 = transform.TransInt16(Content, 28);
            //电阻数据结果
            电阻数据结果 = transform.TransInt16(Content, 30);
            //相机数据结果
            相机数据结果 = transform.TransInt16(Content, 32);
            //通规数据结果
            通规数据结果 = transform.TransInt16(Content, 34);

            耐压mA = transform.TransSingle(Content, 40);
            绝缘电阻Mohm = transform.TransSingle(Content, 44);
         //   线圈3绝缘数据 = transform.TransSingle(Content, 48);

            线圈1匝间 = transform.TransSingle(Content, 60).ToString("P");//yaode 
            线圈1匝间结果 = transform.TransSingle(Content, 76);   //yaode 

            线圈2匝间 = transform.TransSingle(Content, 80).ToString("P");//yaode 
            线圈2匝间结果 = transform.TransSingle(Content, 96);//yaode 

            线圈3匝间 = transform.TransSingle(Content, 100).ToString("P");//yaode 

            线圈3匝间结果 = transform.TransSingle(Content, 116); //yaode 

            线圈1电感uH = transform.TransSingle(Content, 120); //yao
            线圈2电感uH = transform.TransSingle(Content, 124);//y
            线圈3电感uH = transform.TransSingle(Content, 128);//y
            线圈1电感结果 = transform.TransInt16(Content, 132);
            线圈2电感结果 = transform.TransInt16(Content, 134);
            线圈3电感结果 = transform.TransInt16(Content, 136);
            电感1平衡度 = transform.TransSingle(Content, 282);
            电感2平衡度 = transform.TransSingle(Content, 286);
            电感3平衡度 = transform.TransSingle(Content, 290);
   
            线圈1电阻mohm = transform.TransSingle(Content, 140);//Y
            线圈2电阻mohm = transform.TransSingle(Content, 144);//Y
            线圈3电阻mohm = transform.TransSingle(Content, 148);//Y
            线圈1电阻结果 = transform.TransInt16(Content, 152);
            线圈2电阻结果 = transform.TransInt16(Content, 154);
            线圈3电阻结果 = transform.TransInt16(Content, 156);
            电阻平衡度 = transform.TransSingle(Content, 298);

            线圈1位置度_F = transform.TransSingle(Content, 160);
            线圈1位置度_GP = transform.TransSingle(Content, 164);
            线圈1位置度_S = transform.TransSingle(Content, 168);
            线圈2位置度_F = transform.TransSingle(Content, 172);
            线圈2位置度_GP = transform.TransSingle(Content, 176);
            线圈2位置度_S = transform.TransSingle(Content, 180);
            线圈3位置度_F = transform.TransSingle(Content, 184);
            线圈3位置度_GP = transform.TransSingle(Content, 188);
            线圈3位置度_S = transform.TransSingle(Content, 192);

            线圈1高度_F = transform.TransSingle(Content, 200);
            线圈1高度_GP = transform.TransSingle(Content, 204);
            线圈1高度_S = transform.TransSingle(Content, 208);
            线圈2高度_F = transform.TransSingle(Content, 212);
            线圈2高度_GP = transform.TransSingle(Content, 216);
            线圈2高度_S = transform.TransSingle(Content, 220);
            线圈3高度_F = transform.TransSingle(Content, 224);
            线圈3高度_GP = transform.TransSingle(Content, 228);
            图像2数据9 = transform.TransSingle(Content, 232);
            线圈3高度_S = transform.TransSingle(Content, 240);
            通规数据 = transform.TransSingle(Content, 244);
            读码数据 = transform.TransString(Content, 250, 32, Encoding.UTF8);
          
            //ADD DIANGAN1-2 3-4 5-6PINGHENGDU   282 286 290
            //ADD DIANZU PING HENG DU  298 302 306

        }

        public float PercentToFloat(string value)
        {
            return float.TryParse(value.TrimEnd('%'), out float v) ? v / 100f : 0f;
        }

        public byte[] ToSource()
        {
            byte[] bytes = new byte[ReadCount];
            Array.Copy(transform.TransByte(读码数据结果), 0, bytes, 0, 2);
            Array.Copy(transform.TransByte(OK数据结果), 0, bytes, 4, 2);
            Array.Copy(transform.TransByte(NG数据结果), 0, bytes, 6, 2);
            Array.Copy(transform.TransByte(绝缘数据结果), 0, bytes, 24, 2);
            Array.Copy(transform.TransByte(匝间数据结果), 0, bytes, 26, 2);
            Array.Copy(transform.TransByte(电感数据结果), 0, bytes, 28, 2);
            Array.Copy(transform.TransByte(电阻数据结果), 0, bytes, 30, 2);
            Array.Copy(transform.TransByte(相机数据结果), 0, bytes, 32, 2);
            Array.Copy(transform.TransByte(通规数据结果), 0, bytes, 34, 2);

            Array.Copy(transform.TransByte(耐压mA), 0, bytes, 40, 4);
            Array.Copy(transform.TransByte(绝缘电阻Mohm), 0, bytes, 44, 4);
           // Array.Copy(transform.TransByte(线圈3绝缘数据), 0, bytes, 48, 4);

            Array.Copy(transform.TransByte(PercentToFloat(线圈1匝间)), 0, bytes, 60, 4);

            Array.Copy(transform.TransByte(线圈1匝间结果), 0, bytes, 76, 4);

            Array.Copy(transform.TransByte(PercentToFloat(线圈2匝间)), 0, bytes, 80, 4);

            Array.Copy(transform.TransByte(线圈2匝间结果), 0, bytes, 96, 4);

            Array.Copy(transform.TransByte(PercentToFloat(线圈3匝间)), 0, bytes, 100, 4);
            Array.Copy(transform.TransByte(线圈3匝间结果), 0, bytes, 116, 4);

            Array.Copy(transform.TransByte(线圈1电感uH), 0, bytes, 120, 4);
            Array.Copy(transform.TransByte(线圈2电感uH), 0, bytes, 124, 4);
            Array.Copy(transform.TransByte(线圈3电感uH), 0, bytes, 128, 4);

            Array.Copy(transform.TransByte(线圈1电感结果), 0, bytes, 132, 2);
            Array.Copy(transform.TransByte(线圈2电感结果), 0, bytes, 134, 2);
            Array.Copy(transform.TransByte(线圈3电感结果), 0, bytes, 136, 2);


            Array.Copy(transform.TransByte(线圈1电阻mohm), 0, bytes, 140, 4);
            Array.Copy(transform.TransByte(线圈2电阻mohm), 0, bytes, 144, 4);
            Array.Copy(transform.TransByte(线圈3电阻mohm), 0, bytes, 148, 4);
            Array.Copy(transform.TransByte(线圈1电阻结果), 0, bytes, 152, 2);
            Array.Copy(transform.TransByte(线圈2电阻结果), 0, bytes, 154, 2);
            Array.Copy(transform.TransByte(线圈3电阻结果), 0, bytes, 156, 2);




            Array.Copy(transform.TransByte(线圈1位置度_F), 0, bytes, 160, 4);
            Array.Copy(transform.TransByte(线圈1位置度_GP), 0, bytes, 164, 4);
            Array.Copy(transform.TransByte(线圈1位置度_S), 0, bytes, 168, 4);
            Array.Copy(transform.TransByte(线圈2位置度_F), 0, bytes, 172, 4);
            Array.Copy(transform.TransByte(线圈2位置度_GP), 0, bytes, 176, 4);
            Array.Copy(transform.TransByte(线圈2位置度_S), 0, bytes, 180, 4);
            Array.Copy(transform.TransByte(线圈3位置度_F), 0, bytes, 184, 4);
            Array.Copy(transform.TransByte(线圈3位置度_GP), 0, bytes, 188, 4);
            Array.Copy(transform.TransByte(线圈3位置度_S), 0, bytes, 192, 4);

            Array.Copy(transform.TransByte(线圈1高度_F), 0, bytes, 200, 4);
            Array.Copy(transform.TransByte(线圈1高度_GP), 0, bytes, 204, 4);
            Array.Copy(transform.TransByte(线圈1高度_S), 0, bytes, 208, 4);
            Array.Copy(transform.TransByte(线圈2高度_F), 0, bytes, 212, 4);
            Array.Copy(transform.TransByte(线圈2高度_GP), 0, bytes, 216, 4);
            Array.Copy(transform.TransByte(线圈2高度_S), 0, bytes, 220, 4);
            Array.Copy(transform.TransByte(线圈3高度_F), 0, bytes, 224, 4);
            Array.Copy(transform.TransByte(线圈3高度_GP), 0, bytes, 228, 4);
            Array.Copy(transform.TransByte(图像2数据9), 0, bytes, 232, 4);
            Array.Copy(transform.TransByte(线圈3高度_S), 0, bytes, 240, 4);
            Array.Copy(transform.TransByte(通规数据), 0, bytes, 244, 4);
            var bs = transform.TransByte(读码数据, Encoding.UTF8);
            Array.Copy(bs, 0, bytes, 250, 32);
            Array.Copy(transform.TransByte(电感1平衡度), 0, bytes, 282, 4);
            Array.Copy(transform.TransByte(电感2平衡度), 0, bytes, 286, 4);
            Array.Copy(transform.TransByte(电感3平衡度), 0, bytes, 290, 4);

            Array.Copy(transform.TransByte(电阻平衡度), 0, bytes, 298, 4);
  
            return bytes;
        }
    }
}