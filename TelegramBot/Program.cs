using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot
{
    public static class Program
    {
        private static TelegramBotClient Bot;
        private static bool IsRequest;
        private static int Count = 7;
        private static List<object> Data = new List<object>();
        private static string ListName;
        private static readonly string[] Props = new[] {
            "Опиши свои навыки:",
            "Введи свой email:",
            "Введи свой номер телефона:",
            "Введи город, в который ты хочешь подать заявку:",
            "Введи ФИО:",
            "Опиши свои компетенции:"
        };
        
        public static async Task Main()
        {
            Bot = new TelegramBotClient(Configuration.BotToken);
            var me = await Bot.GetMeAsync();
            var cancellationToken = new CancellationTokenSource();
            Console.Title = me.Username;
            
            Bot.StartReceiving(
                new DefaultUpdateHandler(HandleUpdateAsync, HandleErrorAsync),
                cancellationToken.Token
            );

            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();
            cancellationToken.Cancel();
        }

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var handler = update.Type switch
            {
                UpdateType.Message => BotOnMessageReceived(update.Message),
                UpdateType.EditedMessage => BotOnMessageReceived(update.Message),
                UpdateType.CallbackQuery => BotOnCallbackQueryReceived(update.CallbackQuery),
                UpdateType.InlineQuery => BotOnInlineQueryReceived(update.InlineQuery),
                UpdateType.ChosenInlineResult => BotOnChosenInlineResultReceived(update.ChosenInlineResult),
                _ => UnknownUpdateHandlerAsync(update)
            };


            Console.WriteLine(update.Type);
            
            try
            {
                await handler;
            }
            
            catch (Exception exception)
            {
                await HandleErrorAsync(botClient, exception, cancellationToken);
            }
        }

        private static async Task BotOnMessageReceived(Message message)
        {
            Console.WriteLine($"Receive message type: {message.Type}");
            if (message.Type != MessageType.Text) return;

            if (IsRequest)
            {
                if (Count == -1)
                {
                    IsRequest = false;
                    Count = 7;
                    GoogleSheetsInterference.AppendList(ListName, Data);
                    
                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[] {InlineKeyboardButton.WithCallbackData("Назад", "/internships")}
                    });
                    
                    await Bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text:  $"Заявка на вакансию {ListName} успешно отправлена!",
                        replyMarkup: keyboard
                    );
                    
                    
                    Data = new List<object>();
                    ListName = null;
                }

                else
                {
                    if (Count == 7)
                    {
                        Console.WriteLine("Первое вхожедние");
                        Data = new List<object> {message.Chat.Username, message.Date.ToString(CultureInfo.InvariantCulture)};
                        Count = 5;
                    }

                    Data.Add(message.Text);
                    
                    Console.WriteLine(Count);

                    await Bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text:  $"{Props[Count]}"
                    );
                
                    Count--;
                }
            }

            else
            {
                var action = message.Text.Split(' ').First() switch
                {
                    "/start" => StartMessage(message),
                    _ => StartMessage(message)
                };
            
                await action;
            }
            
            static async Task StartMessage(Message message)
            {
                await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] {InlineKeyboardButton.WithCallbackData("Доступные стажировки", "/internships")},
                    new[] {InlineKeyboardButton.WithCallbackData("FAQ", "/faq")},
                    new[] {InlineKeyboardButton.WithCallbackData("Правила приёма", "/rules")},
                    new[] {InlineKeyboardButton.WithCallbackData("Мои заявки", "/reqests")}
                });
                
                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text:  GoogleSheetsInterference.GetHelloMessage()
                );
                
                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Мои команды:",
                    replyMarkup: inlineKeyboard
                );
            }
        }
        
        private static async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery)
        {
            var baseKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[] {InlineKeyboardButton.WithCallbackData("Доступные стажировки", "/internships")},
                new[] {InlineKeyboardButton.WithCallbackData("FAQ", "/faq")},
                new[] {InlineKeyboardButton.WithCallbackData("Правила приёма", "/rules")},
                new[] {InlineKeyboardButton.WithCallbackData("Мои заявки", "/reqests")}
            });
            
            var internshipsKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[] {InlineKeyboardButton.WithCallbackData("Java Разаработчик", "/developer")},
                new[] {InlineKeyboardButton.WithCallbackData("Тестировщик", "/tester")},
                new[] {InlineKeyboardButton.WithCallbackData("Аналитик", "/analyst")},
                new[] {InlineKeyboardButton.WithCallbackData("Технический Писатель", "/writer")},
                new[] {InlineKeyboardButton.WithCallbackData("Назад", "/home")}
            });
            
            var developerKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[] {InlineKeyboardButton.WithCallbackData("Отправить заявку", "/sendDeveloper")},
                new[] {InlineKeyboardButton.WithCallbackData("Назад", "/internships")}
            });
            
            var testerKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[] {InlineKeyboardButton.WithCallbackData("Отправить заявку", "/sendTester")},
                new[] {InlineKeyboardButton.WithCallbackData("Назад", "/internships")}
            });
            
            var analystKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[] {InlineKeyboardButton.WithCallbackData("Отправить заявку", "/sendAnalyst")},
                new[] {InlineKeyboardButton.WithCallbackData("Назад", "/internships")}
            });
            
            var writerKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[] {InlineKeyboardButton.WithCallbackData("Отправить заявку", "/sendWriter")},
                new[] {InlineKeyboardButton.WithCallbackData("Назад", "/internships")}
            });
            
            var internships = GoogleSheetsInterference.GetAllAvailableInternships();
            
            switch (callbackQuery.Data)
            {
                case "/rules":
                {
                    await Bot.EditMessageTextAsync(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: GoogleSheetsInterference.GetRules(),
                        replyMarkup: baseKeyboard,
                        messageId: callbackQuery.Message.MessageId);
                    break;
                }
                
                case "/faq":
                {
                    await Bot.EditMessageTextAsync(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: GoogleSheetsInterference.GetFaq(),
                        replyMarkup: baseKeyboard,
                        messageId: callbackQuery.Message.MessageId);
                    break;
                }
                
                case "/home":
                {
                    await Bot.EditMessageTextAsync(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: "Мои команды:",
                        replyMarkup: baseKeyboard,
                        messageId: callbackQuery.Message.MessageId);
                    break;
                }
                
                case "/internships":
                {
                    var text = internships.Count == 0 
                        ? "К сожалению, доступных стажировок не найдено. Обязательно возвращайтесь позже."
                        : "В данный момент имеются следующие направления стажировок:\n";

                    await Bot.EditMessageTextAsync(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: text,
                        replyMarkup: internshipsKeyboard,
                        messageId: callbackQuery.Message.MessageId);
                    break;
                }

                case "/developer":
                {
                    var text = GoogleSheetsInterference.GetDescription("Java Разработчик");
                    
                    await Bot.EditMessageTextAsync(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: text,
                        replyMarkup: developerKeyboard,
                        messageId: callbackQuery.Message.MessageId);
                    break;
                }
                
                case "/tester":
                {
                    var text = GoogleSheetsInterference.GetDescription("Тестировщик");
                    
                    await Bot.EditMessageTextAsync(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: text,
                        replyMarkup: testerKeyboard,
                        messageId: callbackQuery.Message.MessageId);
                    break;
                }
                
                case "/analyst":
                {
                    var text = GoogleSheetsInterference.GetDescription("Аналитик");
                    
                    await Bot.EditMessageTextAsync(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: text,
                        replyMarkup: analystKeyboard,
                        messageId: callbackQuery.Message.MessageId);
                    break;
                }
                
                case "/writer":
                {
                    var text = GoogleSheetsInterference.GetDescription("Технический Писатель");
                    
                    await Bot.EditMessageTextAsync(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: text,
                        replyMarkup: writerKeyboard,
                        messageId: callbackQuery.Message.MessageId);
                    break;
                }

                case "/sendDeveloper":
                {
                    ListName = "Java Разработчик";
                    Count = 7;
                    IsRequest = true;
                    
                    await Bot.SendTextMessageAsync(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: "Загрузи тестовое задание на Google Drive и отправь мне ссылку на него:"
                    );
                    
                    break;
                }
                
                case "/sendTester":
                {
                    ListName = "Тестировщик";
                    Count = 7;
                    IsRequest = true;
                    
                    await Bot.SendTextMessageAsync(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: "Загрузи тестовое задание на Google Drive и отправь мне ссылку на него:"
                    );
                    break;
                }

                case "/sendAnalyst":
                {
                    ListName = "Аналитик";
                    Count = 7;
                    IsRequest = true;
                    
                    await Bot.SendTextMessageAsync(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: "Загрузи тестовое задание на Google Drive и отправь мне ссылку на него:"
                    );
                    break;
                }

                case "/sendWriter":
                {
                    ListName = "Технический Писатель";
                    Count = 7;
                    IsRequest = true;
                    
                    await Bot.SendTextMessageAsync(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: "Загрузи тестовое задание на Google Drive и отправь мне ссылку на него:"
                    );
                    break;
                }

            }

            await Bot.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                $"Received {callbackQuery.Data}"
            );
        }
        
        private static async Task BotOnInlineQueryReceived(InlineQuery inlineQuery)
        {
            Console.WriteLine($"Received inline query from: {inlineQuery.From.Id}");

            InlineQueryResultBase[] results = {
                // displayed result
                new InlineQueryResultArticle(
                    id: "3",
                    title: "TgBots",
                    inputMessageContent: new InputTextMessageContent(
                        "hello"
                    )
                )
            };

            await Bot.AnswerInlineQueryAsync(
                inlineQuery.Id,
                results,
                isPersonal: true,
                cacheTime: 0
            );
        }

        private static async Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult)
        {
            Console.WriteLine($"Received inline result: {chosenInlineResult.ResultId}");
        }

        private static async Task UnknownUpdateHandlerAsync(Update update)
        {
            Console.WriteLine($"Unknown update type: {update.Type}");
        }

        private static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMessage);
        }
    }
}