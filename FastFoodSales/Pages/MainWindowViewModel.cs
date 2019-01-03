using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stylet;
using StyletIoC;
namespace DAQ
{
    public class MainWindowViewModel : Conductor<object>
    {
        int index = 0;
        [Inject]
        public HomeViewModel Home { get; set; }
        [Inject]
        public SettingsViewModel Setting { get; set; }
        public int Index
        {
            get { return index; }
            set
            {
                index = value;
                
                switch (index)
                {
                    case 0:
                        ActivateItem(Home);
                        break;
                    case 1:
                        ActivateItem(new UserControl1ViewModel());
                        break;
                }

            }
        }

        public void ShowSetting()
        {
           ActivateItem(Setting);        
        }
    }
}
