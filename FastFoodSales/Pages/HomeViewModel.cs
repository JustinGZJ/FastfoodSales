using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StyletIoC;
using Stylet;

namespace DAQ
{
  public  class HomeViewModel
    {
       [Inject]
       IEventAggregator EventAggregator { get; set; }

        public void Send()
        {
            EventAggregator.Publish("hello");
        }
    }
}
