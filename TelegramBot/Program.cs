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

        private static List<string> Props = new List<string>()
        {
            "Опиши свои навыки:",
            "Введи свой email:",
            "Введи свой номер телефона:",
            "Введи город, в который ты хочешь подать заявку:",
            "Введи ФИО:"
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
                    
                    await Bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text:  $"Заявка на вакансию {ListName} успешно отправлена!"
                    );
                    
                    Data = new List<object>();
                    ListName = null;
                }

                else
                {
                    if (Count == 7)
                    {
                        Data = new List<object> {message.Chat.Username, message.Date.ToString(CultureInfo.InvariantCulture)};
                        Count -= 2;
                    }

                
                    Data.Add(message.Text);

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
                    //text:  "*Hello*"
                );
                
                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Мои команды:",
                    replyMarkup: inlineKeyboard
                );
            }
            
            // Send inline keyboard
            // You can process responses in BotOnCallbackQueryReceived handler
            /*static async Task SendInlineKeyboard(Message message)
            {
                await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    // first row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("1.1", "11"),
                        InlineKeyboardButton.WithCallbackData("1.2", "12"),
                    },
                    // second row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("2.1", "21"),
                        InlineKeyboardButton.WithCallbackData("2.2", "22"),
                    }
                });
                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Choose",
                    replyMarkup: inlineKeyboard
                );
            }

            static async Task SendReplyKeyboard(Message message)
            {
                var replyKeyboardMarkup = new ReplyKeyboardMarkup(
                    new []
                    {
                        new KeyboardButton[] { "1.1", "1.2" },
                        new KeyboardButton[] { "2.1", "2.2" },
                    },
                    resizeKeyboard: true
                );

                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Choose",
                    replyMarkup: replyKeyboardMarkup

                );
            }

            static async Task SendFile(Message message)
            {
                await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto);

                const string filePath = @"Files/tux.png";
                await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var fileName = filePath.Split(Path.DirectorySeparatorChar).Last();
                await Bot.SendPhotoAsync(
                    chatId: message.Chat.Id,
                    photo: new InputOnlineFile(fileStream, fileName),
                    caption: "Nice Picture"
                );
            }

            static async Task RequestContactAndLocation(Message message)
            {
                var requestReplyKeyboard = new ReplyKeyboardMarkup(new[]
                {
                    KeyboardButton.WithRequestLocation("Location"),
                    KeyboardButton.WithRequestContact("Contact"),
                });
                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Who or Where are you?",
                    replyMarkup: requestReplyKeyboard
                );
            }

            static async Task Usage(Message message)
            {
                const string usage = "Usage:\n" +
                                        "/start" +
                                        "/inline   - send inline keyboard\n" +
                                        "/keyboard - send custom keyboard\n" +
                                        "/photo    - send a photo\n" +
                                        "/request  - request location or contact";
                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: usage,
                    replyMarkup: new ReplyKeyboardRemove()
                );
            }*/
        }

        // Process Inline Keyboard callback data
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
                    
                    await Bot.SendTextMessageAsync(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: "Загрузи тестовое задание на Google Drive и отправь мне ссылку на него:"
                    );
                    
                    break;
                }
                
                case "/sendTester":
                {
                    break;
                }

                case "/sendAnalyst":
                {
                    break;
                }

                case "/sendWriter":
                {
                    break;
                }

            }

            await Bot.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                $"Received {callbackQuery.Data}"
            );
        }

        #region Inline Mode

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

        #endregion

        private static async Task UnknownUpdateHandlerAsync(Update update)
        {
            Console.WriteLine($"Unknown update type: {update.Type}");
        }

        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
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