using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DAQ.Service
{
    public class MsgFileSaver<T> : IQueueProcesser<T> where T : ISource
    {
        QueueProcesser<T> processer;
        public string FolderName { get; set; } = "../DAQData/";
        string filename = "";
        System.Reflection.PropertyInfo[] Properties;
       
        public MsgFileSaver()
        {
            Properties = typeof(T).GetProperties().Where(x=>x.Name!="Source"&&x.Name!= "ReadCount").ToArray();
            processer = new QueueProcesser<T>((s) =>
              {
                  string fullpath = Path.GetFullPath(FolderName);
                  List<Task> tasks = new List<Task>();

                  var groups = s.GroupBy(x => x.Source);
                  foreach (var group in groups)
                  {
                      string path = Path.Combine(fullpath, DateTime.Today.ToString("yyyyMMdd"));
                      if (!Directory.Exists(path))
                          Directory.CreateDirectory(path);
                      var fileName = Path.Combine(path, group.Key + ".csv");
                      Console.WriteLine(fileName);
                      if (!File.Exists(fileName))
                      {
                          StringBuilder stringBuilder = new StringBuilder();
                          var propertyInfos = Properties;
                          stringBuilder.Append("Date Time,");
                          foreach (var p in propertyInfos)
                          {
                              stringBuilder.Append($"{p.Name},");
                          }
                          stringBuilder.AppendLine();
                          File.AppendAllText(fileName, stringBuilder.ToString(), Encoding.UTF8);
                      }

                      StringBuilder sb = new StringBuilder();
                     
                      foreach (var v in group)
                      {
                          string value = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}," +
                              $"{string.Join(",", Properties.Select(x => x.GetValue(v, null).ToString().Replace(",","") ?? ""))}";       
                          sb.Append(value);                      
                          sb.AppendLine();
                      }
                      File.AppendAllText(fileName, sb.ToString(), Encoding.UTF8);
                      //));
                  }
                  //     Task.WaitAll(tasks.ToArray());
              },()=> {
                  bool inUse = true;

                  FileStream fs = null;
                  if (filename == "")
                      return true;
                  try
                  {
                      fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.None);
                      inUse = false;
                  }
                  catch
                  {
                      return false;
                  }
                  finally
                  {
                      if (fs != null)
                          fs.Close();
                  }
                  return true;
              });
        }
        public void Process(T msg)
        {
            processer.Process(msg);
        }

        public bool CanProcess()
        {
            return processer.CanProcess();
        }
    }
}
