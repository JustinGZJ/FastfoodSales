using Stylet;
using StyletIoC;
using DAQ.Pages;

namespace DAQ
{
    public class MainWindowViewModel : Conductor<object>
    {
        int index = 0;
        [Inject]
        public IEventAggregator Events { get; set; }
        [Inject]
        public HomeViewModel Home { get; set; }
        [Inject]
        public SettingsViewModel Setting { get; set; }
        [Inject]
        public MsgViewModel Msg { get; set; }
        [Inject]
        public PLCViewModel PLC { get; set; }
        [Inject]
        public NsfCollectionViewModel NsfCollection { get; set; }
        [Inject]
        public MU100AViewModel MU100A { get; set; }



        public object CurrentPage { get; set; }
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
                        ActivateItem(NsfCollection);
                        break;
                    case 2:
                        ActivateItem(MU100A);
                        break;
                    case 3:
                        ActivateItem(Msg);

                        break;
                    case 4:
                        ActivateItem(new AboutViewModel());
                        //      ActivateItem(Camera);
                        break;
                }

            }
        }

        protected override void OnActivate()
        {

            base.OnActivate();
        }
        protected override void OnInitialActivate()
        {
            ActivateItem(Home);
            ActiveValues();
            base.OnInitialActivate();
        }

        public void ShowSetting()
        {
            ActivateItem(Setting);
        }

        public void ActiveValues()
        {
            CurrentPage = PLC;
        }
        public void ActiveMessages()
        {
            CurrentPage = Msg;
        }

    }
}
