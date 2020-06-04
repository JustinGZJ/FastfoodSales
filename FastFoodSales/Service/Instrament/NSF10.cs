using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows.Converters;

namespace DAQ.Service.Instrament
{
    public class NSF10 : IDisposable
    {
        private readonly string remoteIp;
        private UdpClient _client;
        private Dictionary<IPEndPoint, MemoryStream> buffer;

        public NSF10()
        {
            _client = new UdpClient();
        }

        public void Dispose()
        {

        }

        public IObservable<UdpReceiveResult> Received { get; private set; }

        public void Start()
        {
            Stop();
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse("192.168.0.255"), 8810);//初始化一个发送广播和指定端口的网络端口实例
            byte[] sendbuf = new byte[] { 1, 7, 0, 0 };
            _client.Send(sendbuf, sendbuf.Length, ep);
            Received = Observable.Defer(() => _client.ReceiveAsync().ToObservable()).Repeat();
        }

        public void Stop()
        {
            _client?.Close();
            buffer.Clear();
        }
    }
}