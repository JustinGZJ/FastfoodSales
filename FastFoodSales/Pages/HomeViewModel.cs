using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StyletIoC;
using Stylet;
using DAQ.Service;

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
        //[Inject]
        //public CameraService CameraService { get; set; }

      
    }
}
