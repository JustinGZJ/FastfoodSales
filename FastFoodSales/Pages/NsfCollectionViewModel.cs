using Stylet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAQ.Pages
{
    public class NsfCollectionViewModel:Screen
    {
        public ObservableCollection<NSF10ViewModel> ViewModels { get; set; } = new BindableCollection<NSF10ViewModel>();

        public NsfCollectionViewModel()
        {
            ViewModels.Add(new NSF10ViewModel("192.168.0.51"));
            ViewModels.Add(new NSF10ViewModel("192.168.0.52"));
            ViewModels.Add(new NSF10ViewModel("192.168.0.53"));
            ViewModels.Add(new NSF10ViewModel("192.168.0.54"));
            ViewModels.Add(new NSF10ViewModel("192.168.0.55"));
            ViewModels.Add(new NSF10ViewModel("192.168.0.56"));
        }
    }
}
