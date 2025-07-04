using CryptoExchange.Net.CommonObjects;
using Microsoft.Extensions.Options;
using ResourceHandler.Resources;
using ResourceHandler.Resources.Enums;
using ResourceHandler.Resources.Helper;
using ResourceHandler.Resources.Models.TelegramBot;
using ResourceHandler.Services;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramClient.Services
{
    public interface IClientService
    {
        Task SendMessageAsync(long chatId, string message);
        Task StartReceivingAsync(CancellationToken cancellationToken = default);
    }

    public class ClientService : IClientService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IBinanceService _binanceService;
        private readonly IOptions<Config> _config;
        private readonly SubscriptionStore _subscriptionStore;

        public ClientService(IOptions<Config> config, SubscriptionStore subscriptionStore, IBinanceService binanceService)
        {
            _config = config;
            _subscriptionStore = subscriptionStore;
            _botClient = new TelegramBotClient(_config.Value.API_KEY);
            _binanceService = binanceService;
        }

        public async Task SendMessageAsync(long chatId, string message)
        {
            await _botClient.SendTextMessageAsync(chatId, message);
        }

        public async Task StartReceivingAsync(CancellationToken cancellationToken = default)
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new UpdateType[]
                      {
                    UpdateType.Message
                      },
                ThrowPendingUpdates = true
            };
            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken);

            var me = await _botClient.GetMeAsync();
            Console.WriteLine($"Bot started: @{me.Username}");
        }

        private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
        {
            try
            {
                var message = update.Message;

                if (update.Type != UpdateType.Message)
                    return;
                if (message!.Type != MessageType.Text)
                    return;


                long chatId = message.Chat.Id;
                var userId = message.From.Id;
                var text = message.Text.Trim();

                if (!MyStaticHelpers.IsBotEnabled())
                    await SendMessageAsync(chatId, "Üzrlü say, hazırda aktiv deyiləm");
                else
                {
                    CommandModel commandModel = MyStaticHelpers.CheckMessage(text);
                    if (!commandModel.CommandIsAvailable) { await SendMessageAsync(chatId, "Verilən command bilinmir.Zəhmət olmasa /help vasitəsi ilə mövcud commandlardan seçin"); return; }

                    string? username = update.Message.Chat.Username;

                    if (Enum.TryParse(commandModel.Command.ToUpper(), out Enums.Commands enumCommand) && Enum.IsDefined(typeof(Enums.Commands), enumCommand))
                    {
                        switch (enumCommand)
                        {
                            case Enums.Commands.ERROR:
                                {
                                    await SendMessageAsync(chatId,
                                                         "❌ Əmr işlənərkən xəta baş verdi.\n" +
                                                         "Zəhmət olmasa əmr formatını düzgün daxil etdiyinizə əmin olun.\n\n" +
                                                         "ℹ️ /help yazaraq bütün mövcud əmrlərlə tanış ola bilərsiniz.");
                                }
                                break;
                            case Enums.Commands.TUBIN:
                                {
                                    chatId = update.Message.Chat.Id;
                                    StringBuilder sb = new StringBuilder();

                                    if (text.StartsWith("/tubin-"))
                                    {
                                        var coin = text.Replace("/tubin-", "").ToUpper();
                                        var tradeResult = await _binanceService.GetSymbolTickerAsync(coin);

                                        if (!tradeResult.Success)
                                            await SendMessageAsync(chatId, $@"error\n{tradeResult.Error?.Message}");
                                        else
                                        {
                                            var trade = tradeResult.Data;
                                            sb.AppendLine(new string('-', 39));
                                            sb.AppendLine($"Simvol: {trade.Symbol}");
                                            sb.AppendLine($"Açılış Vaxtı: {trade.OpenTime.ToLocalTime():dd.MM.yyyy HH:mm:ss}");
                                            sb.AppendLine($"Açılış Qiymət: {trade.OpenPrice}");
                                            sb.AppendLine($"Son Qiymət: {trade.LastPrice}");
                                            sb.AppendLine($"Qiymət Dəyişikliyi (24H): {trade.PriceChange}");
                                            sb.AppendLine($"Faizlə Dəyişikliyi (24H): {trade.PriceChangePercent}");
                                            sb.AppendLine($"Ən Yüksək Qiymət (24H): {trade.HighPrice}");
                                            sb.AppendLine($"Ən Aşağı Qiymət (24H):: {trade.LowPrice}");
                                            sb.AppendLine($"Həcm (24H): {trade.Volume}");
                                            sb.AppendLine($"Kvit Həcmi (24H): {trade.QuoteVolume}");
                                            sb.AppendLine($"Bağlanış Vaxtı: {trade.CloseTime.ToLocalTime():dd.MM.yyyy HH:mm:ss}");
                                            sb.AppendLine(new string('-', 39));
                                        }

                                        await SendMessageAsync(chatId, sb.ToString());
                                    }
                                    else
                                        await SendMessageAsync(chatId, $@"❌ Command düzgün verilməyib");
                                }
                                break;
                            case Enums.Commands.SUBSCRIBE:
                                {
                                    var parts = text.Split(' ', 3);

                                    if (parts.Length < 3)
                                    {
                                        await SendMessageAsync(chatId, "Səhv Command. Nümunə: /subscribe coin interval");
                                        return;
                                    }

                                    var coin = parts[1].ToUpperInvariant();

                                    if (!Regex.IsMatch(coin, @"^[A-Z]+USDT$", RegexOptions.IgnoreCase))
                                    {
                                        await SendMessageAsync(chatId, $"❌ '{coin}' simvolu mövcud deyil və ya USDT ilə bitmir.");
                                        return;
                                    }

                                    var interval = parts[2].ToUpperInvariant();
                                    if (!MyStaticHelpers.TryParseInterval(interval, out var correctInterval))
                                    {
                                        await SendMessageAsync(chatId, $"❌ '{interval}' interval düzgün formatda deyil.\n\n" +
                                                                       $"✅ Doğru format: yalnız dəqiqə olaraq **rəqəm** yazılmalıdır.\n" +
                                                                       $"📌 Nümunə: `/subscribe BTCUSDT 5`\n" +
                                                                       $"🔸 Qeyd: Dəqiqə dəyəri maksimum **3 rəqəmli** ola bilər (məsələn: 1, 15, 120).");
                                        return;
                                    }

                                    var coinResult = await _binanceService.GetSymbolTickerAsync(coin);

                                    if (coinResult.Success)
                                    {
                                        _subscriptionStore.Subscribe(chatId, coin, coinResult.Data.LastPrice, correctInterval);
                                        await SendMessageAsync(chatId,
                                            $"✅ {coin} üçün abunəlik yaradıldı.\n" +
                                            $"💰 Giriş qiymətiniz: {coinResult.Data.LastPrice} USD\n" +
                                            $"⏱ Bildiriş intervalı: {interval} dəqiqə");
                                    }
                                    else
                                        await SendMessageAsync(chatId, $@"❌ Command düzgün verilməyib");
                                }
                                break;
                            case Enums.Commands.UNSUBSCRIBE:
                                {
                                    if (message.Text.StartsWith("/unsubscribe"))
                                    {

                                        var parts = text.Split(' ', 2);

                                        if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
                                        {
                                            await SendMessageAsync(chatId, "Səhv Command. Nümunə: /unsubscribe btcusdt");
                                            return;
                                        }

                                        var coin = parts[1].ToUpperInvariant();

                                        _subscriptionStore.UnSubscribe(chatId, coin);
                                        await SendMessageAsync(chatId, $"{coin} Abunəlik ləğv edildi");
                                    }
                                    else
                                        await SendMessageAsync(chatId, $@"❌ Command düzgün verilməyib");
                                }
                                break;
                            case Enums.Commands.SUBSCRIBES:
                                {
                                    var coinList = _subscriptionStore.GetSubscribersByUser(chatId);

                                    if (!coinList.Any())
                                        await SendMessageAsync(chatId, $"Abunəliyiniz yoxdur");
                                    else
                                    {
                                        var msg = "📋 Abunə olduğunuz coinlər:\n\n" +
                                                      string.Join("\n", coinList.Select(c =>
                                                          $"• {c.Key}\n" +
                                                          $"   • Giriş qiyməti: {c.Value.EntryPrice} USD\n" +
                                                          $"   • Yoxlama intervalı: {c.Value.Interval.TotalMinutes} dəq."));
                                        await SendMessageAsync(chatId, msg);
                                    }

                                }
                                break;
                            case Enums.Commands.HELP:
                                {
                                    var tradeResult = await _binanceService.GetSymbolTickersAsync();

                                    var usdtSymbols = tradeResult.Data
                                    .Where(x => x.Symbol.EndsWith("USDT"))
                                    .Select(x => x.Symbol)
                                    .Take(10)
                                    .ToList();

                                    var sb = new StringBuilder();
                                    sb.AppendLine("🤖 *Əmrlər üzrə yardım*");
                                    sb.AppendLine("Aşağıdakı əmrləri istifadə edərək bot ilə qarşılıqlı əlaqə qura bilərsiniz:\n");

                                    sb.AppendLine("📋 *Əsas Əmrlər:*");
                                    sb.AppendLine("/help - Əmrlər haqqında məlumat.");
                                    sb.AppendLine("/tubin [coin] - Binance üzərindən coin haqqında məlumat al.");
                                    sb.AppendLine("/subscribe [coin] [dəqiqə] - Coin üçün qiymət abunəliyi (məsələn: /subscribe BTCUSDT 5).");
                                    sb.AppendLine("/unsubscribe [coin] - Coin üçün abunəliyi ləğv et.");
                                    sb.AppendLine("/subscribes - Hazırda abunə olduğunuz coinlərin siyahısını göstər.\n");

                                    sb.AppendLine("⏱ *Interval haqqında:*");
                                    sb.AppendLine("- /subscribe əmri ilə birlikdə **dəqiqə** formatında interval yazmalısınız.");
                                    sb.AppendLine("- Məsələn: `/subscribe BTCUSDT 10` → hər 10 dəqiqədən bir qiymət yoxlanacaq.");
                                    sb.AppendLine("- Yalnız rəqəm yazılmalıdır və maksimum 3 rəqəmli ola bilər.\n");

                                    sb.AppendLine("💰 *Mövcud USDT Coin-lər:*");

                                    foreach (var symbol in usdtSymbols)
                                    {
                                        sb.AppendLine($"• {symbol.ToUpper()}");
                                    }

                                    await SendMessageAsync(chatId,
                                        $"{sb.ToString()}");
                                }
                                break;
                            default:
                                {
                                    await SendMessageAsync(chatId, "Verilən command bilinmir.Zəhmət olmasa /help vasitəsi ilə mövcud commandlardan seçin");
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await SendMessageAsync(update.Message.Chat.Id, $"Xəta baş verdi: {ex.Message}");
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken cancellationToken)
        {
            var errMsg = ex switch
            {
                ApiRequestException apiEx => $"Telegram API Error: {apiEx.Message}",
                _ => ex.ToString()
            };

            Console.WriteLine(errMsg);
            return Task.CompletedTask;
        }

    }
}
