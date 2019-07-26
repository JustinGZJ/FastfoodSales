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
    public class TH2882ATests
    {
        [TestMethod()]
        public void ProcessDataTest()
        {

            ProcessData("1, -2.285253e-002, 9.9E37, 9999, 9.9E37,"); 
            Assert.Fail();
        }
        float[] vs = new float[4];
        public void ProcessData(string reply)
        {
            var s = reply.Split(',');
            if (s.Length > 4)
            {
                if (!float.TryParse(s[1], out vs[0]))
                {
                    vs[0] = float.MaxValue;
                }
                if (!float.TryParse(s[2], out vs[1]))
                {
                    vs[1] = float.MaxValue;
                }
                if (!float.TryParse(s[3], out vs[2]))
                {
                    vs[2] = float.MaxValue;
                }
                if (!float.TryParse(s[4], out vs[3]))
                {
                    vs[3] = float.MaxValue;
                }
            }
        }
    }
}