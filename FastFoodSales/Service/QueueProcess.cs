using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using Stylet;

namespace DAQ.Service
{
    public class QueueProcesser<T>
    {
        ConcurrentQueue<T> Msgs = new ConcurrentQueue<T>();
        public int Capcity { get; set; } = 100;
        public int Interval { get; set; } = 1000;
        Task task;
        int locker = 0;
        Action<List<T>> Todo = new Action<List<T>>((s) => { });
        public QueueProcesser(Action<List<T>> action,Func<bool> canAction=null)
        {
            Todo = action;
            CanProcess = canAction;
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

        private void BatchProcess()
        {
            lock (this)
            {
               // CanProcess?.Invoke()
                if(CanProcess!=null&&CanProcess()==false)
                {
                    return;
                }
                List<T> vs = new List<T>();
                while (Msgs.TryDequeue(out T v))
                {
                    vs.Add(v);
                }
                try
                {
                    Todo(vs);
                }
                catch (Exception)
                {
                    ;
                   // throw;
                }
            }
        }

        public Func<bool> CanProcess;
    }
    public class SaveMsg<T>
    {
        public string Source { get; set; }
        public T Msg { get; set; }

        public static SaveMsg<T> Create(string source, T msg)
        {
            var m = new SaveMsg<T>() { Msg = msg, Source = source };
            return m;
        }
    }
    public interface ISource
    {
        string Source { get; set; }
    }
    public class TLog : ISource
    {
        public string Source { get; set; }
        public string Log { get; set; }
    }
}
