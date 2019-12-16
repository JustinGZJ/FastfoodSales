using StyletIoC;
using Stylet;
using DAQ.Pages;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net;
using CsvHelper;
using System.Dynamic;

namespace DAQ
{
    public static class AppFolders
    {
        static AppFolders()
        {
            Directory.CreateDirectory(Apps);
            Directory.CreateDirectory(Logs);
            Directory.CreateDirectory(Users);
        }

        /// <summary>
        /// It represents the path where the "Accelerider.Windows.exe" is located.
        /// </summary>
        public static readonly string MainProgram = AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// %AppData%\DAQ
        /// </summary>
        public static readonly string AppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DAQ");

        /// <summary>
        /// %AppData%\DAQ\Apps
        /// </summary>
        public static readonly string Apps = Path.Combine(AppData, nameof(Apps));

        /// <summary>
        /// %AppData%\DAQ\Logs
        /// </summary>
        public static readonly string Logs = Path.Combine(AppData, nameof(Logs));

        /// <summary>
        /// %AppData%\DAQ\Users
        /// </summary>
        public static readonly string Users = Path.Combine(AppData, nameof(Users));
    }
}

namespace DAQ
{
    public static class DAQFiles
    {
        /// <summary>
        /// %AppData%\Accelerider\accelerider.config
        /// </summary>
        public static readonly string Configure = Path.Combine(AppFolders.AppData, "DAQ.config");
    }

}

public class ServoLocation
{
    public string Name { get; set; }
    public string Addr1 { get; set; }
    public string Addr2 { get; set; }
    public string Addr3 { get; set; }
    public string Addr4 { get; set; }
}

namespace DAQ
{
    public class ServoLocationVm : PropertyChangedBase
    {
        private string name;
        public string Name
        {
            get { return name; }
            set { SetAndNotify(ref name, value); }
        }
        private double addr1;
        public double Addr1
        {
            get { return addr1; }
            set { SetAndNotify(ref addr1, value); }
        }
        private double addr2;
        public double Addr2
        {
            get { return addr2; }
            set { SetAndNotify(ref addr2, value); }
        }
        private double addr3;
        public double Addr3
        {
            get { return addr3; }
            set { SetAndNotify(ref addr3, value); }
        }
        private double addr4;
        public double Addr4
        {
            get { return addr4; }
            set { SetAndNotify(ref addr4, value); }
        }
    }

    public class HomeViewModel : Screen
    {

        IEventAggregator _eventAggregator;
        MsgViewModel _msg;
        private MCPLCDataAccess _dataAccess;
        private IConfigureFile configure;
        private List<ServoLocation> _locations;

        BindableCollection<ServoLocationVm> servos = new BindableCollection<ServoLocationVm>();

        public HomeViewModel(MsgViewModel msg, IEventAggregator eventAggregator, MCPLCDataAccess dataAccess, IConfigureFile configure)
        {
            _eventAggregator = eventAggregator;
            _msg = msg;
            _dataAccess = dataAccess;
            this.configure = configure;

            configure = InitialConnection(configure);
        }

