namespace ResourceHandler.Resources
{
    public class Config
    {
        public char CommandPrefix { get; set; }
        public string? API_KEY { get; set; }
        public string? BinanceApiKey { get; set; }
        public string? BinanceSecretKey { get; set; }
        public bool Bot_Status { get; set; }
        public long Chat_Id { get; set; }
        public string? AvailableCommandsText { get; set; }
        public List<string>? Available_Commands { get; set; }
    }
}
