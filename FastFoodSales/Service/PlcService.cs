using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HslCommunication;
using HslCommunication.Core;
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
        
        IReadWriteNet _rw;
        string addr = "D8000";
        private IReadWriteFactory readWriteFactory;
        public bool IsConnected { get; set; }
        public BindableCollection<short> Datas { get; set; } = new BindableCollection<short>(new short[100]);
        public BindableCollection<bool> Bits { get; set; } = new BindableCollection<bool>(new bool[16]
        );
        public BindableCollection<string> BitTags { get; set; } = new BindableCollection<string>(new string[16]);
        public BindableCollection<KV<bool>> KVBits { get; set; } = new BindableCollection<KV<bool>>();

        public BindableCollection<Int32> Ints { get; set; } = new BindableCollection<int>(new int[10]);
     
        public IEventAggregator Events { get; set; }

        public PlcService([Inject]IReadWriteFactory readWriteFactory,[Inject] IEventAggregator eventAggregator)
        {
            this.Events = eventAggregator;
            this.readWriteFactory = readWriteFactory;
            addr = readWriteFactory.AddressA;

            BitTags[0] = "启动电阻测试";
            BitTags[1] = "启动耐压测试";
            BitTags[2] = "RESISTANCE 1";
            BitTags[3] = "RESISTANCE 2";
            BitTags[4] = "RESISTANCE 3";
            BitTags[5] = "RESISTANCE 4";
            BitTags[8] = "完成电阻测试";
            BitTags[9] = "完成耐压测试";
            BitTags[10] = "HI-POT 1";
            BitTags[11] = "HI-POT 2";
            BitTags[12] = "HI-POT 3";
            BitTags[13] = "HI-POT 4";
            BitTags[15] = "PLC握手";
            for (int i = 0; i < Bits.Count; i++)
            {
                KVBits.Add(new KV<bool>() { Key = BitTags[i], Value = Bits[i], Index = i, Time = DateTime.Now });
            }
        }
        public bool Connect()
        {
            if (_rw != null)
            {
                _rw = null;
            }

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
                                    if (i < 2)
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
                       var RI =_rw.ReadInt32(readWriteFactory.AddressB, 10); 
                       if(RI.IsSuccess)
                        {
                            for(int i=0;i<10;i++)
                            {
                                if(RI.Content[i]!=Ints[i])
                                {
                                    Ints[i] = RI.Content[i];
                                }
                            }
                        }
                    }
                    System.Threading.Thread.Sleep(10);
                }

            });
            return IsConnected;
        }

        public bool WriteBool(int index, bool value)
        {
            var opr = _rw.ReadUInt16(addr);
            if (opr.IsSuccess)
            {
                ushort m;
                if (value)
                {
                     m= (UInt16) (opr.Content | ((ushort)(1 << index)));
                }
                else
                {
                    m = (UInt16)(opr.Content & (~(1 << index)));
                }

              return  _rw.Write(addr,m).IsSuccess;
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





 
    }
}
