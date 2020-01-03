using System;
using Stylet;
using StyletIoC;
using System.IO.Ports;

using System.Threading.Tasks;
using DAQ.Pages;
using HslCommunication.Core;
using HslCommunication.Profinet.Omron;
using HslCommunication.Profinet.Siemens;

namespace DAQ
{

    public interface IReadWriteFactory
    {
        IReadWriteNet GetReadWriteNet();
        string AddressA { get; }
        string AddressB { get; }
        string CameraDataAddr { get; }
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
        public string CameraDataAddr => "D8010";
    }


    public class SiemensPLCFactory : IReadWriteFactory
    {
        public IReadWriteNet GetReadWriteNet()
        {
            return new SiemensS7Net(SiemensPLCS.S1500, Properties.Settings.Default.PLC_IP);
        }
        public string AddressA => "M8000";
        public string AddressB => "M8002";
        public string CameraDataAddr => "M8010";
    }




    public class Bootstrapper : Bootstrapper<MainWindowViewModel>
    {
        protected override void ConfigureIoC(IStyletIoCBuilder builder)//2
        {
            // Configure the IoC container in here
            builder.Bind<IEventAggregator>().To<EventAggregator>().InSingletonScope();
            builder.Bind<IConfigureFile>().To<ConfigureFile>().InSingletonScope();
            builder.Bind<HomeViewModel>().ToSelf().InSingletonScope();
            builder.Bind<MsgViewModel>().ToSelf().InSingletonScope();
            builder.Bind<SettingsViewModel>().ToSelf().InSingletonScope();
            builder.Bind<PLCViewModel>().ToSelf();
            builder.Bind<MainWindowViewModel>().ToSelf().InSingletonScope();
            builder.Bind<IQueueProcesser<RecordDto>>().To<MsgFileSaver<RecordDto>>();
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
        protected override void OnLaunch()//4
        {

            base.OnLaunch();
        }

    }
}
