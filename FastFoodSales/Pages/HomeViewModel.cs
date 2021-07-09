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
        public MsgViewModel Msg { get; set; }
        [Inject]
        public PlcService Plc { get; set; }
        [Inject]
        public AIP AIP { get; set; }
        public string[] Labels { get; set; } = new[] { "绝缘", "匝间", "电感", "电阻", "位置度","高度", "通止规","读码"};
        public Func<double, string> Formatter { get; set; } = x => x.ToString();

        public HomeViewModel()
        {
        }



    }

}
