using System;

namespace DAQ.Service
{
	public class SaveMsg<T>
	{
		public string Source
		{
			get;
			set;
		}

		public T Msg
		{
			get;
			set;
		}

		public static SaveMsg<T> Create(string source, T msg)
		{
			return new SaveMsg<T>
			{
				Msg = msg,
				Source = source
			};
		}
	}
}
