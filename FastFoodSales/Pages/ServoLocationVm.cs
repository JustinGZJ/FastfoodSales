using Stylet;

namespace DAQ
{
    public class ServoLocationVm : PropertyChangedBase
    {
        private string name;
        public string Name
        {
            get { return name; }
            set { SetAndNotify(ref name, value); }
        }
        private double addr1;
        public double Addr1
        {
            get { return addr1; }
            set { SetAndNotify(ref addr1, value); }
        }
        private double addr2;
        public double Addr2
        {
            get { return addr2; }
            set { SetAndNotify(ref addr2, value); }
        }
        private double addr3;
        public double Addr3
        {
            get { return addr3; }
            set { SetAndNotify(ref addr3, value); }
        }
        private double addr4;
        public double Addr4
        {
            get { return addr4; }
            set { SetAndNotify(ref addr4, value); }
        }
    }
}
