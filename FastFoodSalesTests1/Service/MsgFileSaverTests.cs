using Microsoft.VisualStudio.TestTools.UnitTesting;
using DAQ.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAQ.Service.Tests
{
    [TestClass()]
    public class MsgFileSaverTests
    {

        [TestMethod()]
        public void ProcessTest()
        {
            MsgFileSaver<PLC_FINAL_DATA> fileSaver = new MsgFileSaver<PLC_FINAL_DATA>();
            fileSaver.Process(new PLC_FINAL_DATA());
            Assert.IsTrue(true);
        }

    }
}