using HalconDotNet;
using Stylet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAQ.Pages
{
    public class CameraViewModel : Screen
    {
        HWindow _dispwin1;
        HWindow _dispwin2;
        HDevelopExport _export = new HDevelopExport();
        public CameraViewModel()
        {

        }
        protected override void OnViewLoaded()
        {
            _dispwin1 = ((CameraView)View).disp1.HalconWindow;
            _dispwin2 = ((CameraView)View).disp2.HalconWindow;
            base.OnViewLoaded();
        }

        public void Test()
        {
            _export.action(_dispwin1,_dispwin2);
        }
    }
}
