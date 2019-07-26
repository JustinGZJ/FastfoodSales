using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StyletIoC;
using Stylet;
using DAQ.Service;
using System.Windows.Data;
using System.Globalization;
using DAQ.Pages;

namespace DAQ
{
    public class HomeViewModel
    {
        [Inject]
        IEventAggregator EventAggregator { get; set; }
        [Inject]
        public RM3545 RM3545{get;set;}
        [Inject]
        public TH2882A TH2882A { get; set; }
        [Inject]
        public TH9320 TH9320 { get; set; }
        [Inject]
        public TH2775B TH2775B { get; set; }
        [Inject]
        public MsgViewModel Msg { get; set; }
        [Inject]
        public PlcService Plc { get; set; }
        //[Inject]
        //public CameraService CameraService { get; set; }

      
    }

}
