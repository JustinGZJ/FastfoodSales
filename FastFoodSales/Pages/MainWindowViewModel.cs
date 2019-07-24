using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stylet;
using StyletIoC;
using DAQ.Pages;
using DAQ.Service;

namespace DAQ
{
    public class MainWindowViewModel : Conductor<object>, IHandle<AlarmItem>
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
        public CameraViewModel Camera { get; set; }

    public bool IsDialogOpen { get { return AlarmList.Count > 0; } }

        public BindableCollection<AlarmItem> AlarmList { get; set; } = new BindableCollection<AlarmItem>();

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
                        ActivateItem(Msg);
                        break;
                    case 2:
                        ActivateItem(new AboutViewModel());
                        break;
                    case 3:
                        ActivateItem(Camera);
                        break;
                }

            }
        }

        protected override void OnActivate()
        {
            Events.Subscribe(this);
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

        public void Handle(AlarmItem message)
        {
            if (!message.Value)
            {
                if(AlarmList.Any(x=>x.Address==message.Address))
                {
                    var a = AlarmList.Where(x => x.Address == message.Address);
                    foreach(var v in a)
                    {
                        AlarmList.Remove(v);
                    }
                }               
            }
            else
            {
                AlarmList.Add(message);
            }
        }
    }
}
