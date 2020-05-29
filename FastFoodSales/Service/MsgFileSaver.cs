using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DAQ.Service
{
	public class MsgFileSaver<T> : IQueueProcesser<T> where T : ISource
	{
		[CompilerGenerated]
		[Serializable]
		private sealed class <>c
		{
			public static readonly MsgFileSaver<T>.<>c <>9 = new MsgFileSaver<T>.<>c();

			public static Func<T, string> <>9__5_1;

			internal string ctor>b__5_1(T x)
			{
				return x.Source;
			}
		}

		private QueueProcesser<T> processer;

		public string FolderName
		{
			get;
			set;
		}

		public MsgFileSaver()
		{
			this.<FolderName>k__BackingField = "../DAQData/";
			base..ctor();
			this.processer = new QueueProcesser<T>(delegate(List<T> s)
			{
				string fullPath = Path.GetFullPath(this.FolderName);
				List<Task> list = new List<Task>();
				Func<T, string> arg_33_1;
				if ((arg_33_1 = MsgFileSaver<T>.<>c.<>9__5_1) == null)
				{
					arg_33_1 = (MsgFileSaver<T>.<>c.<>9__5_1 = new Func<T, string>(MsgFileSaver<T>.<>c.<>9.<.ctor>b__5_1));
				}
				IEnumerable<IGrouping<string, T>> enumerable = s.GroupBy(arg_33_1);
				foreach (IGrouping<string, T> current in enumerable)
				{
					string text = Path.Combine(fullPath, DateTime.Today.ToString("yyyyMMdd"));
					bool flag = !Directory.Exists(text);
					if (flag)
					{
						Directory.CreateDirectory(text);
					}
					string text2 = Path.Combine(text, current.Key + ".csv");
					Console.WriteLine(text2);
					bool flag2 = !File.Exists(text2);
					if (flag2)
					{
						StringBuilder stringBuilder = new StringBuilder();
						PropertyInfo[] properties = typeof(T).GetProperties();
						stringBuilder.Append("Date Time,");
						PropertyInfo[] array = properties;
						for (int i = 0; i < array.Length; i++)
						{
							PropertyInfo propertyInfo = array[i];
							stringBuilder.Append(propertyInfo.Name + ",");
						}
						stringBuilder.AppendLine();
						File.AppendAllText(text2, stringBuilder.ToString());
					}
					StringBuilder stringBuilder2 = new StringBuilder();
					using (IEnumerator<T> enumerator2 = current.GetEnumerator())
					{
						while (enumerator2.MoveNext())
						{
							T v = enumerator2.Current;
							stringBuilder2.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + string.Join<object>(",", from x in v.GetType().GetProperties()
							select x.GetValue(v, null) ?? ""));
							stringBuilder2.AppendLine();
						}
					}
					File.AppendAllText(text2, stringBuilder2.ToString());
				}
			});
		}

		public void Process(T msg)
		{
			this.processer.Process(msg);
		}
	}
}
