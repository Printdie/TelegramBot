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
        
        private static async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Message.Text == null) return;
            Console.WriteLine($"Received a text message in chat {e.Message.Chat.Id}.");

            await _botClient.SendTextMessageAsync(
                chatId: e.Message.Chat,
                text: "You said:\n" + e.Message.Text
            );
        }
    }
}