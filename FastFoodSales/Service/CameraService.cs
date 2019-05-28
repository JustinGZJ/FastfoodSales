using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using SimpleTCP;
using System.Threading;
using Stylet;
using StyletIoC;
using Newtonsoft.Json;
using System.IO;

namespace DAQ.Service
{
    public class CameraService : PropertyChangedBase
    {
        private const string Path = "camera.txt";
        SimpleTcpClient Client = null;
        public CameraService()
        {
            LoadData();
            StartTxConnect();
        }
        [Inject]
        IEventAggregator Events;
        [Inject]
        PlcService PLC;
        [Inject]
        MsgDBSaver DBSaver;
        [Inject]
        MsgFileSaver<TestSpecViewModel> FileSaver;
        public void StartTxConnect()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    Connect();
                    Thread.Sleep(2000);
                }
            });
        }
        ~CameraService()
        {
            SaveData();
        }

        public void LoadData()
        {
            try
            {
                if (File.Exists(Path))
                {
                    var str = File.ReadAllText(Path);
                    var obj = JsonConvert.DeserializeObject(str) as List<BindableCollection<TestSpecViewModel>>;
                    if (obj?.Count == 3)
                    {
                        C1Values = obj[0];
                        C2Values = obj[1];
                        C3Values = obj[2];
                    }
                }
            }
            catch (Exception ex)
            {
                Events.Publish(new MsgItem { Level = "E", Time = DateTime.Now, Value = ex.Message });
                //  throw;
            }
        }
        public void SaveData()
        {
            var vs = new List<BindableCollection<TestSpecViewModel>>()
            {
                C1Values,C2Values,C3Values
            };
            string json = JsonConvert.SerializeObject(vs);
            File.WriteAllText(Path, json);
        }

        public bool IsConnected
        {
            get
            {
                return Client?.TcpClient.Connected == true;
            }
        }

        public BindableCollection<TestSpecViewModel> C1Values { get; set; } 
                = new BindableCollection<TestSpecViewModel>();
        public BindableCollection<TestSpecViewModel> C2Values { get; set; } 
                = new BindableCollection<TestSpecViewModel>();
        public BindableCollection<TestSpecViewModel> C3Values { get; set; }
                = new BindableCollection<TestSpecViewModel>();
        public void Connect()
        {
            try
            {

                var r = Client?.TcpClient.Connected;
                if (r != true)
                {

                    if (Client != null)
                    {
                        Client.Disconnect();
                        Client.Dispose();
                    }
                    Client = new SimpleTcpClient()
                        .Connect(Properties.Settings.Default.CAMERA_IP,
                        Properties.Settings.Default.CAMERA_PORT);
                }
            }
            catch (SocketException ex)
            {
                Events.Publish(new MsgItem
                {
                    Level = "E",
                    Time = DateTime.Now,
                    Value = ex.Message
                });
            }
        }
        private void Client_DelimiterDataReceived(object sender, Message e)
        {
            try
            {
                var str = e.MessageString;
                Events.Publish(new MsgItem
                {
                    Level = "D",
                    Time = DateTime.Now,
                    Value = str
                });
                e.ReplyLine("ok");
                ParseDatas(str);
            }
            catch (Exception ex)
            {
                Events.Publish(new MsgItem
                {
                    Level = "E",
                    Time = DateTime.Now,
                    Value = ex.Message
                });
                //  throw;
            }
        }

        public void ParseDatas(string Msg)
        {

            var Itesms = Msg.Split(':');
            if (Itesms.Length > 1)
            {
                if (Itesms[0] == "C1")
                {
                    var specs = GetTestSpecs(Itesms[0], Itesms[1]);
                    C1Values.Clear();
                    C1Values.AddRange(specs);
                    foreach (var s in specs)
                    {
                        FileSaver.Process(s);
                        DBSaver.Process(s);
                    }
                }
                else if (Itesms[0] == "C2")
                {

                    var specs = GetTestSpecs(Itesms[0], Itesms[1]);
                    C2Values.Clear();
                    C2Values.AddRange(specs);
                    foreach (var s in specs)
                    {
                        //      FileSaver.Process(s);
                        DBSaver.Process(s);
                    }
                }
                else if (Itesms[0] == "C3")
                {

                    var specs = GetTestSpecs(Itesms[0], Itesms[1]);
                    C3Values.Clear();
                    C3Values.AddRange(specs);
                    foreach (var s in specs)
                    {
                        //         FileSaver.Process(s);
                        DBSaver.Process(s);
                    }
                }
            }
        }

        public List<TestSpecViewModel> GetTestSpecs(string source, string content)
        {
            var specs = new List<TestSpecViewModel>();

            var Subitems = content.Split(';');
            foreach (var Subitem in Subitems)
            {
                var vals = Subitem.Split(',');
                if (vals.Length == 5)
                {
                    bool[] pr = new bool[3];
                    pr[0] = float.TryParse(vals[1], out float upper);
                    pr[1] = float.TryParse(vals[2], out float lower);
                    pr[2] = float.TryParse(vals[3], out float value);
                    if (pr.Any(x => x == false))
                    {
                        Events.Publish(new MsgItem() { Level = "D", Time = DateTime.Now, Value = $"Camera date parse error:{Subitem}" });
                        return specs;
                    }
                    specs.Add(
                                new TestSpecViewModel()
                                {
                                    Name = vals[0],
                                    Upper = upper,
                                    Lower = lower,
                                    Value = value,
                                    Result = vals[4].ToUpper().Contains("PASS") ? 1 : -1,
                                    Source = source
                                });

                }
            }
            return specs;
        }
    }
}