        private IConfigureFile InitialConnection(IConfigureFile configure)
        {
            configure = configure.Load();
            var plcparas = configure.GetValue<PlcParas>(ConfigureKeys.PLCParas);
            if (plcparas == null)
            {
                plcparas = new PlcParas();
                configure.SetValue(ConfigureKeys.PLCParas, plcparas);
            }
            _locations = plcparas.ServoLocations;
            _dataAccess.Ip = plcparas.Ip;
            _dataAccess.Port = plcparas.Port;
            _dataAccess.TriggerAddress = plcparas.TriggerAddress;
            var vs = _locations.SelectMany(x => new[] { x.Addr1, x.Addr2, x.Addr3, x.Addr4 }).Select(m => m.Substring(1));
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
                    record.Z1Station1 =servos[0].Addr1;
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
            foreach (var loc in _locations)
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

        public BindableCollection<ServoLocationVm> Servos { get => servos; set => servos = value; }

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
    public class ValueChangedEventArgs : EventArgs
    {
        public string KeyName { get; }

        public ValueChangedEventArgs(string keyName) => KeyName = keyName;
    }

    public static class ConfigureKeys
    {
        public static string PLCParas => nameof(PLCParas);
    }

    public class PlcParas
    {
        public int Port { get; set; } = 5000;
        public string Ip { get; set; } = "127.0.0.1";
        public List<ServoLocation> ServoLocations { get; set; } =
            new List<ServoLocation>
                {
                    new ServoLocation() { Name = "Z1", Addr1 = "D562", Addr2 = "D568", Addr3 = "D572", Addr4 = "D578" },
                    new ServoLocation() { Name = "Z2", Addr1 = "D662", Addr2 = "D668", Addr3 = "D672", Addr4 = "D678" },
                    new ServoLocation() { Name = "Z3", Addr1 = "D762", Addr2 = "D768", Addr3 = "D772", Addr4 = "D778" },
                    new ServoLocation() { Name = "Z4", Addr1 = "D862", Addr2 = "D868", Addr3 = "D872", Addr4 = "D878" }
                };
        public string TriggerAddress { get; set; } = "M300000";
    }
}

namespace DAQ
{
    public static class JsonExtensions
    {
        public static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
        public static readonly JsonSerializerSettings JsonDeserializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
        public static readonly FileLocatorConverter FileLocatorConverter = new FileLocatorConverter();

        static JsonExtensions()
        {
            JsonSerializerSettings.Converters.Add(FileLocatorConverter);
        }

        public static string ToJson<T>(this T @object, Formatting formatting = Formatting.None)
        {
            var type = @object.GetType();

            return typeof(T) != type
                ? JsonConvert.SerializeObject(@object, typeof(T), formatting, JsonSerializerSettings)
                : JsonConvert.SerializeObject(@object, formatting, JsonSerializerSettings);
        }

        public static T ToObject<T>(this string json)
        {
            return !string.IsNullOrWhiteSpace(json)
                ? JsonConvert.DeserializeObject<T>(json, JsonDeserializerSettings)
                : default;
        }


    }
}

namespace DAQ
{
    public class FileLocatorConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var fileLocator = (FileLocator)value;
            writer.WriteValue(fileLocator.FullPath);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        public override bool CanConvert(Type objectType) => objectType == typeof(FileLocator);
    }
}

namespace DAQ
{
    public class ConfigureFile : IConfigureFile
    {
        private JObject _storage = new JObject();
        private string _filePath = DAQFiles.Configure;

        public event EventHandler<ValueChangedEventArgs> ValueChanged;

        public bool Contains(string key) => _storage.Values().Any(token => token.Path == key);

        public T GetValue<T>(string key) => (_storage[key]?.ToString() ?? string.Empty).ToObject<T>();

        public IConfigureFile SetValue<T>(string key, T value)
        {
            if (EqualityComparer<T>.Default.Equals(GetValue<T>(key), value)) return this;

            _storage[key] = value.ToJson(Formatting.Indented);
            Save();
            ValueChanged?.Invoke(this, new ValueChangedEventArgs(key));

            return this;
        }

        public IConfigureFile Load(string filePath = null)
        {
            if (!string.IsNullOrEmpty(filePath)) _filePath = filePath;

            if (!File.Exists(_filePath))
            {
                _storage = new JObject(JObject.Parse("{}"));
                Save();
            }
            _storage = JObject.Parse(File.ReadAllText(_filePath));

            return this;
        }

        public IConfigureFile Clear()
        {
            _storage = new JObject();
            Save();
            return this;
        }

        public void Delete()
        {
            Clear();
            File.Delete(_filePath);
        }


        private void Save() => WriteToLocal(_filePath, _storage.ToString(Formatting.Indented));

        private void WriteToLocal(string path, string text)
        {
            try
            {
                File.WriteAllText(path, text);
            }
            catch (IOException)
            {
                WriteToLocal(path, text);
            }
        }
    }
}

namespace DAQ
{
    public interface IConfigureFile
    {
        event EventHandler<ValueChangedEventArgs> ValueChanged;

        bool Contains(string key);

        T GetValue<T>(string key);

        IConfigureFile SetValue<T>(string key, T value);

        IConfigureFile Load(string filePath = null);

        IConfigureFile Clear();

        void Delete();
    }
}