using BinancePriceNotifier.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResourceHandler.Resources;
using ResourceHandler.Services;
using TelegramClient.Services;

namespace BinancePriceNotifier
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(config => {
                    config.AddJsonFile("appsettings.json");
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<IClientService, ClientService>();
                    services.AddSingleton<IBinanceService, BinanceService>();
                    services.AddTransient<SubscriptionStore>();

                    services.Configure<Config>(context.Configuration.GetSection("Config"));

                    services.AddHostedService<PriceCheckWorker>();
                })
                .Build();

            await host.RunAsync();
        }
    }
}
