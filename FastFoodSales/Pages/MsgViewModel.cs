using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stylet;
using StyletIoC;


namespace DAQ.Pages
{
    public class MsgViewModel:IHandle<MsgItem>
    {
        IEventAggregator @events;
        public MsgViewModel(IEventAggregator @event)
        {
            @events = @event;
            events.Subscribe(this);
        }
        public BindableCollection<MsgItem> Items { get; set; } = new BindableCollection<MsgItem>() {
        };

        public void Handle(MsgItem message)
        {
            Items.Insert(0,message);
            if(Items.Count>20)
            {
                Items.RemoveAt(Items.Count-1);
            }
        }
    }

}

