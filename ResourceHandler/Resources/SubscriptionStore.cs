using Newtonsoft.Json.Linq;
using ResourceHandler.Services;

namespace ResourceHandler.Resources
{
    public class SubscriptionStore
    {

        private readonly string _filePath;
        private readonly IBinanceService _binanceService;
        private Dictionary<long, Dictionary<string, decimal>> _subscriptions;

        public SubscriptionStore(IBinanceService binanceService)
        {
            var dllFolder = Environment.CurrentDirectory;
            _filePath = Path.Combine(@"C:\Users\t.bakhishli\Downloads\TelegramBot-master\TelegramBot-master\ResourceHandler\", "subscriptions.json");

            _subscriptions = LoadFromFile();
            _binanceService = binanceService;
        }

        public Dictionary<long, Dictionary<string, decimal>> LoadFromFile()
        {
            if (!File.Exists(_filePath))
                return new Dictionary<long, Dictionary<string, decimal>>();

            try
            {
                var json = File.ReadAllText(_filePath);
                var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<long, List<KeyValuePair<string, decimal>>>>(json);
                var result = data.ToDictionary(
                                                kvp => kvp.Key,
                                                kvp => kvp.Value.ToDictionary(x => x.Key, x => x.Value)
                                              );
                return result ?? new Dictionary<long, Dictionary<string, decimal>>();
            }
            catch (Exception)
            {
                return new Dictionary<long, Dictionary<string, decimal>>();
            }

        }

        public void SaveToFile()
        {
            var dict = _subscriptions.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList());
            File.WriteAllText(_filePath, System.Text.Json.JsonSerializer.Serialize(dict));
        }

        public void Subscribe(long userId, string symbol, decimal baseLinePrice)
        {
            symbol = symbol.ToUpperInvariant();
            if (!_subscriptions.ContainsKey(userId))
                _subscriptions[userId] = new Dictionary<string, decimal>();

            if (!_subscriptions[userId].ContainsKey(symbol))
                _subscriptions[userId].Add(symbol, baseLinePrice);

            SaveToFile();
            _subscriptions = LoadFromFile();
        }

        public void UnSubscribe(long userId, string symbol)
        {
            symbol = symbol.ToUpperInvariant();
            if (_subscriptions[userId].ContainsKey(symbol))
                _subscriptions[userId].Remove(symbol);

            SaveToFile();
            _subscriptions = LoadFromFile();
        }

        public IEnumerable<long> GetSubscribersBySymbol(string symbol)
        {
            return _subscriptions
                .Where(kvp => kvp.Value.ContainsKey(symbol.ToUpperInvariant()))
                .Select(kvp => kvp.Key);
        }

        public Dictionary<string, decimal> GetSubscribersByUser(long userId)
        {
            if (_subscriptions.TryGetValue(userId, out var symbols))
                return symbols;

            return new Dictionary<string, decimal>();
        }

        public IEnumerable<string> GetAllSymbols()
        {
            _subscriptions = LoadFromFile();
            return _subscriptions.Values.SelectMany(dict => dict.Keys).Distinct();
        }

        public Dictionary<long, Dictionary<string, decimal>> GetAllSubscriptions()
        {
            _subscriptions = LoadFromFile();
            return _subscriptions;
        }
    }
}
