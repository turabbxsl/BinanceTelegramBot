using Binance.Net.Clients;
using Binance.Net.Interfaces;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using Microsoft.Extensions.Options;
using ResourceHandler.Resources;

namespace ResourceHandler.Services
{
    public interface IBinanceService
    {
        Task<WebCallResult<IBinanceTick>> GetSymbolTickerAsync(string symbol);
        Task<WebCallResult<IEnumerable<IBinanceTick>>> GetSymbolTickersAsync();
    }

    public class BinanceService : IBinanceService
    {
        private readonly BinanceRestClient _client;
        private readonly IOptions<Config> _config;

        public BinanceService(IOptions<Config> config)
        {
            _client = new BinanceRestClient();
            _config = config;
            _client.SetApiCredentials(new ApiCredentials(
                _config.Value.BinanceApiKey,
                _config.Value.BinanceSecretKey
            ));
        }

        public async Task<WebCallResult<IBinanceTick>> GetSymbolTickerAsync(string symbol)
        {
            return await _client.SpotApi.ExchangeData.GetTickerAsync(symbol);
        }

        public async Task<WebCallResult<IEnumerable<IBinanceTick>>> GetSymbolTickersAsync()
        {
            return await _client.SpotApi.ExchangeData.GetTickersAsync();
        }
    }
}
