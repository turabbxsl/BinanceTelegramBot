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
        private readonly Dictionary<(long userid, string symbol), DateTime> _lastCheckers = new();

        private readonly TimeSpan _delayInterval = TimeSpan.FromSeconds(10);

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
                var currentSubscriptions = _subscriptions.GetAllSubscriptions();

                var userTasks = currentSubscriptions.Select(user => Task.Run(async () =>
                {
                    long userId = user.Key;
                    var coins = user.Value;

                    foreach (var (symbol, info) in coins)
                    {
                        var latestSubs = _subscriptions.GetAllSubscriptions();
                        if (!latestSubs.TryGetValue(userId, out var userCoins) || !userCoins.ContainsKey(symbol))
                            continue;

                        var key = (userId, symbol);
                        if (_lastCheckers.TryGetValue(key, out var lastChecked))
                            if ((DateTime.UtcNow - lastChecked) < info.Interval)
                                continue;

                        var tickerResult = await _binanceService.GetSymbolTickerAsync(symbol);
                        if (!tickerResult.Success) continue;

                        var currentPrice = tickerResult.Data.LastPrice;

                        if (_lastPrices.TryGetValue(symbol, out var oldPrice))
                        {
                            string msg = string.Empty;
                            if (currentPrice > info.EntryPrice)
                            {
                                var changePercent = ((currentPrice - info.EntryPrice) / info.EntryPrice) * 100;
                                msg = $"📈 {symbol} qiyməti artdı!\n" +
                                      $"Giriş qiymətiniz: {info.EntryPrice}\n" +
                                      $"Hazırda: {currentPrice}\n" +
                                      $"🔼 Artım: +%{changePercent:F2}";
                            }
                            else if (currentPrice < info.EntryPrice)
                            {
                                var changePercent = ((info.EntryPrice - currentPrice) / info.EntryPrice) * 100;
                                msg = $"📉 {symbol} qiyməti azaldı!\n" +
                                      $"Giriş qiymətiniz: {info.EntryPrice}\n" +
                                      $"Hazırda: {currentPrice}\n" +
                                      $"🔽 Azalma: -%{changePercent:F2}";
                            }

                            if (!string.IsNullOrEmpty(msg))
                                await _botClient.SendMessageAsync(userId, msg);
                        }

                        _lastPrices[symbol] = currentPrice;
                        _lastCheckers[key] = DateTime.UtcNow;
                        info.LastChecked = DateTime.UtcNow;
                    }
                }));

                await Task.WhenAll(userTasks);

                await Task.Delay(_delayInterval, stoppingToken);
            }
        }


    }
}
