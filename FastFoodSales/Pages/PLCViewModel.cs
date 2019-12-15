using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StyletIoC;

using Stylet;
namespace DAQ.Pages
{
    public class PLCViewModel : Screen
    {
    };
}
public class KV<T> : PropertyChangedBase
{
    public int Index { get; set; }
    public DateTime Time { get; set; }
    public string Key { get; set; }
    public T Value { get; set; }
}


