using ResourceHandler.Resources.Models.TelegramBot;
using System.Text.RegularExpressions;

namespace ResourceHandler.Resources.Helper
{
    public class MyStaticHelpers
    {
        private static Config? _config;

        public static void Initialize(Config config)
        {
            _config = config;
        }

        public static long? GetChatId()
        {
            return _config?.Chat_Id;
        }

        public static string GetApiKey()
        {
            return _config?.BinanceApiKey ?? "";
        }

        public static bool IsBotEnabled()
        {
            return _config?.Bot_Status ?? false;
        }

        public static string? GetAvailableCommandsText()
        {
            return _config?.AvailableCommandsText;
        }

        public static CommandModel CheckMessage(string? commandString)
        {
            CommandModel commandModel = new CommandModel();

            commandModel.CommandText = commandString;
            commandModel.CommandSections = PrepareCommandSections(commandString);
            commandModel.CommandIsAvailable = CheckIfCommandAvailable(commandModel.CommandSections);
            commandModel.Command = commandModel.CommandIsAvailable ? commandModel.CommandSections[1] : String.Empty;
            commandModel.CommandHasParameter = commandModel.CommandIsAvailable && commandModel.CommandSections.Count() > 2;
            commandModel.CommandParameter = commandModel.CommandHasParameter ? commandModel.CommandSections[2] : String.Empty;

            return commandModel;
        }

        public static bool CheckIfCommandAvailable(string[] commandSections)
        {
            return MyStaticHelpers.GetAvailableCommandsText().Contains(commandSections[1]);
        }

        public static string[] PrepareCommandSections(string commandString)
        {
            var returnArray = new List<string>();
            returnArray.Add(commandString.Substring(0, 1));

            var remaining = commandString.Substring(1);

            var parts = remaining.Contains(' ')
                ? remaining.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                : remaining.Split('-', StringSplitOptions.RemoveEmptyEntries);

            returnArray.AddRange(parts);
            return returnArray.ToArray();
        }

        public static bool TryParseInterval(string input, out TimeSpan interval)
        {
            interval = TimeSpan.Zero;

            var match = Regex.Match(input, @"^\d{1,3}$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                int minutes = int.Parse(match.Groups[0].Value);
                interval = TimeSpan.FromMinutes(minutes);
                return true;
            }
            return false;
        }
    }
}
