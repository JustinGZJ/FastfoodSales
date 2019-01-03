using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HslCommunication;
using HslCommunication.Profinet.Omron;
using Stylet;

namespace DAQ.Service
{
    public class PlcService : PropertyChangedBase
    {
        OmronFinsNet omr;
        string addr = "D100";
        public bool Ready { get; set; }
        public BindableCollection<short> Datas { get; set; } = new BindableCollection<short>(new short[100]);
        public BindableCollection<bool> Bits { get; set; } = new BindableCollection<bool>(new bool[16]);
        public BindableCollection<string> BitTags { get; set; } = new BindableCollection<string>(new string[16]);

        public bool Connect()
        {
            if (omr != null)
            {
                omr.ConnectClose();
                omr = null;
            }
            omr = new OmronFinsNet(Properties.Settings.Default.PLC_IP, Properties.Settings.Default.PLC_PORT)
            {
                DA1 = 0
            };
            var op = omr.ConnectServer();
            if (op.IsSuccess)
            {
                Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        var rop = omr.ReadInt16(addr, 100);
                        Ready = rop.IsSuccess;
                        if (Ready)
                        {
                            if (Datas[0] != rop.Content[0])
                            {
                                for (int i = 0; i < 16; i++)
                                {
                                    Bits[i] = (Datas[0] & (1 << i)) > 0;
                                }
                                NotifyOfPropertyChange("Bits");
                            }
                            for (int i = 0; i < 100; i++)
                            {
                                if (Datas[i] != rop.Content[i])
                                {
                                    Datas[i] = rop.Content[i];
                                }                             
                            }
                        }
                        System.Threading.Thread.Sleep(10);
                    }
                });
            }
            Ready = op.IsSuccess;
            return op.IsSuccess;
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
        public bool WriteValue(int index, float value)
        {
            var r = int.TryParse(addr.Trim().Substring(1), result: out int v);
            if (r)
            {
                return omr.Write($"D{v + index}", value).IsSuccess;
            }
            return false;
        }

        public bool WriteGroupValue(int group,int subidx,float value)
        {
            return WriteValue(group * 4 * 2 + subidx + 1, value);
        }
       
    }
}
