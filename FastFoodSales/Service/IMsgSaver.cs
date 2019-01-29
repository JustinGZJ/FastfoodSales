

namespace DAQ.Service
{
    public interface IQueueProcesser<T>
    {
        void Process(T msg);
    }
}