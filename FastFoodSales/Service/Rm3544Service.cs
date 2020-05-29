using DAQ.Properties;
using Stylet;
using System;

namespace DAQ.Service
{
	public class Rm3544Service : PortService
	{
		public override string PortName
		{
			get
			{
				return Settings.Default.PORT_A;
			}
		}

		public Rm3544Service(PlcService plc, IEventAggregator events) : base(plc, events)
		{
			base.InstName = "RM3544";
			base.TestSpecs[0].Name = "RM3544";
			base.TestSpecs[0].Result = 0;
			base.Events.Subscribe(this, Array.Empty<string>());
		}

		public override void Handle(EventIO message)
		{
			bool value = message.Value;
			if (value)
			{
				int index = message.Index;
				int num = index;
				if (num == 2)
				{
					base.Plc.WriteBool(2, false);
					this.Read();
					base.Plc.Pulse(10, 100);
				}
			}
		}

		public override void Read()
		{
			base.Read();
			string text;
			bool flag = base.Request("FETCH?", out text);
			if (flag)
			{
				base.FileSaver.Process(new TLog
				{
					Source = base.InstName,
					Log = text
				});
				float value;
				bool flag2 = float.TryParse(text, out value);
				if (flag2)
				{
					base.TestSpecs[0].Value = value;
				}
				base.TestSpecs[0].Result = (base.Plc.Bits[3] ? 1 : -1);
			}
		}
	}
}
