using StyletIoC;
using Stylet;
using DAQ.Pages;

namespace DAQ
{
    public class HomeViewModel
    {
        [Inject]
        IEventAggregator EventAggregator { get; set; }
        [Inject]
        public MsgViewModel Msg { get; set; }


     
    }

}
