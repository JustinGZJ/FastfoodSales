using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StyletIoC;
using Stylet;
using DAQ.Service;
using System.Windows.Data;
using System.Globalization;
using DAQ.Pages;
using LiveCharts;
using LiveCharts.Wpf;
using Bogus;

namespace DAQ
{
    public class HomeViewModel : Screen
    {
        [Inject]
        IEventAggregator EventAggregator { get; set; }
        [Inject]
        public RM3545 RM3545 { get; set; }
        [Inject]
        public TH2882A TH2882A { get; set; }
        [Inject]
        public TH9320 TH9320 { get; set; }
        [Inject]
        public TH2775B TH2775B { get; set; }
        [Inject]
        public MsgViewModel Msg { get; set; }
        [Inject]
        public PlcService Plc { get; set; }
        public string[] Labels { get; set; } = new[] { "绝缘", "匝间", "电感", "电阻", "位置度","高度", "通止规","读码"};
        public Func<double, string> Formatter { get; set; } = x => x.ToString();

        public HomeViewModel()
        {
        }
        [Inject] MsgFileSaver<PLC_FINAL_DATA> fileSaver;
        public void Save()
        {
            fileSaver.Process(genData());
            fileSaver.Process(genData());
            fileSaver.Process(genData());
            fileSaver.Process(genData());
            fileSaver.Process(genData());
            fileSaver.Process(genData());
        }

        PLC_FINAL_DATA genData()
        {
            var faker = new Faker<PLC_FINAL_DATA>()
                 .RuleFor(x => x.NG数据结果, m => m.Random.Short(0, 1))
                 .RuleFor(x => x.OK数据结果, m => m.Random.Short(0, 1))
                 .RuleForType(typeof(float), x => x.Random.Float(0, 5000))
                 .RuleForType(typeof(string), x => x.Random.Float().ToString("P"))
                 .RuleFor(x => x.Source, m => "hello");
            return faker.Generate();
               
        }


    }

}
