using System;
using Stylet;
using StyletIoC;
using System.IO.Ports;
using DAQ.Service;
using System.Threading.Tasks;
using DAQ.Pages;

namespace DAQ
{
    public class Bootstrapper : Bootstrapper<MainWindowViewModel>
    {
        protected override void ConfigureIoC(IStyletIoCBuilder builder)//2
        {
            // Configure the IoC container in here
            builder.Bind<IEventAggregator>().To<EventAggregator>().InSingletonScope();
            builder.Bind<PortAService>().ToSelf().InSingletonScope();
            builder.Bind<PortBService>().ToSelf().InSingletonScope();
            builder.Bind<HomeViewModel>().ToSelf().InSingletonScope();
            builder.Bind<MsgViewModel>().ToSelf().InSingletonScope();
            builder.Bind<SettingsViewModel>().ToSelf().InSingletonScope();
            builder.Bind<MsgDBSaver>().ToSelf().InSingletonScope();
            builder.Bind<MsgFileSaver<TestSpecViewModel>>().ToSelf().InSingletonScope();
            builder.Bind<PlcService>().ToSelf().InSingletonScope();
            builder.Bind<PLCViewModel>().ToSelf();
         //   builder.Bind<IMsgSaver>().To<MsgSaver<>>().InSingletonScope();
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
            var a = Container.Get<PortAService>();
            var b = Container.Get<PortBService>();
            var c = Container.Get<PlcService>();
            Task.Run(() => a.Connect());
            Task.Run(() => b.Connect());
            Task.Run(() => c.Connect());

            base.OnLaunch();
        }

    }
}
