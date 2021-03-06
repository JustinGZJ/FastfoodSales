using System;
using Stylet;
using StyletIoC;
using System.IO.Ports;
using DAQ.Service;
using System.Threading.Tasks;
using DAQ.Pages;
using HslCommunication.Core;
using HslCommunication.Profinet.Omron;
using HslCommunication.Profinet.Siemens;
using System.Windows.Threading;

namespace DAQ
{

    public interface IReadWriteFactory
    {
        IReadWriteNet GetReadWriteNet();
        string AddressA { get; }
        string AddressB { get; }
    }


    public class OmronPLCFactory : IReadWriteFactory
    {
        public IReadWriteNet GetReadWriteNet()
        {
            return new OmronFinsNet(Properties.Settings.Default.PLC_IP, Properties.Settings.Default.PLC_PORT)
            {
                SA1 = 0,
                DA1 = 0
            };
        }
        public string AddressA => "D8000";
        public string AddressB => "D8002";
    }


    public class SiemensPLCFactory : IReadWriteFactory
    {
        public IReadWriteNet GetReadWriteNet()
        {
            return new SiemensS7Net(SiemensPLCS.S1500, Properties.Settings.Default.PLC_IP);
        }
        public string AddressA => "M8000";
        public string AddressB => "M8002";
    }




    public class Bootstrapper : Bootstrapper<MainWindowViewModel>
    {
        protected override void ConfigureIoC(IStyletIoCBuilder builder)//2
        {
            // Configure the IoC container in here
            builder.Bind<IEventAggregator>().To<EventAggregator>().InSingletonScope();
            builder.Bind<RM3545>().ToSelf().InSingletonScope();
            builder.Bind<TH2775B>().ToSelf().InSingletonScope();
            builder.Bind<TH9320>().ToSelf().InSingletonScope();
            builder.Bind<TH2882A>().ToSelf().InSingletonScope();
            builder.Bind<HomeViewModel>().ToSelf().InSingletonScope();
            builder.Bind<MsgViewModel>().ToSelf().InSingletonScope();
            builder.Bind<SettingsViewModel>().ToSelf().InSingletonScope();
            builder.Bind<MsgDBSaver>().ToSelf().InSingletonScope();
            builder.Bind<MsgFileSaver<TLog>>().ToSelf();
            builder.Bind<PlcService>().ToSelf().InSingletonScope();
            builder.Bind<PLCViewModel>().ToSelf();
            builder.Bind<MainWindowViewModel>().ToSelf().InSingletonScope();
            builder.Bind<AlarmService>().ToSelf().InSingletonScope();
            builder.Bind<IReadWriteFactory>().To<SiemensPLCFactory>();
            builder.Autobind();
        }

        protected override void Configure()//3
        {

            // Perform any other configuration before the application starts
        }
        protected override void OnStart()//1
        {

            base.OnStart();
        }
        protected override void OnUnhandledException(DispatcherUnhandledExceptionEventArgs e)
        {
            
            base.OnUnhandledException(e);
        }
        protected override void OnLaunch()//4
        {
            var a = Container.Get<RM3545>();
            var b = Container.Get<TH2882A>();
            var c = Container.Get<PlcService>();
            var d = Container.Get<TH2775B>();
            var e = Container.Get<TH9320>();

            a.PortName = "COM3";
            b.PortName = "COM5";
            d.PortName = "COM4";
            e.PortName = "COM2";
            Task.Run(() => a.Connect());
            Task.Run(() => b.Connect());
            Task.Run(() => c.Connect());
            Task.Run(() => d.Connect());
            Task.Run(() => e.Connect());
            base.OnLaunch();
        }

    }
}
