namespace DAQ.Service
{
    public interface IPortService
    {
        bool IsConnected { get; set; }

        bool Connect();
        void DisConnect();
    }
}