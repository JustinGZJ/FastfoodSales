using StyletIoC;
using Stylet;
using DAQ.Pages;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;
using CsvHelper;
using System.Dynamic;


namespace DAQ
{

    public class HomeViewModel : Screen
    {

        IEventAggregator _eventAggregator;
        MsgViewModel _msg;
        private MCPLCDataAccess _dataAccess;
        private IConfigureFile configure;
        PlcParas plcparas;

        BindableCollection<ServoLocationVm> servos = new BindableCollection<ServoLocationVm>();

        public HomeViewModel(MsgViewModel msg, IEventAggregator eventAggregator, MCPLCDataAccess dataAccess, IConfigureFile configure)
        {
            _eventAggregator = eventAggregator;
            _msg = msg;
            _dataAccess = dataAccess;
            this.configure = configure;

            InitialConnection(configure);
        }

        private IConfigureFile InitialConnection(IConfigureFile configure)
        {

            configure = configure.Load();
            plcparas = configure.GetValue<PlcParas>(ConfigureKeys.PLCParas);
            if (Plcparas == null)
            {
                plcparas = new PlcParas();
                Plcparas.ServoLocations = 
                new List<ServoLocation>
                {
                    new ServoLocation() { Name = "Z1", Addr1 = "D562", Addr2 = "D568", Addr3 = "D572", Addr4 = "D578" },
                    new ServoLocation() { Name = "Z2", Addr1 = "D662", Addr2 = "D668", Addr3 = "D672", Addr4 = "D678" },
                    new ServoLocation() { Name = "Z3", Addr1 = "D762", Addr2 = "D768", Addr3 = "D772", Addr4 = "D778" },
                    new ServoLocation() { Name = "Z4", Addr1 = "D862", Addr2 = "D868", Addr3 = "D872", Addr4 = "D878" }
                };
                configure = configure.SetValue(ConfigureKeys.PLCParas, Plcparas);
            }
            _dataAccess.Ip = Plcparas.Ip;
            _dataAccess.Port = Plcparas.Port;
            _dataAccess.TriggerAddress = Plcparas.TriggerAddress;
            var vs = Locations.SelectMany(x => new[] { x.Addr1, x.Addr2, x.Addr3, x.Addr4 }).Select(m => m.Substring(1));
            maxIndex = vs.Select(x => int.Parse(x)).Max();
            minIndex = vs.Select(x => int.Parse(x)).Min();
            _dataAccess.StartAddress = $"D{minIndex}";
            _dataAccess.Length = (ushort)(maxIndex - minIndex + 20);
            _dataAccess.Connect();
            _dataAccess.OnDataReady -= _dataAccess_OnDataReady;
            _dataAccess.OnDataReady += _dataAccess_OnDataReady;
            _dataAccess.OnError -= _dataAccess_OnError;
            _dataAccess.OnError += _dataAccess_OnError;
            _dataAccess.OnDataTriger -= _dataAccess_OnDataTriger;
            _dataAccess.OnDataTriger += _dataAccess_OnDataTriger;
            return configure;
        }

        private void _dataAccess_OnDataTriger(bool obj)
        {
            using (var writer = new StreamWriter(Path.Combine(AppFolders.Logs, DateTime.Today.ToString("yyyyMMdd") + ".csv")))
            using (var csv = new CsvWriter(writer))
            {
                if (servos.Count >= 3)
                {
                    dynamic record = new ExpandoObject();
                    record.DateTime = DateTime.Now;
                    record.Z1Station1 = servos[0].Addr1;
                    record.Z1Station2 = servos[0].Addr2;
                    record.Z1Station3 = servos[0].Addr3;
                    record.Z1Station4 = servos[0].Addr4;
                    record.Z2Station1 = servos[1].Addr1;
                    record.Z2Station2 = servos[1].Addr2;
                    record.Z2Station3 = servos[1].Addr3;
                    record.Z2Station4 = servos[1].Addr4;
                    record.Z3Station1 = servos[2].Addr1;
                    record.Z3Station2 = servos[2].Addr2;
                    record.Z3Station3 = servos[2].Addr3;
                    record.Z3Station4 = servos[2].Addr4;
                    record.Z4Station1 = servos[3].Addr1;
                    record.Z4Station2 = servos[3].Addr2;
                    record.Z4Station3 = servos[3].Addr3;
                    record.Z4Station4 = servos[3].Addr4;
                    csv.WriteRecord(record);
                }
            }
        }

        private void _dataAccess_OnError(string obj)
        {
            _eventAggregator.Publish(new MsgItem() { Level = "E", Time = DateTime.Now, Value = obj });
        }
        int maxIndex;
        int minIndex;

        private void _dataAccess_OnDataReady(byte[] obj)
        {
            foreach (var loc in Locations)
            {
                var vm = Servos.FirstOrDefault(x => x.Name == loc.Name);
                if (vm == null)
                {
                    vm = new ServoLocationVm() { Name = loc.Name };
                    Servos.Add(vm);
                }
                vm.Addr1 = _dataAccess.ReadUInt32(int.Parse(loc.Addr1.Substring(1)) - minIndex) / 1000.0;
                vm.Addr2 = _dataAccess.ReadUInt32(int.Parse(loc.Addr2.Substring(1)) - minIndex) / 1000.0;
                vm.Addr3 = _dataAccess.ReadUInt32(int.Parse(loc.Addr3.Substring(1)) - minIndex) / 1000.0;
                vm.Addr4 = _dataAccess.ReadUInt32(int.Parse(loc.Addr4.Substring(1)) - minIndex) / 1000.0;
            }
        }

        public void ChangeData()
        {
            configure.Load();
            var plcparas = configure.GetValue<PlcParas>(ConfigureKeys.PLCParas);
            foreach (var vm in Locations)
            {
                var para = plcparas.ServoLocations.FirstOrDefault(x => x.Name == vm.Name);                
                if (para != null)
                {
                    para.Addr1 = vm.Addr1;
                    para.Addr2 = vm.Addr2;
                    para.Addr3 = vm.Addr3;
                    para.Addr4 = vm.Addr4;
                }
            }
            plcparas.Ip = Plcparas.Ip;
            plcparas.Port = Plcparas.Port;
            configure.SetValue(ConfigureKeys.PLCParas, plcparas);
        }

        public BindableCollection<ServoLocationVm> Servos { get => servos; set => servos = value; }
        public List<ServoLocation> Locations { get => Plcparas.ServoLocations;  }
        public PlcParas Plcparas { get => plcparas;  }

        protected override void OnInitialActivate()
        {
            base.OnInitialActivate();
        }

        protected override void OnViewLoaded()
        {
            base.OnViewLoaded();
        }
    }
}

namespace DAQ
{
}
