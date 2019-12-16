using System.Collections.Generic;

namespace DAQ
{
    public class PlcParas
    {
        public int Port { get; set; } = 5000;
        public string Ip { get; set; } = "127.0.0.1";
        public List<ServoLocation> ServoLocations { get; set; }
        public string TriggerAddress { get; set; } = "M30000";
    }
}
