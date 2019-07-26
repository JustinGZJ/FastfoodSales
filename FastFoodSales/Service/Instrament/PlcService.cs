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

        readonly short[] Datas = new short[100];
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

        public PlcService([Inject] IEventAggregator eventAggregator,[Inject] MsgFileSaver<PLC_FINAL_DATA> saver)
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
                    var rop = _rw.Read(addr, 4);
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
                                if(v)
                                {
                                    Events.Publish(new EventIO
                                    {
                                        Index = i,
                                        Value = v
                                    });
                                }
                 
                                if((i==(int)IO_DEF.通讯建立开始)&&v)
                                {
                                    Events.PublishMsg("PLC", "通信建立连接");
                                    Pulse(((int)IO_DEF.通讯建立完成));
                                }
                                if(i==(int)IO_DEF.OK数据上传开始&&v)
                                {
                                    var DATA = ReadTestData("DB3010.0");
                                    Events.PublishMsg("PLC", "OK数据上传");
                                    AddPLCData(DATA);
                                    saver.Process(DATA);
                                
                                    Pulse((int)IO_DEF.OK数据上传完成);
                                }
                                if (i == (int)IO_DEF.NG数据上传开始 && v)
                                {
                                    var DATA = ReadTestData("DB3011.0");
                                    Events.PublishMsg("PLC", "NG数据上传");
                                    saver.Process(DATA);
                                    AddPLCData(DATA);
                                    Pulse((int)IO_DEF.NG数据上传完成);
                                }
                            }
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
            data.绝缘数据1 = values[0];
            data.绝缘数据2 = values[1];
            WriteTestData("DB3002.0", data);
        }

        public void WriteZJ(float[] values,int index)//匝间
        {
            var data = ReadTestData("DB3003.0");
            switch(index)
            {
                case 0:
                    data.匝间测试数据1_1 = values[0];
                    data.匝间测试数据1_2 = values[1];
                    data.匝间测试数据1_3 = values[2];
                    data.匝间测试数据1_4 = values[3];
                    data.匝间1结果 = values[4];
                    break;
                case 1:
                    data.匝间测试数据2_1 = values[0];
                    data.匝间测试数据2_2 = values[1];
                    data.匝间测试数据2_3 = values[2];
                    data.匝间测试数据2_4 = values[3];
                    data.匝间2结果 = values[4];
                    break;
                case 2:
                    data.匝间测试数据3_1 = values[0];
                    data.匝间测试数据3_2 = values[1];
                    data.匝间测试数据3_3 = values[2];
                    data.匝间测试数据3_4 = values[3];
                    data.匝间3结果 = values[4];
                    break;
            }

            WriteTestData("DB3003.0", data);
        }

        public void WriteLS(float[] values)
        {
            var data = ReadTestData("DB3004.0");
            data.电感测试数据1 = values[0];
            data.电感测试数据2 = values[1];
            data.电感测试数据3 = values[2];
            WriteTestData("DB3004.0", data);
        }

        public void WriteR(float[] values)
        {
            var data = ReadTestData("DB3005.0");
            data.电阻测试数据1 = values[0];
            data.电阻测试数据2 = values[1];
            data.电阻测试数据3 = values[2];
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
        public float 绝缘数据1 { get; set; }
        //绝缘数据2
        public float 绝缘数据2 { get; set; }
        //绝缘数据3
        public float 绝缘数据3 { get; set; }
        //匝间测试数据1-1
        public float 匝间测试数据1_1 { get; set; }
        //匝间测试数据1-2
        public float 匝间测试数据1_2 { get; set; }
        //匝间测试数据1-3
        public float 匝间测试数据1_3 { get; set; }
        //匝间测试数据1-4
        public float 匝间测试数据1_4 { get; set; }

        public float 匝间1结果 { get; set; }
        //匝间测试数据2-1
        public float 匝间测试数据2_1 { get; set; }
        //匝间测试数据2-2
        public float 匝间测试数据2_2 { get; set; }
        //匝间测试数据2-3
        public float 匝间测试数据2_3 { get; set; }
        //匝间测试数据2-4
        public float 匝间测试数据2_4 { get; set; }

        public float 匝间2结果 { get; set; }
        //匝间测试数据3-1
        public float 匝间测试数据3_1 { get; set; }
        //匝间测试数据3-2
        public float 匝间测试数据3_2 { get; set; }
        //匝间测试数据3-3
        public float 匝间测试数据3_3 { get; set; }
        //匝间测试数据3-4
        public float 匝间测试数据3_4 { get; set; }
        //匝间预留3
        public float 匝间3结果 { get; set; }
        //电感测试数据1
        public float 电感测试数据1 { get; set; }
        //电感测试数据2
        public float 电感测试数据2 { get; set; }
        //电感测试数据3
        public float 电感测试数据3 { get; set; }
        //电阻测试数据1
        public float 电阻测试数据1 { get; set; }
        //电阻测试数据2
        public float 电阻测试数据2 { get; set; }
        //电阻测试数据3
        public float 电阻测试数据3 { get; set; }
        //图像1数据1
        public float 图像1数据1 { get; set; }
        //图像1数据2
        public float 图像1数据2 { get; set; }
        //图像1数据3
        public float 图像1数据3 { get; set; }
        //图像1数据4
        public float 图像1数据4 { get; set; }
        //图像1数据5
        public float 图像1数据5 { get; set; }
        //图像1数据6
        public float 图像1数据6 { get; set; }
        //图像1数据7
        public float 图像1数据7 { get; set; }
        //图像1数据8
        public float 图像1数据8 { get; set; }
        //图像1数据9
        public float 图像1数据9 { get; set; }
        //图像2数据1
        public float 图像2数据1 { get; set; }
        //图像2数据2
        public float 图像2数据2 { get; set; }
        //图像2数据3
        public float 图像2数据3 { get; set; }
        //图像2数据4
        public float 图像2数据4 { get; set; }
        //图像2数据5
        public float 图像2数据5 { get; set; }
        //图像2数据6
        public float 图像2数据6 { get; set; }
        //图像2数据7
        public float 图像2数据7 { get; set; }
        //图像2数据8
        public float 图像2数据8 { get; set; }
        //图像2数据9
        public float 图像2数据9 { get; set; }
        //通规数据
        public float 通规数据 { get; set; }
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

            绝缘数据1 = transform.TransSingle(Content, 40);
            绝缘数据2 = transform.TransSingle(Content, 44);
            绝缘数据3 = transform.TransSingle(Content, 48);

            匝间测试数据1_1 = transform.TransSingle(Content, 60);
            匝间测试数据1_2 = transform.TransSingle(Content, 64);
            匝间测试数据1_3 = transform.TransSingle(Content, 68);
            匝间测试数据1_4 = transform.TransSingle(Content, 72);
            匝间1结果 = transform.TransSingle(Content, 76);

            匝间测试数据2_1 = transform.TransSingle(Content, 80);
            匝间测试数据2_2 = transform.TransSingle(Content, 84);
            匝间测试数据2_3 = transform.TransSingle(Content, 88);
            匝间测试数据2_4 = transform.TransSingle(Content, 92);
            匝间2结果 = transform.TransSingle(Content, 96);

            匝间测试数据3_1 = transform.TransSingle(Content, 100);
            匝间测试数据3_2 = transform.TransSingle(Content, 104);
            匝间测试数据3_3 = transform.TransSingle(Content, 108);
            匝间测试数据3_4 = transform.TransSingle(Content, 112);
            匝间3结果 = transform.TransSingle(Content, 116);

            电感测试数据1 = transform.TransSingle(Content, 120);
            电感测试数据2 = transform.TransSingle(Content, 124);
            电感测试数据3 = transform.TransSingle(Content, 128);

            电阻测试数据1 = transform.TransSingle(Content, 140);
            电阻测试数据2 = transform.TransSingle(Content, 144);
            电阻测试数据3 = transform.TransSingle(Content, 148);

            图像1数据1 = transform.TransSingle(Content, 160);
            图像1数据2 = transform.TransSingle(Content, 164);
            图像1数据3 = transform.TransSingle(Content, 168);
            图像1数据4 = transform.TransSingle(Content, 172);
            图像1数据5 = transform.TransSingle(Content, 176);
            图像1数据6 = transform.TransSingle(Content, 180);
            图像1数据7 = transform.TransSingle(Content, 184);
            图像1数据8 = transform.TransSingle(Content, 188);
            图像1数据9 = transform.TransSingle(Content, 192);

            图像2数据1 = transform.TransSingle(Content, 200);
            图像2数据2 = transform.TransSingle(Content, 204);
            图像2数据3 = transform.TransSingle(Content, 208);
            图像2数据4 = transform.TransSingle(Content, 212);
            图像2数据5 = transform.TransSingle(Content, 216);
            图像2数据6 = transform.TransSingle(Content, 220);
            图像2数据7 = transform.TransSingle(Content, 224);
            图像2数据8 = transform.TransSingle(Content, 228);
            图像2数据9 = transform.TransSingle(Content, 232);
            通规数据 = transform.TransSingle(Content, 240);
            止规数据 = transform.TransSingle(Content, 244);
            读码数据 = transform.TransString(Content, 250, 32,Encoding.UTF8);
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

            Array.Copy(transform.TransByte(绝缘数据1), 0, bytes, 40, 4);
            Array.Copy(transform.TransByte(绝缘数据2), 0, bytes, 44, 4);
            Array.Copy(transform.TransByte(绝缘数据3), 0, bytes, 48, 4);

            Array.Copy(transform.TransByte(匝间测试数据1_1), 0, bytes, 60, 4);
            Array.Copy(transform.TransByte(匝间测试数据1_2), 0, bytes, 64, 4);
            Array.Copy(transform.TransByte(匝间测试数据1_3), 0, bytes, 68, 4);
            Array.Copy(transform.TransByte(匝间测试数据1_4), 0, bytes, 72, 4);
            Array.Copy(transform.TransByte(匝间1结果), 0, bytes, 76, 4);

            Array.Copy(transform.TransByte(匝间测试数据2_1), 0, bytes, 80, 4);
            Array.Copy(transform.TransByte(匝间测试数据2_2), 0, bytes, 84, 4);
            Array.Copy(transform.TransByte(匝间测试数据2_3), 0, bytes, 88, 4);
            Array.Copy(transform.TransByte(匝间测试数据2_4), 0, bytes, 92, 4);
            Array.Copy(transform.TransByte(匝间2结果), 0, bytes, 96, 4);

            Array.Copy(transform.TransByte(匝间测试数据3_1), 0, bytes, 100, 4);
            Array.Copy(transform.TransByte(匝间测试数据3_2), 0, bytes, 104, 4);
            Array.Copy(transform.TransByte(匝间测试数据3_3), 0, bytes, 108, 4);
            Array.Copy(transform.TransByte(匝间测试数据3_4), 0, bytes, 112, 4);
            Array.Copy(transform.TransByte(匝间3结果), 0, bytes, 116, 4);

            Array.Copy(transform.TransByte(电感测试数据1), 0, bytes, 120, 4);
            Array.Copy(transform.TransByte(电感测试数据2), 0, bytes, 124, 4);
            Array.Copy(transform.TransByte(电感测试数据3), 0, bytes, 128, 4);

            Array.Copy(transform.TransByte(电阻测试数据1), 0, bytes, 140, 4);
            Array.Copy(transform.TransByte(电阻测试数据2), 0, bytes, 144, 4);
            Array.Copy(transform.TransByte(电阻测试数据3), 0, bytes, 148, 4);

            Array.Copy(transform.TransByte(图像1数据1), 0, bytes, 160, 4);
            Array.Copy(transform.TransByte(图像1数据2), 0, bytes, 164, 4);
            Array.Copy(transform.TransByte(图像1数据3), 0, bytes, 168, 4);
            Array.Copy(transform.TransByte(图像1数据4), 0, bytes, 172, 4);
            Array.Copy(transform.TransByte(图像1数据5), 0, bytes, 176, 4);
            Array.Copy(transform.TransByte(图像1数据6), 0, bytes, 180, 4);
            Array.Copy(transform.TransByte(图像1数据7), 0, bytes, 184, 4);
            Array.Copy(transform.TransByte(图像1数据8), 0, bytes, 188, 4);
            Array.Copy(transform.TransByte(图像1数据9), 0, bytes, 192, 4);

            Array.Copy(transform.TransByte(图像2数据1), 0, bytes, 200, 4);
            Array.Copy(transform.TransByte(图像2数据2), 0, bytes, 204, 4);
            Array.Copy(transform.TransByte(图像2数据3), 0, bytes, 208, 4);
            Array.Copy(transform.TransByte(图像2数据4), 0, bytes, 212, 4);
            Array.Copy(transform.TransByte(图像2数据5), 0, bytes, 216, 4);
            Array.Copy(transform.TransByte(图像2数据6), 0, bytes, 220, 4);
            Array.Copy(transform.TransByte(图像2数据7), 0, bytes, 224, 4);
            Array.Copy(transform.TransByte(图像2数据8), 0, bytes, 228, 4);
            Array.Copy(transform.TransByte(图像2数据9), 0, bytes, 232, 4);
            Array.Copy(transform.TransByte(通规数据), 0, bytes, 240, 4);
            Array.Copy(transform.TransByte(止规数据), 0, bytes, 244, 4);
            var bs = transform.TransByte(读码数据, Encoding.UTF8);
            Array.Copy(bs,0,bytes,250,bs.Length);
            return bytes;
        }
    }
}
