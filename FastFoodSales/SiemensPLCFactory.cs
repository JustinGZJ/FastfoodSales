using HslCommunication.Core;
using HslCommunication.Profinet.Siemens;

namespace DAQ
{
    public class SiemensPLCFactory : IReadWriteFactory
    {
        public IReadWriteNet GetReadWriteNet()
        {
            return new SiemensS7Net(SiemensPLCS.S1500, Properties.Settings.Default.PLC_IP);
        }
        public string AddressA => "M8000";
        public string AddressB => "M8002";
    }
}
