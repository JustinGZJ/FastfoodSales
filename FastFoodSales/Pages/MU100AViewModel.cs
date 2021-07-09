using SimpleTCP;
using Stylet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Linq;

namespace DAQ.Pages
{
    public class WeildingDto
    {
        public string A { get; set; }
        public string B { get; set; }
        public string C { get; set; }
        public string D { get; set; }
        public string E { get; set; }
        public string F { get; set; }
        public string G { get; set; }
        public string H { get; set; }
        public string I { get; set; }
        public string J { get; set; }
        public string K { get; set; }
        public string L { get; set; }
        public string M { get; set; }
    }

    public class MU100AViewModel : Screen
    {
        private SimpleTcpClient simpleTcpClient1;
        private SimpleTcpClient simpleTcpClient2;
        private readonly IEventAggregator @event;
        IDisposable disposable;

        public BindableCollection<WeildingDto> Source { get; set; } = new BindableCollection<WeildingDto>();
        // public bool MyProperty { get; set; }

        public MU100AViewModel(IEventAggregator @event)
        {
            Connect();
            this.@event = @event;
            disposable = Observable.Interval(TimeSpan.FromSeconds(1)).SubscribeOnDispatcher().Subscribe(x => Connect());
        }

        private bool Connect()
        {
            try
            {
                if (simpleTcpClient1 == null)
                {
                    simpleTcpClient1 = new SimpleTcpClient() { Delimiter = 0x0d };
                    simpleTcpClient1.DelimiterDataReceived += SimpleTcpClient1_DelimiterDataReceived;
                }
                if (simpleTcpClient2 == null)
                {
                    simpleTcpClient2 = new SimpleTcpClient() { Delimiter = 0x0d };
                    simpleTcpClient2.DelimiterDataReceived += SimpleTcpClient2_DelimiterDataReceived;
                }
                  
                if (simpleTcpClient1.TcpClient == null || !simpleTcpClient1.TcpClient.Connected)
                    simpleTcpClient1.Connect("192.168.0.38", 5000);
                if (simpleTcpClient2.TcpClient == null || !simpleTcpClient2.TcpClient.Connected)
                    simpleTcpClient2.Connect("192.168.0.39", 5000);
                IsConnected1 = simpleTcpClient1.TcpClient.Connected;
                IsConnected2 = simpleTcpClient2.TcpClient.Connected;
                return true;
            }
            catch (Exception ex)
            {
                @event.PublishError("焊机连接异常", ex.Message);
                return false;
            }
        }
        public bool IsConnected1 { get; set; }
        public bool IsConnected2 { get; set; }
        private void SimpleTcpClient2_DelimiterDataReceived(object sender, Message e)
        {
            var msg = e.MessageString.Trim('\n');
            @event.PublishMsg("MU100A_1",msg);
            ProcessMessage("MU100A_1", msg);
        }

        private void SimpleTcpClient1_DelimiterDataReceived(object sender, Message e)
        {
            var msg = e.MessageString.Trim('\n');
            @event.PublishMsg("MU100A_2", msg);
            ProcessMessage("MU100A_2", msg);
        }
        //!01:001,00006,122.7,N,121.3,N,+01.254,N,+01.018,N,-00.236,N,---,-----,-----, ,-----, ,---.---, ,---.---, ,---.---, ,**
        private void ProcessMessage(string source, string msg)
        {
            if (!msg.StartsWith("!"))
                return;
            string[] headers = new string[] {
                "设备地址编号",
                 "计划编号",
                 "计数器",
                 "焊接前焊接压力",
                 "焊接前焊接压力判定",
                 "焊接后焊接压力",
                 "焊接后焊接压力判定",
                 "焊接前工件厚度",
                 "焊接前工件厚度判定",
                 "焊接后工件厚度",
                 "焊接后工件厚度判定",
                 "位移量",
                 "位移量判定" };
            var splits = msg.Replace("N", "PASS")
                .Replace("H", "HIGH")
                .Replace("L", "LOW").Substring(1).Split(',', ':', '!');
            if (splits.Length >= headers.Length)
            {
                var dict = new Dictionary<string, string>();

                for(int i=0;i<headers.Length;i++)
                {
                    dict[headers[i]] = splits[i];
                }
                //  WeildingDto
                var dto = new WeildingDto();
                var properties = dto.GetType().GetProperties();
                for(int i=0;i<properties.Length;i++)
                {
                    properties[i].SetValue(dto, splits[i]);
                }
                var fileName = Path.Combine("../DaqData", DateTime.Today.ToString("yyyyMMdd"), source+".csv");
                Source.Add(dto);
                SaveFile(fileName, dict);
            }
        }

        public static bool SaveFile(string fileName, Dictionary<string, string> dictionary)
        {
            try
            {
                var dir = Path.GetDirectoryName(fileName);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                if (!File.Exists(fileName))
                {
                    var header = string.Join(",", dictionary.Keys).Trim(',') + Environment.NewLine;
                    var content = string.Join(",", dictionary.Values).Trim(',') + Environment.NewLine;
                    File.AppendAllText(fileName, header + content);
                }
                else
                {
                    var content = string.Join(",", dictionary.Values).Trim(',');
                    File.AppendAllText(fileName, content + Environment.NewLine);
                }
                return true;
            }
            catch (Exception)
            {
                ;
                return false;
            }
        }

        protected override void OnClose()
        {
            //   disposable.Dispose();
            base.OnClose();
        }
    }
}