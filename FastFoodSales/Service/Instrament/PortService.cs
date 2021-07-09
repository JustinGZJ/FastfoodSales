using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAQ.Pages;
using Stylet;
using StyletIoC;


namespace DAQ.Service
{
    public class PortService : PropertyChangedBase, IPortService, IHandle<EventIO>
    {
        protected SerialPort port = new SerialPort();
        public virtual string PortName { get; set; }
        public bool IsConnected { get; set; }
        protected IEventAggregator Events { get; set; }
        protected PlcService Plc { get; set; }
        [StyletIoC.Inject]
        public MsgFileSaver<TLog> FileSaver { get; set; }

        protected string InstName { get; set; }

        public BindableCollection<TestSpecViewModel> TestSpecs { get; set; }

        public PortService(PlcService plc, IEventAggregator events)
        {
            Events = events;
            Plc = plc;
            Events.Subscribe(this);
            TestSpecs = new BindableCollection<TestSpecViewModel>();
            Events.Subscribe(this);
        }

        public PortService(IEventAggregator events)
        {
            Events = events;
        }

        virtual public void UpdateDatas()
        {
        }

        public virtual bool Connect()
        {
            if (port.IsOpen)
                port.Close();
            try
            {
                port.PortName = PortName;
                port.Open();
                port.WriteLine("*IDN?");
                port.ReadTimeout = 1000;
                string v = port.ReadLine();
                if (v.Length > 0)
                {
                    if (!string.IsNullOrEmpty(InstName))
                    {
                        IsConnected = v.Contains(InstName);
                    }
                    else
                        IsConnected = true;
                    return IsConnected;
                }
                else
                {
                    port.Close();
                    return false;
                }
            }
            catch (Exception EX)
            {
                Events.Publish(new MsgItem() { Level = "E", Time = DateTime.Now, Value = PortName + ":" + EX.Message });
                port.Close();
                return false;
            }
        }

        public bool Request(string cmd, out string reply)
        {
            Events.Publish(new MsgItem
            {
                Level = "D",
                Time = DateTime.Now,
                Value = $"{PortName}\t{cmd}{Environment.NewLine}"
            });
            if (IsConnected)
            {
                port.WriteLine(cmd);
                try
                {
                    reply = port.ReadLine();
                    Events.Publish(new MsgItem
                    {
                        Level = "D",
                        Time = DateTime.Now,
                        Value = $"{PortName}\t{reply}{Environment.NewLine}"
                    });
                    return true;
                }
                catch (Exception ex)
                {
                    reply = ex.Message;
                    Events.Publish(new MsgItem
                    {
                        Level = "E",
                        Time = DateTime.Now,
                        Value = $"{PortName}\t{reply}{Environment.NewLine}"
                    });
                    return false;
                }
            }
            else
            {
                Events.Publish(new MsgItem
                {
                    Level = "E",
                    Time = DateTime.Now,
                    Value = $"{PortName}\t{" port is not connected"}{Environment.NewLine}"
                });
                reply = "port is not connected";
                return false;
            }
        }

        public void DisConnect()
        {
            IsConnected = false;

            port.Close();
        }

        public virtual void Handle(EventIO message)
        {
        }
        public virtual void Read()
        {
            if (!IsConnected)
            {
                Connect();
            }
        }
    }
    public enum IO_DEF
    {
        空0_0=0,
        空0_1=1,
        通讯建立开始=2,
        通讯建立完成=3,
        OK数据上传开始=4,
        OK数据上传完成=5,
        NG数据上传开始=6,
        NG数据上传完成=7,

        绝缘数据获取开始=8,
        AIP测试完成=9,
        匝间数据2获取开始=10,
        匝间数据3获取开始=11,
        电感数据获取开始=12,
        电阻数据获取开始=13,
        空1_6=14,
        空1_7=15,

        空2_0=16,
        空2_1=17,
        空2_2=18,
        空2_3=19,
        空2_4=20,
        空2_5=21,
        空2_6=22,
        空2_7=23,

        绝缘数据获取完成=24,
        匝间数据1获取完成=25,
        匝间数据2获取完成=25,
        匝间数据3获取完成=27,
        电感数据获取完成=28,
        电阻数据获取完成=29,
        空3_6=30,
        空3_7=31
    }
}
