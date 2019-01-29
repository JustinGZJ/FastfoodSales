using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;
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

        private void BatchProcess()
        {
            lock (this)
            {
                List<T> vs = new List<T>();
                while (Msgs.TryDequeue(out T v))
                {
                    vs.Add(v);
                }
                Todo(vs);
            }
        }
    }
 
    public class MsgDBSaver : IQueueProcesser<TestSpecViewModel>
    {
        QueueProcesser<TestSpecViewModel> processer;
        public string FolderName { get; set; } = "../DAQData/";
        [StyletIoC.Inject]
        public Stylet.IEventAggregator Event { get; set; }
        public MsgDBSaver()
        {
            processer = new QueueProcesser<TestSpecViewModel>((s) =>
            {
                using (DataAccess db = new DataAccess())
                {
                    try
                    {
                        db.SaveTestSpecs(s);
                    }
                    catch (Exception ex)
                    {
                        Event.Publish(new MsgItem { Level = "E", Time = DateTime.Now, Value = ex.Message });
                    }
                }
            });
        }

        public void Process( TestSpecViewModel msg)
        {
            processer.Process(msg);
        }
    }
    public class MsgFileSaver<T> : IQueueProcesser<T> where T:ISource
    {
        QueueProcesser<T> processer;
        public string FolderName { get; set; } = "../DAQData/";
        public MsgFileSaver()
        {
            processer = new QueueProcesser<T>((s) =>
              {
                  string fullpath = Path.GetFullPath(FolderName);
                  List<Task> tasks = new List<Task>();

                  var groups = s.GroupBy(x => x.Source);
                  foreach (var group in groups)
                  {
  
                      {
                          string path = Path.Combine(fullpath, DateTime.Today.ToString("yyyyMMdd"));
                          if (!Directory.Exists(path))
                              Directory.CreateDirectory(path);
                          var fileName = Path.Combine(path, group.Key + ".csv");
                          Console.WriteLine(fileName);
                          if (!File.Exists(fileName))
                          {
                              StringBuilder stringBuilder = new StringBuilder();
                              var propertyInfos = typeof(T).GetProperties();
                              stringBuilder.Append("Date Time,");
                              foreach (var p in propertyInfos)
                              {
                                      stringBuilder.Append($"{p.Name},");
                              }
                              stringBuilder.AppendLine();
                              File.AppendAllText(fileName, stringBuilder.ToString());
                          }

                          StringBuilder sb = new StringBuilder();
                          foreach (var v in group)
                          {
                              sb.Append($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}," +        
                                  $"{string.Join(",", v.GetType().GetProperties().Select(x => x.GetValue(v, null) ?? ""))}");
                              sb.AppendLine();
                          }
                          File.AppendAllText(fileName, sb.ToString());
                      }
                    //));
                  }
             //     Task.WaitAll(tasks.ToArray());
              });
        }
        public void Process(T msg)
        {
            processer.Process(msg);
        }
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
}
