using TelegramClient.Services;

public class TelegramBot
{
    private readonly IClientService _clientService;

    public TelegramBot(IClientService clientService)
    {
        _clientService = clientService;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Telegram Bot başlatılır...");
        await _clientService.StartReceivingAsync(cancellationToken);
    }
}