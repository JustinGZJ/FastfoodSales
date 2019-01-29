using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HslCommunication;
using HslCommunication.Profinet.Omron;
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
        OmronFinsNet omr;
        string addr = "D8000";
        public bool IsConnected { get; set; }
        public BindableCollection<short> Datas { get; set; } = new BindableCollection<short>(new short[100]);
        public BindableCollection<bool> Bits { get; set; } = new BindableCollection<bool>(new bool[16]
        );
        public BindableCollection<string> BitTags { get; set; } = new BindableCollection<string>(new string[16]);
        public BindableCollection<string> FloatsTags = new BindableCollection<string>(new string[50]);

        public BindableCollection<KV<bool>> KVBits { get; set; } = new BindableCollection<KV<bool>>();
        [Inject]
        public IEventAggregator Events { get; set; }

        public bool Connect()
        {
            if (omr != null)
            {
                omr.ConnectClose();
                omr = null;
            }
            omr = new OmronFinsNet(Properties.Settings.Default.PLC_IP, Properties.Settings.Default.PLC_PORT)
            {
                SA1 = 0,
                DA1 = 0
            };
            Task.Factory.StartNew(() =>
            {


                while (true)
                {
                    var op = omr.ConnectServer();
                    IsConnected = op.IsSuccess;
                    if (!op.IsSuccess)
                        Events.PublishOnUIThread(new MsgItem { Time = DateTime.Now, Level = "E", Value = op.Message });
                    var rop = omr.ReadInt16(addr);


                    if (IsConnected)
                    {
                        if (Datas[0] != rop.Content)
                        {
                            Datas[0] = rop.Content;
                            for (int i = 0; i < 16; i++)
                            {
                                bool v = (Datas[0] & (1 << i)) > 0;
                                if (Bits[i] != v)
                                {
                                    Bits[i] = v;
                                    KVBits[i].Value = v;
                                    if (i < 8)
                                    {
                                                                                Events.Publish(new MsgItem
                                        {
                                            Level = "D",
                                            Time = DateTime.Now,
                                            Value = $"Bit[{i}]:" + (v ? "Rising edge" : "Failing edge")
                                        });
                                        Events.Publish(new EventIO
                                        {
                                            Index = i,
                                            Value = v
                                        });

                                    }
                                }

                            }
                            if (Bits[15] == true)
                            {
                                WriteBool(15, false);
                            }
                        }
                        omr.ConnectClose();
                    }
                   System.Threading.Thread.Sleep(10);
                }

            });
            return IsConnected;
        }

        public bool WriteBool(int index, bool value)
        {
           
            var op = omr.Write($"{addr}.{index}", value);
            return op.IsSuccess;
        }

        public bool WriteValue(int index, ushort value)
        {
            var r = int.TryParse(addr.Trim().Substring(1), result: out int v);
            if (r)
            {
                return omr.Write($"D{v + index}", value).IsSuccess;
            }
            return false;
        }
        public void Pulse(int bitIndex, int Delayms = 100)
        {
            Task.Factory.StartNew(() =>
            {
                WriteBool(bitIndex, true);
                System.Threading.Thread.Sleep(Delayms);
                WriteBool(bitIndex, false);
            });
        }

        public PlcService()
        {
            for (int i = 0; i < 4; i++)
            {
                FloatsTags[4 * i + 0] = $"电阻 {i + 1} :上限";
                FloatsTags[4 * i + 1] = $"电阻 {i + 1} :下限";
                FloatsTags[4 * i + 2] = $"电阻 {i + 1} :测试值";
                FloatsTags[4 * i + 3] = $"电阻 {i + 1} :判定";
                FloatsTags[4 * i + 0 + 4 * 4] = $"耐压 {i + 1} :上限";
                FloatsTags[4 * i + 1 + 4 * 4] = $"耐压 {i + 1} :下限";
                FloatsTags[4 * i + 2 + 4 * 4] = $"耐压 {i + 1} :测试值";
                FloatsTags[4 * i + 3 + 4 * 4] = $"耐压 {i + 1} :判定";
            }
            BitTags[0] = "启动电阻测试";
            BitTags[1] = "启动耐压测试";
            BitTags[8] = "完成电阻测试";
            BitTags[9] = "完成耐压测试";

            for (int i = 0; i < Bits.Count; i++)
            {
                KVBits.Add(new KV<bool>() { Key = BitTags[i], Value = Bits[i], Index = i, Time = DateTime.Now });
            }

        }

        public bool WriteValue(int index, float value)
        {
            var r = int.TryParse(addr.Trim().Substring(1), result: out int v);
            if (r)
            {
                return omr.Write($"D{v + index}", value).IsSuccess;
            }
            return false;
        }
        public float GetFloat(int byteindex)
        {
            var bytes = new byte[4];
            if (byteindex > Datas.Count - 1)
                throw new Exception("get float value out of index");
            bytes[0] = BitConverter.GetBytes(Datas[byteindex + 1])[0];
            bytes[1] = BitConverter.GetBytes(Datas[byteindex + 1])[1];
            bytes[2] = BitConverter.GetBytes(Datas[byteindex])[0];
            bytes[3] = BitConverter.GetBytes(Datas[byteindex])[1];
            var single = BitConverter.ToSingle(bytes, 0);
            return single;
        }
        public float GetGroupValue(int group, int subidx)
        {
            return GetFloat(group * 4 * 2 + subidx + 1);
        }
        public bool WriteGroupValue(int group, int subidx, float value)
        {
            return WriteValue(group * 4 * 2 + subidx + 1, value);
        }
    }
}
