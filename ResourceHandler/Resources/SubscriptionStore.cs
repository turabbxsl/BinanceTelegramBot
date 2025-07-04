using Newtonsoft.Json.Linq;
using ResourceHandler.Services;

namespace ResourceHandler.Resources
{
    public class SubscriptionStore
    {

        private readonly string _filePath;
        private readonly IBinanceService _binanceService;
        private Dictionary<long, Dictionary<string, SubscriptionInfo>> _subscriptions;

        public SubscriptionStore(IBinanceService binanceService)
        {
            var dllFolder = Environment.CurrentDirectory;
            _filePath = Path.Combine(@"YOUR_FILE_PATH/", "YOUR_SUBSCRIPTIONS_JSON_FILENAME.json");

            _subscriptions = LoadFromFile();
            _binanceService = binanceService;
        }

        public Dictionary<long, Dictionary<string, SubscriptionInfo>> LoadFromFile()
        {
            if (!File.Exists(_filePath))
                return new Dictionary<long, Dictionary<string, SubscriptionInfo>>();

            try
            {
                var json = File.ReadAllText(_filePath);
                var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<long, List<KeyValuePair<string, SubscriptionInfo>>>>(json);
                var result = data.ToDictionary(
                                                kvp => kvp.Key,
                                                kvp => kvp.Value.ToDictionary(x => x.Key, x => x.Value)
                                              );
                return result ?? new Dictionary<long, Dictionary<string, SubscriptionInfo>>();
            }
            catch (Exception)
            {
                return new Dictionary<long, Dictionary<string, SubscriptionInfo>>();
            }

        }

        public void SaveToFile()
        {
            var dict = _subscriptions.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList());
            File.WriteAllText(_filePath, System.Text.Json.JsonSerializer.Serialize(dict));
        }

        public void Subscribe(long userId, string symbol, decimal baseLinePrice, TimeSpan interval)
        {
            symbol = symbol.ToUpperInvariant();
            if (!_subscriptions.ContainsKey(userId))
                _subscriptions[userId] = new Dictionary<string, SubscriptionInfo>();

            _subscriptions[userId][symbol] = new SubscriptionInfo()
            {
                EntryPrice = baseLinePrice,
                Interval = interval,
                LastChecked = DateTime.MinValue
            };

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

        public Dictionary<string, SubscriptionInfo> GetSubscribersByUser(long userId)
        {
            if (_subscriptions.TryGetValue(userId, out var symbols))
                return symbols;

            return new Dictionary<string, SubscriptionInfo>();
        }

        public IEnumerable<string> GetAllSymbols()
        {
            _subscriptions = LoadFromFile();
            return _subscriptions.Values.SelectMany(dict => dict.Keys).Distinct();
        }

        public Dictionary<long, Dictionary<string, SubscriptionInfo>> GetAllSubscriptions()
        {
            _subscriptions = LoadFromFile();
            return _subscriptions;
        }
    }
}
