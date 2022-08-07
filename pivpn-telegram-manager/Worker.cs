using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace PiVPNTelemanager;

public class Worker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var bot = new TelegramBotClient(ConfigData.TelegramBotToken!);
            if (bot.BotId == null) throw new Exception("Invalid TelegramBotToken value");
            var me = await bot.GetMeAsync(cancellationToken: stoppingToken);

            Console.Title = me.Username ?? "PiVPN Telemanager";

            // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            var receiverOptions = new ReceiverOptions()
            {
                AllowedUpdates = Array.Empty<UpdateType>(),
                ThrowPendingUpdates = true
            };

            bot.StartReceiving(updateHandler: UpdateHandlers.HandleUpdateAsync,
                               pollingErrorHandler: UpdateHandlers.PollingErrorHandler,
                               receiverOptions: receiverOptions,
                               cancellationToken: stoppingToken);

            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();
        } 
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex);
            Console.ResetColor();
        }
    }
}
