using Stylet;
using StyletIoC;

namespace DAQ
{
    public class SettingsViewModel : Screen
    {
        [Inject]
        public IEventAggregator Events { get; set; }


        public int PLCValue { get; set; }

        public string PLCAddress { get; set; }

        public string ErrorMessage { get; set; }




        public SettingsViewModel()
        {
        }
        protected override void OnInitialActivate()
        {

            base.OnInitialActivate();

        }
        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            Properties.Settings.Default.Save();
        }
 
        public string PLC_IP
        {
            get { return Properties.Settings.Default.PLC_IP; }
            set { Properties.Settings.Default.PLC_IP = value; }
        }

        public int PLC_Port
        {
            get { return Properties.Settings.Default.PLC_PORT; }
            set { Properties.Settings.Default.PLC_PORT = value; }
        }



    }
}
