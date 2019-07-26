using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using DAQ.Service;
using Stylet;
using StyletIoC;
using System.Timers;

namespace DAQ.Pages
{
    public class OEEViewModel : PropertyChangedBase
    {
        [Inject]
        public PlcService PLC { get; set; }
        public int Pass { get; set; }
        public int Fail { get; set; }
        public int TotalProduct { get; set; }
        public int Run { get; set; } 
        public int Stop { get; set; }
        public int TotalRun { get; set; }
        Timer timer = new Timer(500);
        public int PlanProduct
        {
            get
            {
                if (PlanCircle > 0)
                {
                    return (int)(Run*1.0  / PlanCircle);
                }
                return 0;
            }
        }
       public OEEViewModel()
        {

            timer.Elapsed += (s, e) =>
            {
                Refresh();
            };
            timer.Start();
        }

        public int PlanCircle { get; set; } = 4;

        public string Availibilty
        {
            get
            {
                if (TotalRun > 0)
                {

                    return (Run*1.0 / TotalRun).ToString("P1");

                }
                else
                    return 0.ToString("P1");
            }
        }
        public Brush AvailibiltyBrush
        {
            get
            {
                if (AvailibiltyValue >0.80)
                    return new SolidColorBrush(Colors.LightSeaGreen);
                else if (AvailibiltyValue > 0.60)
                    return new SolidColorBrush(Colors.Orange);
                else
                    return new SolidColorBrush(Colors.Red);
            }
        }
        public double OEEValue
        {
            get
            {
                return AvailibiltyValue * PerformanceValue * QualityValue;
            }
        }
        public Brush OEEBrush
        {
            get
            {
                if (OEEValue > 0.80)
                    return new SolidColorBrush(Colors.LightSeaGreen);
                else if (OEEValue > 0.60)
                    return new SolidColorBrush(Colors.Orange);
                else
                    return new SolidColorBrush(Colors.Red);
            }
        }
        public string OEE
        {
            get
            {
                return OEEValue.ToString("P1");
            }
        }
        public double AvailibiltyValue
        {
            get
            {
                if (TotalRun > 0)
                {

                    return (Run*1.0 / TotalRun);

                }
                else
                    return 0;
            }
        }

        public string Performance
        {
            get
            {
                if (PlanProduct > 0)
                {
                    return (TotalProduct*1.0 / PlanProduct).ToString("P1");
                }
                else
                    return 0.ToString("P1");
            }
        }

        public double PerformanceValue
        {
            get
            {
                if (PlanProduct > 0)
                {
                    return (TotalProduct*1.0 / PlanProduct);
                }
                else
                    return 0;
            }
        }

        public Brush PerformanceBrush
        {
            get
            {
                if (PerformanceValue > 0.80)
                    return new SolidColorBrush(Colors.LightSeaGreen);
                else if (PerformanceValue > 0.60)
                    return new SolidColorBrush(Colors.Orange);
                else
                    return new SolidColorBrush(Colors.Red);
            }
        }

        public string Quality
        {
            get
            {
                if (TotalProduct > 0)
                {
                    return (Pass*1.0 / TotalProduct).ToString("P");
                }
                else
                    return 0.ToString("P1");
            }
        }

        public double QualityValue
        {
            get
            {
                if (TotalProduct > 0)
                {
                    return (Pass*1.0 / TotalProduct);
                }
                else
                    return 0;
            }
        }

        public Brush QualityBrush
        {
            get
            {
                if (QualityValue > 0.80)
                    return new SolidColorBrush(Colors.LightSeaGreen);
                else if (QualityValue > 0.60)
                    return new SolidColorBrush(Colors.Orange);
                else
                    return new SolidColorBrush(Colors.Red);
            }
        }
    }
}
