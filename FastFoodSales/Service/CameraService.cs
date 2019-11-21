using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using SimpleTCP;
using System.Threading;
using Stylet;
using StyletIoC;
using Newtonsoft.Json;
using System.IO;

namespace DAQ.Service
{
    public class CameraData : ISource
    {
        public string Source { get; set; } = "CameraData";
        public float X1 { get; set; }
        public float X2 { get; set; }
        public float Y1 { get; set; }
        public float Y2 { get; set; }
        public string Result { get; set; }

    }
    public class CameraService : PropertyChangedBase, IHandle<EventIO>
    {
        private PlcService _plc;
        private readonly IEventAggregator _eventAggregator;
        MsgFileSaver<CameraData> saver = new MsgFileSaver<CameraData>();

        public CameraService([Inject]IEventAggregator eventAggregator, [Inject] PlcService plc)
        {
            _plc = plc;
            _eventAggregator = eventAggregator;
            _eventAggregator.Subscribe(this);
        }

        public float X1 { get; private set; }
        public float X2 { get; private set; }
        public float Y1 { get; private set; }
        public float Y2 { get; private set; }
        public string Result { get; private set; } = "";

        public void Handle(EventIO message)
        {
            if(message.Value==false)
                return;
            switch (message.Index)
            {
                case (int)IO_DEF.READ_CAM:
                    _plc.WriteBool((int)IO_DEF.READ_CAM, false);
                    X1 = _plc.KvFloats[1].Value;
                    X2 = _plc.KvFloats[3].Value;
                    Y1 = _plc.KvFloats[0].Value;
                    Y2 = _plc.KvFloats[2].Value;
                    Result = _plc.KvFloats[4].Value > 0?"OK":"NG";
                    saver.Process(new CameraData
                    {
                        X1 = X1,
                        X2 = X2,
                        Y1 = Y1,
                        Y2 = Y2,
                        Result = Result
                    });
                    _plc.Pulse((int)IO_DEF.WRITE_CAM, 200);
                    break;
            }
        }
    }
}

