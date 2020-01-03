using System;

namespace DAQ
{
    public interface IQueueProcesser<T>
    {
        void Process(T msg);
    }
}
