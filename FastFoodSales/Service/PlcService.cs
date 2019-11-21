using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        IReadWriteNet _rw;
        string addr = "D8000";
        private IReadWriteFactory readWriteFactory;
        public bool IsConnected { get; set; }
        public BindableCollection<short> Datas { get; set; } = new BindableCollection<short>(new short[100]);
        public BindableCollection<bool> Bits { get; set; } = new BindableCollection<bool>(new bool[16]
        );
        string[] _bitTags = new string[16];
        public BindableCollection<KV<bool>> KVBits { get; set; } = new BindableCollection<KV<bool>>();

        public BindableCollection<Int32> Ints { get; set; } = new BindableCollection<int>(new int[10]);
        public BindableCollection<KV<float>> KvFloats { get; } = new BindableCollection<KV<float>>();

        public IEventAggregator Events { get; set; }


        public PlcService([Inject]IReadWriteFactory readWriteFactory, [Inject] IEventAggregator eventAggregator)
        {
            this.Events = eventAggregator;
            this.readWriteFactory = readWriteFactory;
            addr = readWriteFactory.AddressA;
            _bitTags[0] = "启动电阻测试";
            _bitTags[1] = "启动耐压测试";
            _bitTags[2] = "RESISTANCE 1";
            _bitTags[3] = "RESISTANCE 2";
            _bitTags[4] = "RESISTANCE 3";
            _bitTags[5] = "RESISTANCE 4";
            _bitTags[6] = "图像读取请求";
            _bitTags[7] = "图像读取完成";
            _bitTags[8] = "完成电阻测试";
            _bitTags[9] = "完成耐压测试";
            _bitTags[10] = "HI-POT 1";
            _bitTags[11] = "HI-POT 2";
            _bitTags[12] = "HI-POT 3";
            _bitTags[13] = "HI-POT 4";
            _bitTags[15] = "PLC握手";
            for (int i = 0; i < Bits.Count; i++)
            {
                KVBits.Add(new KV<bool>() { Key = _bitTags[i], Value = Bits[i], Index = i, Time = DateTime.Now });
            }
            KvFloats.Add(new  KV<float>
            {
                Index = 0,
                Time = DateTime.Now,
                Key = "1Y",
                Value = 0
            });
            KvFloats.Add(new KV<float>
            {
                Index = 1,
                Time = DateTime.Now,
                Key = "1X",
                Value = 0
            });
            KvFloats.Add(new KV<float>
            {
                Index = 2,
                Time = DateTime.Now,
                Key = "2Y",
                Value = 0
            });
            KvFloats.Add(new KV<float>
            {
                Index = 3,
                Time = DateTime.Now,
                Key = "2X",
                Value = 0
            });
            KvFloats.Add(new KV<float>
            {
                Index = 4,
                Time = DateTime.Now,
                Key = "RESULT",
                Value = 0
            });
        }
        public bool Connect()
        {
            _rw = readWriteFactory.GetReadWriteNet();
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var rop = _rw.ReadInt16(addr);
                    IsConnected = rop.IsSuccess;
                    if (!rop.IsSuccess)
                        Events.Publish(new MsgItem { Time = DateTime.Now, Level = "E", Value = rop.Message });
                    if (IsConnected)
                    {
                        var RI = _rw.ReadInt32(readWriteFactory.AddressB, 10);
                        if (RI.IsSuccess)
                        {
                            for (int i = 0; i < 10; i++)
                            {
                                if (RI.Content[i] != Ints[i])
                                {
                                    Ints[i] = RI.Content[i];
                                }
                            }
                        }
                        var rs = _rw.ReadFloat(readWriteFactory.CameraDataAddr, 5);
                        if (rs.IsSuccess)
                        {
                            for (int i = 0; i < 5; i++)
                            {
                               KvFloats[i].Value =rs.Content[i];
                            }
                        }
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
                                    if (new[] { 0, 1, 6 }.Contains(i))
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
                    }
                    Thread.Sleep(10);
                }
            });
            return IsConnected;
        }

        public bool WriteBool(int index, bool value)
        {
            if (_rw is SiemensS7Net s7)
            {
                var opr = s7.ReadUInt16(addr);
                if (opr.IsSuccess)
                {
                    ushort m;
                    if (value)
                    {
                        m = (UInt16)(opr.Content | ((ushort)(1 << index)));
                    }
                    else
                    {
                        m = (UInt16)(opr.Content & (~(1 << index)));
                    }
                    return s7.Write(addr, m).IsSuccess;
                }
            }
            else
            {
                var opr = _rw.ReadUInt16(addr);
                if (opr.IsSuccess)
                {
                    ushort m;
                    if (value)
                    {
                        m = (UInt16)(opr.Content | ((ushort)(1 << index)));
                    }
                    else
                    {
                        m = (UInt16)(opr.Content & (~(1 << index)));
                    }
                    return _rw.Write(addr, m).IsSuccess;
                }
            }
            return false;

        }

        public void Pulse(int bitIndex, int Delayms = 100)
        {
            Task.Factory.StartNew(() =>
            {
                WriteBool(bitIndex, true);
                Thread.Sleep(Delayms);
                WriteBool(bitIndex, false);
            });
        }
    }
}
