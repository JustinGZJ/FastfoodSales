using DAQ.Properties;
using Stylet;
using System;

namespace DAQ.Service
{
	public class Th2882Service : PortService
	{
		public override string PortName
		{
			get
			{
				return Settings.Default.PORT_C;
			}
		}

		public Th2882Service(PlcService plc, IEventAggregator @event) : base(plc, @event)
		{
			base.InstName = "TH2882";
			base.TestSpecs[0].Name = "TH2882";
		}

		public override void Handle(EventIO message)
		{
			bool value = message.Value;
			if (value)
			{
				int index = message.Index;
				int num = index;
				if (num == 1)
				{
					base.Plc.WriteBool(1, false);
					this.Read();
					base.Plc.Pulse(9, 100);
				}
			}
		}

		public override void Read()
		{
			base.Read();
			string text;
			bool flag = base.Request("FETCh:CRESult?", out text);
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
					base.TestSpecs[0].Result = ((num > 0) ? 1 : -1);
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
			this.port.BaudRate = 38400;
			return base.Connect();
		}
	}
}
