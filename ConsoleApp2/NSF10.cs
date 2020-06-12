
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DAQ.Service
{
    public class PeakValue
    {
        public DateTime time { get; set; }

        public byte Mp { get; set; }

        public int ArcNo { get; set; }

        public byte Result { get; set; }

        public (float, float) XPositivePeak { get; set; }
        public (float, float) YPositivePeak { get; set; }

        public (float, float) XNagitivePeak { get; set; }
        public (float, float) YNagitivePeak { get; set; }

        public string PdtId { get; set; }
        public int EONo { get; set; }
        public (float, float) EoTz { get; set; }
    }

    public class ArcValue
    {
        public DateTime time { get; set; }

        public byte Mp { get; set; }

        public int ArcNo { get; set; }
        public string PdtId { get; set; }

        public byte Result { get; set; }
        public (float, float) XPositivePeak { get; set; }
        public (float, float) YPositivePeak { get; set; }
        public (float, float) XNagitivePeak { get; set; }
        public (float, float) YNagitivePeak { get; set; }
        public List<(float, float)> XyPoint { get; set; } = new List<(float, float)>();
    }
    public class Nsf10 : IDisposable
    {
        UdpClient client = new UdpClient();
        CancellationTokenSource cts = new CancellationTokenSource();
        System.Timers.Timer timer = new System.Timers.Timer(5000);
        public Nsf10(IPAddress ip)
        {
            client.Connect(new IPEndPoint(ip, 8810));
            Task.Factory.StartNew(Recieve, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            timer.Elapsed += Timer_Elapsed;
            client.Send(new byte[] { 1, 4, 0, 0 }, 4);
            Thread.Sleep(100);
            client.Send(new byte[] { 1, 7, 0, 0 }, 4);
            timer.Start();

        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                client.Send(new byte[] { 1, 4, 0, 0 }, 4);
                Thread.Sleep(100);
                client.Send(new byte[] { 1, 7, 0, 0 }, 4);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                // throw;
            }
        }

        public void Recieve()
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    var m = client.ReceiveAsync().Result;
                    if (m.Buffer.Length >= 4)
                    {
                        var buf = m.Buffer;
                        var len = (buf[2] << 8) + buf[3];
                        if (buf.Length - 4 < len)
                        {
                            Console.WriteLine("数据长度不符合");
                            continue;
                        }
                        if (buf[0] == 1 && buf[1] == 4)
                        {
                            Console.WriteLine("曲线接收已经打开");
                        }
                        else if (buf[0] == 1 && buf[1] == 7)
                        {
                            Console.WriteLine("最值接收已经打开");
                        }
                        else if (buf[0] == 1 && buf[1] == 5)
                        {
                            Console.WriteLine("收到曲线");

                            var arcValue = new ArcValue()
                            {
                                time = DateTime.ParseExact(buf.Skip(4).Take(7).Select(x => Convert.ToString(x, 16).PadLeft(2, '0'))
                                .Aggregate((s, v) => s + v), "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture),
                                Mp = buf[11],
                                ArcNo = BitConverter.ToInt32(buf, 12),
                                Result = buf[36],
                                XPositivePeak = (BitConverter.ToSingle(buf, 17 + 20), BitConverter.ToSingle(buf, 17 + 20 + 4)),
                                YPositivePeak = (BitConverter.ToSingle(buf, 25 + 20), BitConverter.ToSingle(buf, 25 + 20 + 4)),
                                XNagitivePeak = (BitConverter.ToSingle(buf, 33 + 20), BitConverter.ToSingle(buf, 33 + 20 + 4)),
                                YNagitivePeak = (BitConverter.ToSingle(buf, 41 + 20), BitConverter.ToSingle(buf, 41 + 20 + 4)),
                                PdtId = Encoding.ASCII.GetString(buf.Skip(16).Take(20).TakeWhile(x => x != 0).ToArray()),
                            };
                            for (int i = 49; i < buf.Length-7; i+=8)
                            {
                                arcValue.XyPoint.Add((BitConverter.ToSingle(buf, i), BitConverter.ToSingle(buf, i + 4)));
                            }
                        }
                        else if (buf[0] == 1 && buf[1] == 8)
                        {
                            Console.WriteLine("收到极值");
                            var peakValue = new PeakValue()
                            {
                                time = DateTime.ParseExact(buf.Skip(4).Take(7).Select(x => Convert.ToString(x, 16).PadLeft(2, '0'))
                                    .Aggregate((s, v) => s + v), "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture),
                                Mp = buf[11],
                                ArcNo = BitConverter.ToInt32(buf, 12),
                                Result = buf[16],
                                XPositivePeak = (BitConverter.ToSingle(buf, 17), BitConverter.ToSingle(buf, 17 + 4)),
                                YPositivePeak = (BitConverter.ToSingle(buf, 25), BitConverter.ToSingle(buf, 25 + 4)),
                                XNagitivePeak = (BitConverter.ToSingle(buf, 33), BitConverter.ToSingle(buf, 33 + 4)),
                                YNagitivePeak = (BitConverter.ToSingle(buf, 41), BitConverter.ToSingle(buf, 41 + 4)),
                                PdtId = Encoding.ASCII.GetString(buf.Skip(49).Take(20).TakeWhile(x => x != 0).ToArray()),
                                EONo = buf[69],
                                EoTz = (BitConverter.ToSingle(buf, 70), BitConverter.ToSingle(buf, 70 + 4))
                            };                     
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    //throw;
                }
            }
        }
        public void Close()
        {
            timer.Elapsed -= Timer_Elapsed;
            cts.Cancel();
            client.Close();
        }
        public void Dispose()
        {
            Close();
        }
    }
}
