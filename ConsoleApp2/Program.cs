
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Timer = System.Timers.Timer;
using System.Buffers.Binary;
using System.ComponentModel.DataAnnotations;
using DAQ.Service;

namespace ConsoleApp2
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            // Console.WriteLine(PercentToFloat("50%"));
            var m1 = new Nsf10(IPAddress.Parse("192.168.0.51"));
            var m2 = new Nsf10(IPAddress.Parse("192.168.0.52"));
            var m3 = new Nsf10(IPAddress.Parse("192.168.0.53"));
            Console.ReadLine();
        }
        public static float PercentToFloat(string value)
        {
            return float.TryParse(value.TrimEnd('%'), out float v) ? v / 100f : 0f;
        }
    }

  
}
