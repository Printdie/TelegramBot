using System;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace TelegramBot
{
    public static class Program
    {
        private static ITelegramBotClient _botClient;

        public static void Main()
        {
            _botClient = new TelegramBotClient("1799435878:AAG2PkXn1VIDw2n2qNAkiXSQv3JDxb9YcYQ");
            var me = _botClient.GetMeAsync().Result;
            Console.WriteLine($"Hello, World! I am user {me.Id} and my name is {me.FirstName}.");

            _botClient.OnMessage += Bot_OnMessage;
            _botClient.StartReceiving();
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            _botClient.StopReceiving();
        }
        
        private static async void Bot_OnMessage(object sender, MessageEventArgs messageEvent)
        {
            if (messageEvent.Message.Text == null) return;
            Console.WriteLine($"Received a text message in chat {messageEvent.Message.Chat.Id}.");
            await _botClient.SendTextMessageAsync(messageEvent.Message.Chat, "You said:\n" + messageEvent.Message.Text);
        }
    }
}