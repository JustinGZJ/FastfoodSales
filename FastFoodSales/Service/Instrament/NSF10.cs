using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Buffers;

namespace DAQ.Service.Instrament
{
    public class NSF10 : IDisposable
    {
        private UdpClient _client;
        ConcurrentDictionary<string, Pipe> Pipes = new ConcurrentDictionary<string, Pipe>();

        public NSF10()
        {

        }

        public void Dispose()
        {

        }
        public void Start()
        {
            _client = new UdpClient();
            byte[] sendbuf = new byte[] { 1, 7, 0, 0 };
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse("192.168.0.255"), 8810);//初始化一个发送广播和指定端口的网络端口实例
            _client.Send(sendbuf, sendbuf.Length, ep);
            sendbuf = new byte[] { 1, 4, 0, 0 };
            _client.Send(sendbuf, sendbuf.Length, ep);
            Task.Factory.StartNew(ListenAsync, TaskCreationOptions.LongRunning);
        }

        private async Task ListenAsync()
        {
            while (true)
            {
                var m = await _client.ReceiveAsync();
                if (Pipes[m.RemoteEndPoint.Address.ToString()] == null)
                {
                    Pipes[m.RemoteEndPoint.Address.ToString()] = new Pipe();
                }
                await Pipes[m.RemoteEndPoint.Address.ToString()].Writer.WriteAsync(m.Buffer);
                Pipes[m.RemoteEndPoint.Address.ToString()].Writer.Advance(m.Buffer.Length);
                await Pipes[m.RemoteEndPoint.Address.ToString()].Writer.FlushAsync();
            }
        }

        public async Task ParseAsync()
        {
            Pipes.AsParallel().ForAll(
            async (x) =>
            {
                while (true)
                {
                    var m = await x.Value.Reader.ReadAsync();
                    var buffer = m.Buffer;
                    if (buffer.Length < 4)
                        continue;
                    SequencePosition? pos = null;
                    do
                    {
                        pos = buffer.PositionOf((byte)1);
                        if (pos != null)
                        {
                            var headtoCheck = buffer.Slice(pos.Value, 4).ToArray();
                            //SequenceEqual需要引用System.Linq
                            if (headtoCheck.SequenceEqual(new byte[] { 1, 4, 0, 0 }))
                            {
                                Console.WriteLine("启动接收曲线");
                            }
                            else if (headtoCheck.SequenceEqual(new byte[] { 1, 7, 0, 0 }))
                            {
                                Console.WriteLine("启动接收极限值");
                            }
                            else if (headtoCheck.Take(2).ToArray().SequenceEqual(new byte[] { 1, 8 }))
                            {
                                Console.WriteLine("收到极限值");
                                var len = BitConverter.ToUInt16(headtoCheck, 2);
                                if (buffer.Slice(pos.Value).Length >= len)
                                {
                                    buffer.Slice(pos.Value, len);
                                    //deal with msg

                                    //跳过处理过的信息
                                    var next = buffer.GetPosition(len, pos.Value);
                                    buffer.Slice(next);
                                }
                                else
                                    break;
                            }
                            else if (headtoCheck.Take(2).ToArray().SequenceEqual(new byte[] { 1, 5 }))
                            {
                                Console.WriteLine("收到曲线");

                                var len = BitConverter.ToUInt16(headtoCheck, 2);
                                if (buffer.Slice(pos.Value).Length >= len)
                                {
                                   var sequence =buffer.Slice(pos.Value, len);
                                    //deal with msg

                                    //跳过处理过的信息
                                    var next = buffer.GetPosition(len, pos.Value);
                                    buffer.Slice(next);
                                }
                                else
                                    break;
                            }
                            else
                            {
                                //第一个是0x75但是后面不匹配，可能有数据传输问题，那么需要舍弃第一个，0x75后面的字节开始再重新找0x75
                                var next = buffer.GetPosition(1, pos.Value);
                                buffer = buffer.Slice(next);
                            }
                        } while (pos != null) ;
                        x.Value.Reader.AdvanceTo(buffer.Start, buffer.End);
                        if(m.IsCompleted)
                        {
                            break;
                        }
                    } while (pos!=null);
                 }
            }
            );
        }

        public void Stop()
        {
            _client?.Close();
            //   buffer.Clear();
        }
    }

    public class NSF10Unit
    {
        public string Address { get; set; }

        //      BlockingCollection<by>


    }

    public class PeakFrame
    {
        public DateTime Time { get; set; }
        public int MP { get; set; }

        public int ArcNo { get; set; }
        public int XPositive { get; set; }
        public int YPositive { get; set; }
        public int XNagitive { get; set; }
        public int YNagitive { get; set; }
        public string ProductId { get; set; }

        public int EONo { get; set; }
        public int EigenValue { get; set; }
    }

}
