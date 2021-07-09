using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SimpleTCP;
using StyletIoC;
using Stylet;


namespace DAQ.Service
{
    public class AIP : IDisposable
    {
        
        public class TestItem
        {
            public string Name { get; set; }
            public string Spec { get; set; }
            public string strvalue { get; set; }

            public string strjudge { get; set; }
            public float[] value { get; set; }
            public bool judge { get; set; }
        }
        SimpleTcpClient m_client;
        [Inject]
        IEventAggregator Events;
        [Inject]
        public PlcService plc { get; set; }
        public AIP()
        {
     
        }
        public void Connect()
        {
            try
            {
                if (m_client != null)
                    m_client.DataReceived -= M_client_DataReceived;
                m_client = new SimpleTcpClient().Connect("192.168.0.140", 6000);
                m_client.DataReceived += M_client_DataReceived;
            }
            catch (Exception ex)
            {
                Events.Publish(new MsgItem { Time = DateTime.Now, Level = "E", Value = "AIP 连接失败"+ex.Message });
                // throw;
            }
        }


        public void Dispose()
        {
            if (m_client != null)
                m_client.DataReceived -= M_client_DataReceived;
        }

        private void handleReceived(string data)
        {
            try
            {
                var splits = data.Split('(', ')').Where(x => !string.IsNullOrEmpty(x)).ToArray();
                var result = splits[4];
                List<TestItem> array = new List<TestItem>();
                var itemcount = (splits.Length - 13) / 4;
                Regex rgxNumber = new Regex(@"(\-|\+)?\d+(\.\d+)?");
                for (int i = 0; i < itemcount; i++)
                {
                    TestItem item = new TestItem();
                    item.Name = splits[13 + i * 4 + 0];
                    item.Spec = splits[13 + i * 4 + 1];

                    // double.TryParse(splits[13 + i * 4 + 2], out double value);
                    item.strvalue = splits[13 + i * 4 + 2];
                    MatchCollection matchs = rgxNumber.Matches(item.strvalue);
                    List<float> values = new List<float>();
                    foreach (var x in matchs)
                    {
                        var value = double.Parse(x.ToString());
                        values.Add((float)value);
                    }
                    //  var values = matchs.Select(x => double.Parse(x.Value)).ToArray();
                    item.value = values.ToArray();
                    item.strjudge = splits[13 + i * 4 + 3];
                    item.judge = item.strjudge == "OK";
                    array.Add(item);
                }
                var testdata = plc.ReadTestData("DB3003.0");
                var dictrionalry = array.ToDictionary(X => X.Name);

                if (dictrionalry.ContainsKey("ACHV"))
                {
                    testdata.耐压mA = dictrionalry["ACHV"].value[0];
                }


                if (dictrionalry.ContainsKey("IR"))
                {
                    testdata.绝缘电阻Mohm = dictrionalry["IR"].value[0]; 
                }
                // testdata. =dictrionalry["ACHV"].value;


                if (dictrionalry.ContainsKey("SURGE1-2"))
                {
                    testdata.线圈1匝间 = (dictrionalry["SURGE1-2"].value[0] / 100).ToString("P");
                    testdata.线圈1匝间结果 = dictrionalry["SURGE1-2"].judge ? 1 : 0; 
                }

                if (dictrionalry.ContainsKey("SURGE3-4"))
                {
                    testdata.线圈2匝间 = (dictrionalry["SURGE3-4"].value[0] / 100).ToString("P");
                    testdata.线圈2匝间结果 = dictrionalry["SURGE3-4"].judge ? 1 : 0; 
                }
                if (dictrionalry.ContainsKey("SURGE5-6"))
                {
                    testdata.线圈3匝间 = (dictrionalry["SURGE5-6"].value[0] / 100).ToString("P");
                    testdata.线圈3匝间结果 = dictrionalry["SURGE5-6"].judge ? 1 : 0; 
                }

                if (dictrionalry.ContainsKey("IND1-2"))
                {
                    testdata.线圈1电感uH = dictrionalry["IND1-2"].value[0];
                    testdata.线圈1电感结果 = (Int16)(dictrionalry["IND1-2"].judge ? 1 : 0); 
                }
          
                if (dictrionalry.ContainsKey("IND3-4"))
                {
                    testdata.线圈2电感uH = dictrionalry["IND3-4"].value[0];
                    testdata.线圈2电感结果 = (Int16)(dictrionalry["IND3-4"].judge ? 1 : 0); 
                }
             
                if (dictrionalry.ContainsKey("IND5-6"))
                {
                    testdata.线圈3电感uH = dictrionalry["IND5-6"].value[0];
                    testdata.线圈3电感结果 = (Int16)(dictrionalry["IND5-6"].judge ? 1 : 0);
                }
                if (dictrionalry.ContainsKey("IND"))
                {
                    testdata.电感1平衡度 = dictrionalry["IND"].value[0];
                    testdata.电感2平衡度 = dictrionalry["IND"].value[1];
                    testdata.电感3平衡度 = dictrionalry["IND"].value[2];
                }

                if (dictrionalry.ContainsKey("DCR1-2"))
                {
                    testdata.线圈1电阻mohm = dictrionalry["DCR1-2"].value[0];
                    testdata.线圈1电阻结果 = (Int16)(dictrionalry["DCR1-2"].judge ? 1 : 0); 
                }
           
                if (dictrionalry.ContainsKey("DCR3-4"))
                {
                    testdata.线圈2电阻mohm = dictrionalry["DCR3-4"].value[0];
                    testdata.线圈2电阻结果 = (Int16)(dictrionalry["DCR3-4"].judge ? 1 : 0); 
                }
             
                if (dictrionalry.ContainsKey("DCR5-6"))
                {
                    testdata.线圈3电阻mohm = dictrionalry["DCR5-6"].value[0];
                    testdata.线圈3电阻结果 = (Int16)(dictrionalry["DCR5-6"].judge ? 1 : 0); 
                }

                if (dictrionalry.ContainsKey("DCR"))
                {
                    testdata.电阻平衡度 = dictrionalry["DCR"].value[0];
                }
                plc.WriteTestData("DB3003.0", testdata);
            }
            catch(Exception ex)
            {
                Events.Publish(new MsgItem { Time = DateTime.Now, Level = "d", Value = "AIP处理数据异常：" + ex.Message });
            }
            finally
            {
                plc.WriteBool((int)IO_DEF.匝间数据1获取完成, true);
            }
        
        }


        private void M_client_DataReceived(object sender, Message e)
        {
            var data = e.MessageString;
            Events.Publish(new MsgItem { Time = DateTime.Now, Level = "d", Value = "AIP收到数据：" + data});
            handleReceived(data);

        }
    }
}

