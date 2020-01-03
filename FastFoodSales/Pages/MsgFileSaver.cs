using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Reflection;
using Stylet;

namespace DAQ
{
    public class MsgFileSaver<T> : IQueueProcesser<T>
    {
        QueueProcesser<T> processer;
        public string FolderName { get; set; } = "../DAQData/";
        PropertyInfo[] propertyInfos;
        public MsgFileSaver(IEventAggregator @event)
        {
            processer = new QueueProcesser<T>((s) =>
            {
                string fullpath = Path.GetFullPath(FolderName);
                string path = Path.Combine(fullpath);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                var fileName = Path.Combine(path ,DateTime.Today.ToString("yyyyMMdd")+ ".csv");
                if (!File.Exists(fileName))
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    propertyInfos = typeof(T).GetProperties();
                    foreach (var p in propertyInfos)
                    {
                        stringBuilder.Append($"{p.Name},");
                    }
                    stringBuilder.AppendLine();
                    File.AppendAllText(fileName, stringBuilder.ToString());
                }

                StringBuilder sb = new StringBuilder();
                foreach (var v in s)
                {
                    sb.Append(
                        $"{string.Join(",", v.GetType().GetProperties().Select(x => x.GetValue(v, null) ?? ""))}");
                    sb.AppendLine();
                }
                File.AppendAllText(fileName, sb.ToString());
            });
            processer.OnError+=(e)=>
            {
                @event.Publish(new MsgItem()
                {
                    Time = DateTime.Now,
                    Level = "E",
                    Value = e.Message
                });
            };
        }
        public void Process(T msg)
        {
            processer.Process(msg);
        }
    }
}
