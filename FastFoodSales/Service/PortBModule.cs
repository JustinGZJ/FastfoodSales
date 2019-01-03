using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAQ.Service
{
    public class PortBService:PortService
    {
        public override string PortName => Properties.Settings.Default.PORT_B;
       public PortBService()
        {

        }
    }

  public  class PortService : IPortService
    {
        SerialPort port = new SerialPort();
        public virtual string PortName { get; }
        public bool IsConnected { get; set; }
        public bool Connect()
        {
            if (port.IsOpen)
                port.Close();
            try
            {
                port.PortName = PortName;
                port.Open();
                port.WriteLine("*IDN?");
                string v = port.ReadLine();
                if (v.Length > 0)
                {
                    IsConnected = true;
                    return true;
                }                 
                else
                {
                    port.Close();
                    return false;
                }       
            }
            catch (Exception EX)
            {
                port.Close();
                return false;
            }
        }

        public bool Request(string cmd, out string reply)
        {
            if (IsConnected)
            {
                port.WriteLine(cmd);
                try
                {
                    reply = port.ReadLine();
                    return true;
                }
                catch (Exception ex)
                {
                    reply = ex.Message;
                    return false;
                }
            }
            else
            {
                reply = "port is not connected";
                return false;
            }
        }

        public void DisConnect()
        {
            IsConnected = false;
            port.Close();
        }
    }
}
