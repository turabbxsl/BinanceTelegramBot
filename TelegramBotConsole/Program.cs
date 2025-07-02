using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ResourceHandler.Resources;
using ResourceHandler.Resources.Helper;
using ResourceHandler.Services;
using TelegramClient.Services;


internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = new HostBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json");
            })
    .ConfigureServices((context, services) =>
    {
        services.Configure<Config>(context.Configuration.GetSection("Config"));

        services.AddSingleton<IBinanceService, BinanceService>();
        services.AddSingleton<TelegramBot>();
        services.AddSingleton<SubscriptionStore>();
        services.AddSingleton<IClientService, ClientService>();

    });

        var host = builder.Build();

        var bot = host.Services.GetRequiredService<TelegramBot>();
        var config = host.Services.GetRequiredService<IOptions<Config>>().Value;

        MyStaticHelpers.Initialize(config);

        await bot.StartAsync();

        Console.ReadKey();
    }
}