using System;
using Stylet;
using StyletIoC;
using System.IO.Ports;
using DAQ.Service;
using System.Threading.Tasks;
using DAQ.Pages;
using System.Windows.Threading;

namespace DAQ
{




    public class Bootstrapper : Bootstrapper<MainWindowViewModel>
    {
        protected override void ConfigureIoC(IStyletIoCBuilder builder)//2
        {
            // Configure the IoC container in here
            builder.Bind<IEventAggregator>().To<EventAggregator>().InSingletonScope();
            builder.Bind<HomeViewModel>().ToSelf().InSingletonScope();
            builder.Bind<MsgViewModel>().ToSelf().InSingletonScope();
            builder.Bind<SettingsViewModel>().ToSelf().InSingletonScope();
            builder.Bind<MsgFileSaver<TLog>>().ToSelf();
            builder.Bind<PlcService>().ToSelf().InSingletonScope();
            builder.Bind<PLCViewModel>().ToSelf();
            builder.Bind<MainWindowViewModel>().ToSelf().InSingletonScope();
            builder.Bind<AIP>().ToSelf().InSingletonScope();

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
            var aip = Container.Get<AIP>();
            var c = Container.Get<PlcService>();
            Task.Run(() => c.Connect());
            Task.Run(() => aip.Connect());
            base.OnLaunch();
        }

    }
}
