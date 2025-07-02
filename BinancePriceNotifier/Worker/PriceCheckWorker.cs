using CryptoExchange.Net.Sockets;
using Microsoft.Extensions.Hosting;
using ResourceHandler.Resources;
using ResourceHandler.Services;
using TelegramClient.Services;

namespace BinancePriceNotifier.Worker
{
    public class PriceCheckWorker : BackgroundService
    {
        private readonly SubscriptionStore _subscriptions;
        private readonly IClientService _botClient;
        private readonly IBinanceService _binanceService;
        private readonly Dictionary<string, decimal> _lastPrices = new();


        public PriceCheckWorker(IClientService botClient, SubscriptionStore subscriptions, IBinanceService binanceService)
        {
            _botClient = botClient;
            _binanceService = binanceService;
            _subscriptions = subscriptions;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var symbols = _subscriptions.GetAllSymbols();
                var subscribtions = _subscriptions.GetAllSubscriptions();

                foreach (var user in subscribtions)
                {
                    long userId = user.Key;
                    var coins = user.Value;

                    foreach (var (symbol, baselinePrice) in coins)
                    {
                        var currentSubscriptions = _subscriptions.GetAllSubscriptions();

                        if (!currentSubscriptions.TryGetValue(userId, out var userCoins) || !userCoins.ContainsKey(symbol))
                            continue;

                        var tickerResult = await _binanceService.GetSymbolTickerAsync(symbol);

                        if (tickerResult.Success)
                        {
                            var price = tickerResult.Data.LastPrice;
                            var msg = string.Empty;
                            if (_lastPrices.TryGetValue(symbol, out var newPrice))
                            {
                                if (newPrice > baselinePrice)
                                {
                                    var changePercent = ((newPrice - baselinePrice) / baselinePrice) * 100;

                                    msg = $"📈 {symbol} qiyməti artdı!\n" +
                                          $"Giriş qiymətiniz: {baselinePrice}\n" +
                                          $"Hazırda: {newPrice}\n" +
                                          $"🔼 Artım: +%{changePercent:F4}";

                                    await _botClient.SendMessageAsync(userId, msg);
                                }
                                else if (newPrice < baselinePrice)
                                {
                                    var changePercent = ((baselinePrice - newPrice) / baselinePrice) * 100;

                                    msg = $"📉 {symbol} qiyməti azaldı!\n" +
                                          $"Giriş qiymətiniz: {baselinePrice}\n" +
                                          $"Hazırda: {newPrice}\n" +
                                          $"🔽 Azalma: -%{changePercent:F4}";
                                    await _botClient.SendMessageAsync(userId, msg);
                                }
                            }
                            _lastPrices[symbol] = price;

                            await Task.Delay(5000, stoppingToken);
                        }

                        await Task.Delay(15000, stoppingToken);

                    }
                }


            }
        }




    }
}
