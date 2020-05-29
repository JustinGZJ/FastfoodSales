using DAQ.Properties;
using Stylet;
using System;

namespace DAQ.Service
{
	public class Th9201Service : PortService
	{
		public override string PortName
		{
			get
			{
				return Settings.Default.PORT_B;
			}
		}

		public Th9201Service(PlcService plc, IEventAggregator @event) : base(plc, @event)
		{
			base.InstName = "TH9201";
			base.TestSpecs[0].Name = "TH9201";
		}

		public override void Handle(EventIO message)
		{
			bool value = message.Value;
			if (value)
			{
				int index = message.Index;
				int num = index;
				if (num == 0)
				{
					base.Plc.WriteBool(0, false);
					this.Read();
					base.Plc.Pulse(8, 100);
				}
			}
		}

		public override void Read()
		{
			base.Read();
			string text;
			bool flag = base.Request(":TEST:FETCh?", out text);
			if (flag)
			{
				base.FileSaver.Process(new TLog
				{
					Source = base.InstName,
					Log = text
				});
				int num;
				bool flag2 = int.TryParse(text.Split(new char[]
				{
					','
				})[0], out num);
				if (flag2)
				{
					base.TestSpecs[0].Result = ((num == 1) ? 1 : -1);
				}
				else
				{
					EventAggregatorExtensions.Publish(base.Events, new MsgItem
					{
						Time = DateTime.Now,
						Level = "E",
						Value = "Error reply"
					}, Array.Empty<string>());
				}
			}
		}

		public override bool Connect()
		{
			this.port.BaudRate = 19200;
			return base.Connect();
		}
	}
}
