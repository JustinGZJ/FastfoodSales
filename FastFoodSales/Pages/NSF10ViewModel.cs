using DAQ.Service;
using LiveCharts;
using Stylet;
using System;
using System.Net;

namespace DAQ.Pages
{
    public class NSF10ViewModel : Screen
    {
        public string Ip { get; set; }
        public string Time { get; set; } = DateTime.Now.ToString("HH:mm:ss");

        public string XPositivePeak { get; set; }
        public string YPositivePeak { get; set; }
        public string XNagitivePeak { get; set; }
        public string YNagitivePeak { get; set; }
        private Nsf10 nsf10;

        public NSF10ViewModel()
        {
        }

        public NSF10ViewModel(string ip)
        {
            nsf10 = new Nsf10(IPAddress.Parse(ip));
            Ip = ip;
            Time = DateTime.Now.ToString("HH:mm:ss");

            ChartValuesX = new ChartValues<float>();

            nsf10.OnArcValue += Nsf10_OnArcValue;
        }

        private void Nsf10_OnArcValue(object sender, ArcValue e)
        {
            ChartValuesX.Clear();
            ChartValuesY.Clear();
            foreach (var m in e.XyPoint)
            {
                ChartValuesX.Add(m.Item1);
                ChartValuesY.Add(m.Item2);
            }
            XPositivePeak = $"{e.XPositivePeak.Item1:f2},{e.XPositivePeak.Item2:f2}";
            YPositivePeak = $"{e.YPositivePeak.Item1:f2},{e.YPositivePeak.Item2:f2}";
            XNagitivePeak = $"{e.XNagitivePeak.Item1:f2},{e.XNagitivePeak.Item2:f2}";
            YNagitivePeak = $"{e.YNagitivePeak.Item1:f2},{e.YNagitivePeak.Item2:f2}";
            Time = e.time.ToString("HH:mm:ss");
        }

        protected override void OnClose()
        {
            nsf10.OnArcValue -= Nsf10_OnArcValue;
            base.OnClose();
        }

        public ChartValues<float> ChartValuesX { get; set; }

        public ChartValues<float> ChartValuesY { get; set; }
    }
}