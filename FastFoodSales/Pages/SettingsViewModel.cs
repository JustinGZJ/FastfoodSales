using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using Stylet;
using StyletIoC;
using DAQ.Service;
using HslCommunication.Profinet.Omron;
using DAQ.Pages;

namespace DAQ
{
    public class SettingsViewModel : Screen
    {
        [Inject]
        public IEventAggregator Events { get; set; }
        //[Inject]
        //public RM3545 PortServiceA { get; set; }
        //[Inject]
        //public TH2882A PortServiceB { get; set; }
        [Inject]
        public NSF10ViewModel nsf { get; set; }

        [Inject] public IReadWriteFactory ReadWriteFactory { get; set; }

        public int PLCValue { get; set; }

        public string PLCAddress { get; set; }

        public string ErrorMessage { get; set; }

        public  void WriteInt()
        {
            var rw = ReadWriteFactory.GetReadWriteNet();
            var result = rw.Write(PLCAddress, PLCValue);
            if(result.IsSuccess)
            {
                ErrorMessage = "Success!";
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }

        [Inject]
        public PlcService plc { get; set; }

        public string[] Ports { get { return SerialPort.GetPortNames(); } }

        public string[] PortACMDs { get { return new string[] { "*IDN?", "TRIG?", "SCAN:DATA?", "READ?", "FETCH?" }; } }
        public string[] PortBCMDs { get { return new string[] { "*IDN?", "TRIG?", "FETCh?" }; } }

        public SettingsViewModel()
        {
        }
        protected override void OnInitialActivate()
        {

            base.OnInitialActivate();

        }
        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            Properties.Settings.Default.Save();
        }
        //public string PortA
        //{
        //    get { return Properties.Settings.Default.PORT_RM3545; }
        //    set
        //    {
        //        Properties.Settings.Default.PORT_RM3545 = value;
        //        PortServiceA.Connect();
        //    }
        //}
        //public string PortB
        //{
        //    get { return Properties.Settings.Default.PORT_TH2883S4; }
        //    set
        //    {
        //        Properties.Settings.Default.PORT_TH2883S4 = value;
        //        PortServiceB.Connect();
        //    }
        //}
        public string PLC_IP
        {
            get { return Properties.Settings.Default.PLC_IP; }
            set { Properties.Settings.Default.PLC_IP = value; }
        }

        public string PortABuffer { get; set; }
        public string PortBBuffer { get; set; }
        public int PLC_Port
        {
            get { return Properties.Settings.Default.PLC_PORT; }
            set { Properties.Settings.Default.PLC_PORT = value; }
        }

        //public void QueryA(string Cmd)
        //{
        //    PortABuffer = $"Send:\t{Cmd}{Environment.NewLine}";
        //    bool r = PortServiceA.Request(Cmd, out string replay);
        //    if (r)
        //    {
        //        PortABuffer += $"Recieve:\t{replay}{Environment.NewLine}";
        //    }
        //    else
        //    {
        //        PortABuffer += $"error:\t{replay}{Environment.NewLine}";
        //    }
        //}
        //public void QueryB(string Cmd)
        //{
        //    PortBBuffer = $"Send:\t{Cmd}{Environment.NewLine}";
        //    bool r = PortServiceB.Request(Cmd, out string replay);
        //    if (r)
        //    {
        //        PortBBuffer += $"Recieve:\t{replay}{Environment.NewLine}";
        //    }
        //    else
        //    {
        //        PortBBuffer += $"error:\t{replay}{Environment.NewLine}";
        //    }
        //}
        bool v = false;
        public void SetBit()
        {

            v = !v;
            plc.WriteBool(0, v);

        }

        bool v1 = false;
        public void SetBit1()
        {

            v1 = !v1;
            plc.WriteBool(1, v1);
        }

  


        public string CameraIP
        {
            get { return Properties.Settings.Default.CAMERA_IP; }
            set { Properties.Settings.Default.CAMERA_IP = value; }
        }

        public int CameraPort
        {
            get { return Properties.Settings.Default.CAMERA_PORT; }
            set { Properties.Settings.Default.CAMERA_PORT = value; }
        }


    }
}
