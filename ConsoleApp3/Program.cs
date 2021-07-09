using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ConsoleApp3
{
    class Program
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
        static void Main(string[] args)
        {
            var data = @"(602)(ITEM-1)(2021-07-09)(7016)(OK)(ITEM-2)(10:51:10---10:51:15)(30.0C)(Left)(ITEM-3)(2954)(Admin)(13)(ACHV)(0750V 0.15mA ARC:0)(0.018)(OK)(IR)(1000V 1000M 0.5s)(>2000)(OK)(SURGE1-2)(1000V  Area:5.0)( Area:0.1)(OK)(SURGE3-4)(1000V  Area:5.0)( Area:0.7)(OK)(SURGE5-6)(1000V  Area:5.0)( Area:0.4)(OK)(IND1-2)(137.8uH-152.2uH)(142.4, )(OK)(IND3-4)(137.8uH-152.2uH)(143.7, )(OK)(IND5-6)(137.8uH-152.2uH)(144.4, )(OK)(IND)(Banlance-- 35.000%)(0.767% 0.139% 0.627% , )(OK)(DCR1-2)(54.15m~59.85m)(56.77m)(OK)(DCR3-4)(54.15m~59.85m)(56.23m)(OK)(DCR5-6)(54.15m~59.85m)(56.33m)(OK)(DCR)(Banlance--:2.000%)(0.960%)(OK)
";
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
            Console.WriteLine("Hello World!");
        }
    }
}
