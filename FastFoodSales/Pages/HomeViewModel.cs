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
        public PortAService PortAService{get;set;}
        [Inject]
        public PortBService PortBService { get; set; }
        [Inject]
        public MsgViewModel Msg { get; set; }
        [Inject]
        public OEEViewModel OEE { get; set; }
        //[Inject]
        //public CameraService CameraService { get; set; }

      
    }

}
