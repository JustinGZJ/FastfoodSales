using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;

namespace DAQ
{
    public class QueueProcesser<T>
    {
        ConcurrentQueue<T> Msgs = new ConcurrentQueue<T>();
        public int Capcity { get; set; } = 100;
        public int Interval { get; set; } = 1000;
        Task task;
        int locker = 0;
        Action<List<T>> Todo = new Action<List<T>>((s) => { });
        public QueueProcesser(Action<List<T>> action)
        {
            Todo = action;
            timer = new Timer((o) => BatchProcess(), null, Interval, Interval);
        }

        Timer timer;
        public void Process(T msg)
        {
            Msgs.Enqueue(msg);
            if (Msgs.Count > Capcity)
            {
                task?.Wait();
            }
            if (Interlocked.Increment(ref locker) == 1)
            {
                task = Task.Run(() =>
                {
                    BatchProcess();
                }
                ).ContinueWith((x) => Interlocked.Exchange(ref locker, 0));
            }
            else
            {
                timer.Change(0, Interval);
            }
        }
        public event Action<Exception> OnError;
        private void BatchProcess()
        {
            lock (this)
            {
                List<T> vs = new List<T>();
                while (Msgs.TryDequeue(out T v))
                {
                    vs.Add(v);
                }
                try
                {
                    Todo(vs);
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex);
                    //   throw;
                }
            }
        }
    }
}
