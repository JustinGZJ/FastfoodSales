using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Profinet.Siemens;
using System.Threading.Tasks;
using HslCommunication;
using HslCommunication.Core;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
           IReadWriteNet siemens = new SiemensS7Net(SiemensPLCS.S1500, "192.168.0.1");
            siemens.Write("M8000", (ushort)99);
            var result = siemens.ReadUInt16("M8000");
            
        }
    }
}
