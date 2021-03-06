﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StyletIoC;
using DAQ.Service;
using Stylet;
namespace DAQ.Pages
{
    public class PLCViewModel : Screen
    {
        public PlcService PLC { get; set; }

        protected override void OnViewLoaded()
        { 
            base.OnViewLoaded();
        }
        public void SetValue(KV<bool> kv)
        {
            Task.Run(() =>
            {
                PLC.WriteBool(kv.Index, !kv.Value);
            });         
        }
        public PLCViewModel(PlcService PLC)
        {
            this.PLC = PLC;
        }
    };
}
public class KV<T> : PropertyChangedBase
{
    public int Index { get; set; }
    public DateTime Time { get; set; }
    public string Key { get; set; }
    public T Value { get; set; }
}


