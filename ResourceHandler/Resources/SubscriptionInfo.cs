namespace ResourceHandler.Resources
{
    public class SubscriptionInfo
    {
        public decimal EntryPrice { get; set; }
        public TimeSpan Interval { get; set; }
        public DateTime LastChecked { get; set; }
    }
}
