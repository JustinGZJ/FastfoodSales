using DAQ.Service;
using Stylet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;

namespace DAQ.Pages
{
    public class NsfCollectionViewModel : Screen
    {
        public ObservableCollection<NSF10ViewModel> ViewModels { get; set; } = new BindableCollection<NSF10ViewModel>();

        private List<IObservable<ArcValue>> observables = new List<IObservable<ArcValue>>();

        public NsfCollectionViewModel()
        {
            ViewModels.Add(new NSF10ViewModel("192.168.0.51"));
            ViewModels.Add(new NSF10ViewModel("192.168.0.52"));
            ViewModels.Add(new NSF10ViewModel("192.168.0.53"));
            ViewModels.Add(new NSF10ViewModel("192.168.0.54"));
            ViewModels.Add(new NSF10ViewModel("192.168.0.55"));
            ViewModels.Add(new NSF10ViewModel("192.168.0.56"));
            foreach (var vm in ViewModels)
            {
                var sub = Observable.FromEventPattern<ArcValue>(
                      x => vm.OnArcValue += x,
                      x => vm.OnArcValue -= x
                        ).Select(m => m.EventArgs);
                observables.Add(sub);
            }
            observables[0].Zip(observables[1], observables[2], (x, y, z) => (x, y, z)).Subscribe((m) =>
            {
                string fileName = Path.Combine("../DaqData", DateTime.Today.ToString("yyyyMMdd"), "Presure01" + ".csv");
                var dictionary = new Dictionary<string, string>();
                dictionary["日期"] = DateTime.Now.ToString("yyyy-MM-dd");
                dictionary["时间"] = DateTime.Now.ToString("HH:mm:ss");
                dictionary["PIN1 X正峰值X"] = m.x.XPositivePeak.Item1.ToString("f2");
                dictionary["PIN1 X正峰值Y"] = m.x.XPositivePeak.Item2.ToString("f2");
                dictionary["PIN1 Y正峰值X"] = m.x.YPositivePeak.Item1.ToString("f2");
                dictionary["PIN1 Y正峰值Y"] = m.x.YPositivePeak.Item2.ToString("f2");
                dictionary["PIN1 X负峰值X"] = m.x.XNagitivePeak.Item1.ToString("f2");
                dictionary["PIN1 X负峰值Y"] = m.x.XNagitivePeak.Item2.ToString("f2");
                dictionary["PIN1 Y负峰值X"] = m.x.YNagitivePeak.Item1.ToString("f2");
                dictionary["PIN1 Y负峰值Y"] = m.x.YNagitivePeak.Item2.ToString("f2");

                dictionary["PIN2 X正峰值X"] = m.y.XPositivePeak.Item1.ToString("f2");
                dictionary["PIN2 X正峰值Y"] = m.y.XPositivePeak.Item2.ToString("f2");
                dictionary["PIN2 Y正峰值X"] = m.y.YPositivePeak.Item1.ToString("f2");
                dictionary["PIN2 Y正峰值Y"] = m.y.YPositivePeak.Item2.ToString("f2");
                dictionary["PIN2 X负峰值X"] = m.y.XNagitivePeak.Item1.ToString("f2");
                dictionary["PIN2 X负峰值Y"] = m.y.XNagitivePeak.Item2.ToString("f2");
                dictionary["PIN2 Y负峰值X"] = m.y.YNagitivePeak.Item1.ToString("f2");
                dictionary["PIN2 Y负峰值Y"] = m.y.YNagitivePeak.Item2.ToString("f2");

                dictionary["PIN3 X正峰值X"] = m.z.XPositivePeak.Item1.ToString("f2");
                dictionary["PIN3 X正峰值Y"] = m.z.XPositivePeak.Item2.ToString("f2");
                dictionary["PIN3 Y正峰值X"] = m.z.YPositivePeak.Item1.ToString("f2");
                dictionary["PIN3 Y正峰值Y"] = m.z.YPositivePeak.Item2.ToString("f2");
                dictionary["PIN3 X负峰值X"] = m.z.XNagitivePeak.Item1.ToString("f2");
                dictionary["PIN3 X负峰值Y"] = m.z.XNagitivePeak.Item2.ToString("f2");
                dictionary["PIN3 Y负峰值X"] = m.z.YNagitivePeak.Item1.ToString("f2");
                dictionary["PIN3 Y负峰值Y"] = m.z.YNagitivePeak.Item2.ToString("f2");
                Utils.SaveFile(fileName, dictionary);
            });

            observables[3].Zip(observables[4], observables[5], (x, y, z) => (x, y, z)).Subscribe((m) =>
            {
                string fileName = Path.Combine("../DaqData", DateTime.Today.ToString("yyyyMMdd"), "Presure02" + ".csv");
                var dictionary = new Dictionary<string, string>();
                dictionary["日期"] = DateTime.Now.ToString("yyyy-MM-dd");
                dictionary["时间"] = DateTime.Now.ToString("HH:mm:ss");
                dictionary["PIN1 X正峰值X"] = m.x.XPositivePeak.Item1.ToString("f2");
                dictionary["PIN1 X正峰值Y"] = m.x.XPositivePeak.Item2.ToString("f2");
                dictionary["PIN1 Y正峰值X"] = m.x.YPositivePeak.Item1.ToString("f2");
                dictionary["PIN1 Y正峰值Y"] = m.x.YPositivePeak.Item2.ToString("f2");
                dictionary["PIN1 X负峰值X"] = m.x.XNagitivePeak.Item1.ToString("f2");
                dictionary["PIN1 X负峰值Y"] = m.x.XNagitivePeak.Item2.ToString("f2");
                dictionary["PIN1 Y负峰值X"] = m.x.YNagitivePeak.Item1.ToString("f2");
                dictionary["PIN1 Y负峰值Y"] = m.x.YNagitivePeak.Item2.ToString("f2");

                dictionary["PIN2 X正峰值X"] = m.y.XPositivePeak.Item1.ToString("f2");
                dictionary["PIN2 X正峰值Y"] = m.y.XPositivePeak.Item2.ToString("f2");
                dictionary["PIN2 Y正峰值X"] = m.y.YPositivePeak.Item1.ToString("f2");
                dictionary["PIN2 Y正峰值Y"] = m.y.YPositivePeak.Item2.ToString("f2");
                dictionary["PIN2 X负峰值X"] = m.y.XNagitivePeak.Item1.ToString("f2");
                dictionary["PIN2 X负峰值Y"] = m.y.XNagitivePeak.Item2.ToString("f2");
                dictionary["PIN2 Y负峰值X"] = m.y.YNagitivePeak.Item1.ToString("f2");
                dictionary["PIN2 Y负峰值Y"] = m.y.YNagitivePeak.Item2.ToString("f2");

                dictionary["PIN3 X正峰值X"] = m.z.XPositivePeak.Item1.ToString("f2");
                dictionary["PIN3 X正峰值Y"] = m.z.XPositivePeak.Item2.ToString("f2");
                dictionary["PIN3 Y正峰值X"] = m.z.YPositivePeak.Item1.ToString("f2");
                dictionary["PIN3 Y正峰值Y"] = m.z.YPositivePeak.Item2.ToString("f2");
                dictionary["PIN3 X负峰值X"] = m.z.XNagitivePeak.Item1.ToString("f2");
                dictionary["PIN3 X负峰值Y"] = m.z.XNagitivePeak.Item2.ToString("f2");
                dictionary["PIN3 Y负峰值X"] = m.z.YNagitivePeak.Item1.ToString("f2");
                dictionary["PIN3 Y负峰值Y"] = m.z.YNagitivePeak.Item2.ToString("f2");
                Utils.SaveFile(fileName, dictionary);
            });
        }

    }
}