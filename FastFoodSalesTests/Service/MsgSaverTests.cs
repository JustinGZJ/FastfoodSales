using Microsoft.VisualStudio.TestTools.UnitTesting;
using DAQ.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DAQ.Service.Tests
{
    [TestClass()]
    public class MsgSaverTests
    {
        public class poco1:ISource
        {
            public string Name { get; set; }
            public string Source { get; set; }
            public int Value { get; set; }
            public int Upper { get; set; }
            public int Lower { get; set; }
        }
        [TestMethod()]
        public void MsgSaverTest()
        {
   
            try
            {
                List<Task> tasks = new List<Task>();
                Directory.Delete(Path.GetFullPath("../DAQData"), true);
                MsgFileSaver<poco1> saver = new MsgFileSaver<poco1>();            
                for (int i = 0; i < 100000; i++)
                {
                  var l=  Task.Run(() =>
                    {
                        var t = SaveMsg<poco1>.Create("1", new poco1()
                        {
                            Lower = new Random().Next(100),
                            Name = Faker.NameFaker.MaleFirstName(),
                            Upper = new Random().Next(1000),
                            Value = new Random().Next(1000),
                             Source="1"                         
                        });             
                        saver.Process(t.Msg);
                      // System.Threading.Thread.Sleep(1); //必须有延时
                    });
                    tasks.Add(l);
                }
                Task.WaitAll(tasks.ToArray());
              
                Assert.IsFalse(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Assert.IsFalse(true);
              //  throw;
            }

            
        }

    }
}