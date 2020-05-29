using System;
using System.Collections.Generic;
using System.IO;

namespace DAQ.Service
{
	public class AlarmManager
	{
		private const string ContentTitle = "TextContent";

		private const string AddrTitle = "TrigAddr";

		private int ContentIndex;

		private int AddrIndex;

		public List<AlarmItem> alarms = new List<AlarmItem>();

		public AlarmManager(string FileName = "EventLib.csv")
		{
			bool flag = !File.Exists(FileName);
			if (!flag)
			{
				string[] array = File.ReadAllLines(FileName);
				bool flag2 = array.Length > 3;
				if (flag2)
				{
					string[] array2 = array[1].Split(new char[]
					{
						'\t'
					});
					for (int i = 0; i < array2.Length; i++)
					{
						bool flag3 = array2[i].Contains("TextContent");
						if (flag3)
						{
							this.ContentIndex = i;
						}
						bool flag4 = array2[i].Trim(new char[]
						{
							'"'
						}) == "TrigAddr";
						if (flag4)
						{
							this.AddrIndex = i;
						}
						bool flag5 = this.AddrIndex != 0 && this.ContentIndex != 0;
						if (flag5)
						{
							break;
						}
					}
					for (int j = 2; j < array.Length; j++)
					{
						string[] array3 = array[j].Split(new char[]
						{
							'\t'
						});
						bool flag6 = array3.Length > this.ContentIndex + 1 && array3.Length > this.AddrIndex + 1;
						if (flag6)
						{
							bool flag7 = !string.IsNullOrWhiteSpace(array3[this.ContentIndex].Trim(new char[]
							{
								'"'
							}));
							if (flag7)
							{
								string text = array3[this.AddrIndex].Trim(new char[]
								{
									'"'
								});
								bool flag8 = !text.Contains(".");
								if (flag8)
								{
									text += ".00";
								}
								this.alarms.Add(new AlarmItem
								{
									Address = "C" + text,
									Content = array3[this.ContentIndex].Trim(new char[]
									{
										'"'
									})
								});
							}
						}
					}
				}
			}
		}
	}
}
