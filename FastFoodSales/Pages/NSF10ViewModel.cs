using DAQ.Service;
using LiveCharts;
using Stylet;
using System;
using System.Net;
using LiveCharts.Geared;
using System.Linq;
using System.Collections.Generic;

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

        public event EventHandler<ArcValue> OnArcValue;

        public NSF10ViewModel()
        {
        }

        public NSF10ViewModel(string ip)
        {
            Nsf10 = new Nsf10(IPAddress.Parse(ip));
            Ip = ip;
            Time = DateTime.Now.ToString("HH:mm:ss");

            ChartValuesX = new GearedValues<float>()
            {
            };
            ChartValuesY = new GearedValues<float>();

            Nsf10.OnArcValue += Nsf10_OnArcValue;
        }

        private void Nsf10_OnArcValue(object sender, ArcValue e)
        {
            OnArcValue?.BeginInvoke(this, e, null, null);
            ChartValuesX.Clear();
            ChartValuesY.Clear();
            IEnumerable<(float, float)> values;
            var n = e.XyPoint.Count / 200+1;
            values = Enumerable.Range(0, e.XyPoint.Count - 1).Where(x => x % n == 0).Select(i => e.XyPoint[i]);
            ChartValuesX.AddRange(values.Select(x => x.Item1));
            ChartValuesY.AddRange(values.Select(x => x.Item2));
            XPositivePeak = $"{e.XPositivePeak.Item1:f2},{e.XPositivePeak.Item2:f2}";
            YPositivePeak = $"{e.YPositivePeak.Item1:f2},{e.YPositivePeak.Item2:f2}";
            XNagitivePeak = $"{e.XNagitivePeak.Item1:f2},{e.XNagitivePeak.Item2:f2}";
            YNagitivePeak = $"{e.YNagitivePeak.Item1:f2},{e.YNagitivePeak.Item2:f2}";
            Time = e.time.ToString("HH:mm:ss");
        }

        protected override void OnClose()
        {
         //   Nsf10.OnArcValue -= Nsf10_OnArcValue;
            base.OnClose();
        }

        public GearedValues<float> ChartValuesX { get; set; }

        public GearedValues<float> ChartValuesY { get; set; }
        public Nsf10 Nsf10 { get; set; }
    }
}