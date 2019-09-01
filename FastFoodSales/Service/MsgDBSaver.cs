using Stylet;
using System;

namespace DAQ.Service
{
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

        public void Process(TestSpecViewModel msg)
        {
            processer.Process(msg);
        }
    }
}
