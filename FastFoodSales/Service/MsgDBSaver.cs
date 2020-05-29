using Stylet;
using StyletIoC;
using System;
using System.Collections.Generic;

namespace DAQ.Service
{
	public class MsgDBSaver : IQueueProcesser<TestSpecViewModel>
	{
		private QueueProcesser<TestSpecViewModel> processer;

		public string FolderName
		{
			get;
			set;
		}

		[Inject]
		public IEventAggregator Event
		{
			get;
			set;
		}

		public MsgDBSaver()
		{
			this.<FolderName>k__BackingField = "../DAQData/";
			base..ctor();
			this.processer = new QueueProcesser<TestSpecViewModel>(delegate(List<TestSpecViewModel> s)
			{
				using (DataAccess dataAccess = new DataAccess())
				{
					try
					{
						dataAccess.SaveTestSpecs(s);
					}
					catch (Exception ex)
					{
						EventAggregatorExtensions.Publish(this.Event, new MsgItem
						{
							Level = "E",
							Time = DateTime.Now,
							Value = ex.Message
						}, Array.Empty<string>());
					}
				}
			});
		}

		public void Process(TestSpecViewModel msg)
		{
			this.processer.Process(msg);
		}
	}
}
