using GreenPipes;
using MassTransit;
using MG.EventBus.Startup;
using Settings.Stub;
using SimpleInjector;
using Topshelf;

namespace Consumer.WindowsService
{
	class Program
	{
        static readonly Container container;

        static Program()
        {
            container = new Container();
            container.RegisterSendMailConsumerDependencies(SettingsStub.GetSetting());
            container.Verify();
        }

        #region Testing without DI

        //static void Main(string[] args)
        //{
        //    var bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
        //    {
        //        var host = cfg.Host(new System.Uri(@"rabbitmq://barnacle.rmq.cloudamqp.com:5672/oklnbiuq/"), h =>
        //        {
        //            h.Username("oklnbiuq");
        //            h.Password("OD0mXJWCHtbXBt8JADrigXSlebXNORMA");

        //            //h.UseSsl(s =>
        //            //{
        //            //	s.Protocol = System.Security.Authentication.SslProtocols.Tls12;
        //            //});

        //            cfg.ReceiveEndpoint("send-mail", ec =>
        //            {
        //                ec.UseMessageRetry(retry => retry.Interval(5, 1000));
        //                ec.Consumer<MG.EventBus.Components.Consumers.SendMailConsumer>();
        //                ec.Consumer<MG.EventBus.Components.Consumers.FaultSendMailConsumer>();
        //            });

        //            cfg.ReceiveEndpoint("send-mail-lowest", ec =>
        //            {
        //                ec.UseMessageRetry(retry => retry.Interval(5, 1000));
        //                ec.Consumer<MG.EventBus.Components.Consumers.SendMailConsumer>();
        //            });

        //            cfg.ReceiveEndpoint("send-mail-highest", ec =>
        //            {
        //                ec.UseMessageRetry(retry => retry.Interval(5, 1000));
        //                ec.Consumer<MG.EventBus.Components.Consumers.SendMailConsumer>();
        //            });
        //        });
        //    });

        //    HostFactory.Run(x =>
        //    {
        //        x.Service<Consumer>(s =>
        //        {
        //            s.ConstructUsing(name => new Consumer(bus));
        //            s.WhenStarted(c => c.Start());
        //            s.WhenStopped(c => c.Stop());
        //        });
        //        x.RunAsLocalSystem();

        //        x.SetDescription("Consumer Windows Service Prototype");
        //        x.SetDisplayName("Consumer Windows Service");
        //        x.SetServiceName("Consumer Windows Service");
        //    });
        //}

        #endregion

        static void Main(string[] args)
        {
            var bus = container.GetInstance<IBusControl>();

            HostFactory.Run(x =>
            {
                x.Service<Consumer>(s =>
                {
                    s.ConstructUsing(name => new Consumer(bus));
                    s.WhenStarted(c => c.Start());
                    s.WhenStopped(c => c.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("Consumer Windows Service Prototype");
                x.SetDisplayName("Consumer Windows Service");
                x.SetServiceName("Consumer Windows Service");
            });
        }
    }
}
