using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stylet;
namespace DAQ.Service
{
    public class PortAService : PortService
    {
        public override string PortName => Properties.Settings.Default.PORT_A;
    }
}
