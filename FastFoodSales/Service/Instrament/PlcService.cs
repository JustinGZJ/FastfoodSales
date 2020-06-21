using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HslCommunication;
using HslCommunication.Core;
using HslCommunication.Profinet.Omron;
using HslCommunication.Profinet.Siemens;
using Stylet;
using StyletIoC;

namespace DAQ.Service
{
    public class EventIO
    {
        public int Index { get; set; }
        public bool Value { get; set; }
    }

    public class PlcService : PropertyChangedBase
    {
        SiemensS7Net _rw;
        string addr = "DB3000.0";
        public bool IsConnected { get; set; }

        readonly bool[] Bits = new bool[32];
        readonly string[] BitTags = new string[32] {
                "空0-0",
                "空0-1",
                "通讯建立开始",
                "通讯建立完成",
                "OK数据上传开始",
                "OK数据上传完成",
                "NG数据上传开始",
                "NG数据上传完成",

                "绝缘数据获取开始",
                "匝间数据1获取开始",
                "匝间数据2获取开始",
                "匝间数据3获取开始",
                "电感数据获取开始",
                "电阻数据获取开始",
                "",
                "",

                "空2-0",
                "空2-1",
                "空2-2",
                "空2-3",
                "空2-4",
                "空2-5",
                "空2-6",
                "空2-7",

                "绝缘数据获取完成",
                "匝间数据1获取完成",
                "匝间数据2获取完成",
                "匝间数据3获取完成",
                "电感数据获取完成",
                "电阻数据获取完成",
                "空3-6",
                "空3-7"
                };
        public BindableCollection<KV<bool>> KVBits { get; set; } = new BindableCollection<KV<bool>>();
        MsgFileSaver<PLC_FINAL_DATA> saver;

        public IEventAggregator Events { get; set; }
        public BindableCollection<PLC_FINAL_DATA> PLC_FINAL_DATAS { get; set; } = new BindableCollection<PLC_FINAL_DATA>();
        public LiveCharts.ChartValues<int> PassValues { get; set; } = new LiveCharts.ChartValues<int>(new int[8]);
        public LiveCharts.ChartValues<int> FailValues { get; set; } = new LiveCharts.ChartValues<int>(new int[8]);

        public PlcService([Inject] IEventAggregator eventAggregator, [Inject] MsgFileSaver<PLC_FINAL_DATA> saver)
        {
            this.saver = saver;
            this.Events = eventAggregator;
            for (int i = 0; i < Bits.Length; i++)
            {
                KVBits.Add(new KV<bool>() { Key = BitTags[i], Value = Bits[i], Index = i, Time = DateTime.Now });
            }
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
            _rw = new SiemensS7Net(SiemensPLCS.S1200, "192.168.0.1");
            Task.Factory.StartNew(() =>
            {
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
                                Events.PublishMsg("PLC", $"Bit[{i}]:" + (v ? "Rising edge" : "Failing edge"));
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
                                        saver.Process(DATA);
                                    else
                                        WriteBool(0, true);
                                    Pulse((int)IO_DEF.OK数据上传完成);
                                }
                                if (i == (int)IO_DEF.NG数据上传开始 && v)
                                {
                                    var DATA = ReadTestData("DB3011.0");
                                    Events.PublishMsg("PLC", "NG数据上传");
                                    if (saver.CanProcess())
                                        saver.Process(DATA);
                                    else
                                        WriteBool(0, true);
                                    AddPLCData(DATA);
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
                    System.Threading.Thread.Sleep(10);
                }
            });
            return IsConnected;
        }
        int dbIndex = 3000;
        int offset = 0;
        public bool WriteBool(int index, bool value)
        {
            if (_rw is SiemensS7Net s7)
            {

                var opr = s7.Read($"DB{dbIndex}.{offset}", 4);
                if (opr.IsSuccess)
                {
                    byte m;
                    if (value)
                    {
                        m = (byte)(opr.Content[index / 8] | ((ushort)(1 << (index % 8))));
                    }
                    else
                    {
                        m = (byte)(opr.Content[index / 8] & (~(1 << (index % 8))));
                    }
                    return s7.Write($"DB{dbIndex}.{offset + index / 8}", m).IsSuccess;
                }
            }
            return false;
        }

