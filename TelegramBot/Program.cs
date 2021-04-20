using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;

namespace TelegramBot
{
    public static class Program
    {
        public static async Task Main()
        {
            var telegramBotClient = new TelegramBotClient(Configuration.BotToken);
            var me = await telegramBotClient.GetMeAsync();
            var cancellationToken = new CancellationTokenSource();
            var telegramBotHandle = new TelegramBotHandle(telegramBotClient);
            Console.Title = me.Username;
            
            // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            telegramBotClient.StartReceiving(new DefaultUpdateHandler(telegramBotHandle.HandleUpdateAsync, TelegramBotHandle.HandleErrorAsync),
                cancellationToken.Token);

            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();

            // Send cancellation request to stop bot
            cancellationToken.Cancel();
        }
    }
}