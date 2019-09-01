using System;

namespace ConsoleApp2
{
    class Program
    {
        static void Main(string[] args)
        {         
            Console.WriteLine(PercentToFloat("50%"));
        }
        public static float PercentToFloat(string value)
        {
            return float.TryParse(value.TrimEnd('%'), out float v) ? v / 100f : 0f;
        }
    }
}