        public void Pulse(int bitIndex, int Delayms = 200)
        {
            Task.Factory.StartNew(() =>
            {
                WriteBool(bitIndex, true);
                System.Threading.Thread.Sleep(Delayms);
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

        public void WriteIR(float[] values)
        {
            var data = ReadTestData("DB3002.0");
            data.线圈1绝缘数据 = values[0];
            data.线圈2绝缘数据 = values[1];
            WriteTestData("DB3002.0", data);
        }

        public void WriteZJ(float[] values, int index)//匝间
        {
            var data = ReadTestData("DB3003.0");
            switch (index)
            {
                case 0:
                    data.线圈1匝间A = values[0].ToString("P");
                    data.线圈1匝间D = values[1];
                    data.线圈1匝间C = values[2];
                    data.线圈1匝间Z = values[3];
                    data.线圈1匝间结果 = values[4];
                    break;
                case 1:
                    data.线圈2匝间A = values[0].ToString("P");
                    data.线圈2匝间D = values[1];
                    data.线圈2匝间C = values[2];
                    data.线圈2匝间Z = values[3];
                    data.线圈2匝间结果 = values[4];
                    break;
                case 2:
                    data.线圈3匝间A = values[0].ToString("P");
                    data.线圈3匝间D = values[1];
                    data.线圈3匝间C = values[2];
                    data.线圈3匝间Z = values[3];
                    data.线圈3匝间结果 = values[4];
                    break;
            }

            WriteTestData("DB3003.0", data);
        }

        public void WriteLS(float[] values)
        {
            var data = ReadTestData("DB3004.0");
            data.线圈1电感 = values[0];
            data.线圈2电感 = values[1];
            data.线圈3电感 = values[2];
            WriteTestData("DB3004.0", data);
        }

        public void WriteR(float[] values)
        {
            var data = ReadTestData("DB3005.0");
            data.线圈1电阻 = values[0];
            data.线圈2电阻 = values[1];
            data.线圈3电阻 = values[2];
            WriteTestData("DB3005.0", data);
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
        public float 线圈1绝缘数据 { get; set; }
        //绝缘数据2
        public float 线圈2绝缘数据 { get; set; }
        //绝缘数据3
        public float 线圈3绝缘数据 { get; set; }
        //匝间测试数据1-1
        public string 线圈1匝间A { get; set; }
        //匝间测试数据1-2
        public float 线圈1匝间D { get; set; }
        //匝间测试数据1-3
        public float 线圈1匝间C { get; set; }
        //匝间测试数据1-4
        public float 线圈1匝间Z { get; set; }

        public float 线圈1匝间结果 { get; set; }
        //匝间测试数据2-1
        public string 线圈2匝间A { get; set; }
        //匝间测试数据2-2
        public float 线圈2匝间D { get; set; }
        //匝间测试数据2-3
        public float 线圈2匝间C { get; set; }
        //匝间测试数据2-4
        public float 线圈2匝间Z { get; set; }

        public float 线圈2匝间结果 { get; set; }
        //匝间测试数据3-1
        public string 线圈3匝间A { get; set; }
        //匝间测试数据3-2
        public float 线圈3匝间D { get; set; }
        //匝间测试数据3-3
        public float 线圈3匝间C { get; set; }
        //匝间测试数据3-4
        public float 线圈3匝间Z { get; set; }
        //匝间预留3
        public float 线圈3匝间结果 { get; set; }
        //电感测试数据1
        public float 线圈1电感 { get; set; }
        //电感测试数据2
        public float 线圈2电感 { get; set; }
        //电感测试数据3
        public float 线圈3电感 { get; set; }
        //电阻测试数据1
        public float 线圈1电阻 { get; set; }
        //电阻测试数据2
        public float 线圈2电阻 { get; set; }
        //电阻测试数据3
        public float 线圈3电阻 { get; set; }

        public string 电阻差
        {
            get
            {
                float[] vs = new float[3];
                vs[0] = 线圈1电阻;
                vs[1] = 线圈2电阻;
                vs[2] = 线圈3电阻;
                return ((vs.Max() - vs.Min()) / 48f).ToString("P");
            }
        }
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
        public float 止规数据 { get; set; }

        public string Source { get; set; } = "生产数据";


        public ushort ReadCount { get; } = 282;

        ReverseBytesTransform transform = new ReverseBytesTransform();
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

            线圈1绝缘数据 = transform.TransSingle(Content, 40);
            线圈2绝缘数据 = transform.TransSingle(Content, 44);
            线圈3绝缘数据 = transform.TransSingle(Content, 48);

            线圈1匝间A = transform.TransSingle(Content, 60).ToString("P");
            线圈1匝间D = transform.TransSingle(Content, 64);
            线圈1匝间C = transform.TransSingle(Content, 68);
            线圈1匝间Z = transform.TransSingle(Content, 72);
            线圈1匝间结果 = transform.TransSingle(Content, 76);

            线圈2匝间A = transform.TransSingle(Content, 80).ToString("P");
            线圈2匝间D = transform.TransSingle(Content, 84);
            线圈2匝间C = transform.TransSingle(Content, 88);
            线圈2匝间Z = transform.TransSingle(Content, 92);
            线圈2匝间结果 = transform.TransSingle(Content, 96);

            线圈3匝间A = transform.TransSingle(Content, 100).ToString("P");
            线圈3匝间D = transform.TransSingle(Content, 104);
            线圈3匝间C = transform.TransSingle(Content, 108);
            线圈3匝间Z = transform.TransSingle(Content, 112);
            线圈3匝间结果 = transform.TransSingle(Content, 116);

            线圈1电感 = transform.TransSingle(Content, 120);
            线圈2电感 = transform.TransSingle(Content, 124);
            线圈3电感 = transform.TransSingle(Content, 128);

            线圈1电阻 = transform.TransSingle(Content, 140);
            线圈2电阻 = transform.TransSingle(Content, 144);
            线圈3电阻 = transform.TransSingle(Content, 148);

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
            止规数据 = transform.TransSingle(Content, 244);
            读码数据 = transform.TransString(Content, 250, 32, Encoding.UTF8);
        }

        public float PercentToFloat(string value)
        {
             return  float.TryParse(value.TrimEnd('%'),out float v)?v/100f:0f;
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

            Array.Copy(transform.TransByte(线圈1绝缘数据), 0, bytes, 40, 4);
            Array.Copy(transform.TransByte(线圈2绝缘数据), 0, bytes, 44, 4);
            Array.Copy(transform.TransByte(线圈3绝缘数据), 0, bytes, 48, 4);

            Array.Copy(transform.TransByte(PercentToFloat(线圈1匝间A)), 0, bytes, 60, 4);
            Array.Copy(transform.TransByte(线圈1匝间D), 0, bytes, 64, 4);
            Array.Copy(transform.TransByte(线圈1匝间C), 0, bytes, 68, 4);
            Array.Copy(transform.TransByte(线圈1匝间Z), 0, bytes, 72, 4);
            Array.Copy(transform.TransByte(线圈1匝间结果), 0, bytes, 76, 4);

            Array.Copy(transform.TransByte(PercentToFloat(线圈2匝间A)), 0, bytes, 80, 4);
            Array.Copy(transform.TransByte(线圈2匝间D), 0, bytes, 84, 4);
            Array.Copy(transform.TransByte(线圈2匝间C), 0, bytes, 88, 4);
            Array.Copy(transform.TransByte(线圈2匝间Z), 0, bytes, 92, 4);
            Array.Copy(transform.TransByte(线圈2匝间结果), 0, bytes, 96, 4);

            Array.Copy(transform.TransByte(PercentToFloat(线圈3匝间A)), 0, bytes, 100, 4);
            Array.Copy(transform.TransByte(线圈3匝间D), 0, bytes, 104, 4);
            Array.Copy(transform.TransByte(线圈3匝间C), 0, bytes, 108, 4);
            Array.Copy(transform.TransByte(线圈3匝间Z), 0, bytes, 112, 4);
            Array.Copy(transform.TransByte(线圈3匝间结果), 0, bytes, 116, 4);

            Array.Copy(transform.TransByte(线圈1电感), 0, bytes, 120, 4);
            Array.Copy(transform.TransByte(线圈2电感), 0, bytes, 124, 4);
            Array.Copy(transform.TransByte(线圈3电感), 0, bytes, 128, 4);

            Array.Copy(transform.TransByte(线圈1电阻), 0, bytes, 140, 4);
            Array.Copy(transform.TransByte(线圈2电阻), 0, bytes, 144, 4);
            Array.Copy(transform.TransByte(线圈3电阻), 0, bytes, 148, 4);

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
            Array.Copy(transform.TransByte(止规数据), 0, bytes, 244, 4);
            var bs = transform.TransByte(读码数据, Encoding.UTF8);
            Array.Copy(bs, 0, bytes, 250, bs.Length);
            return bytes;
        }
    }
}
