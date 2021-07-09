using HslCommunication.Core;
using HslCommunication.Profinet.Omron;

namespace DAQ
{
    public class OmronPLCFactory : IReadWriteFactory
    {
        public IReadWriteNet GetReadWriteNet()
        {
            return new OmronFinsNet(Properties.Settings.Default.PLC_IP, Properties.Settings.Default.PLC_PORT)
            {
                SA1 = 0,
                DA1 = 0
            };
        }
        public string AddressA => "D8000";
        public string AddressB => "D8002";
    }
}
