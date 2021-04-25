using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Google.Apis.Sheets.v4.Data;

namespace TelegramBot
{
    internal static class GoogleSheetsInterference
    {
        private const string SpreadsheetId = "1Ekhou8bS4jEZOjVnotTZowA6HoR1XhB5AhMq8xqrCBQ";
        private static readonly SheetsService Service = CreateService();

        private static SheetsService CreateService()
        {
            string[] scopes = {SheetsService.Scope.Spreadsheets, "https://www.googleapis.com/auth/cloud-platform"};
            UserCredential credential;
            const string appName = "TestGS2App";
            const string credentialsJson = "./credentials.json";

            using (var stream = new FileStream(credentialsJson, FileMode.Open, FileAccess.Read))
            {
                var credPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            return new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = appName,
            });
        }

        public static IList<IList<object>> GetDataFromList(string sheetName, string cellRange)
        {
            var range = sheetName + cellRange;
            var request = Service.Spreadsheets.Values.Get(SpreadsheetId, range);
            var response = request.Execute();

            return response.Values;
        }

        public static void AppendList(string sheetName, IList<object> data)
        {
            var range = $"{sheetName}!A:E";
            var valueRange = new ValueRange {Values = new List<IList<object>> {data}};
            var append = Service.Spreadsheets.Values.Append(valueRange, SpreadsheetId, range);

            append.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
            append.Execute();
        }

        public static IList<IList<object>> GetStringById(string sheetName, string searchedId)
        {
            var rowArray = GetDataFromList("A", SpreadsheetId);
            var neededRow = 0;

            for (var i = 0; i < rowArray.Count; i++)
            {
                if (rowArray[i].ToString() == searchedId)
                    neededRow = i + 2;
            }

            var range = string.Format("A{0}:C{0}", neededRow);

            return GetDataFromList(sheetName, range);
        }

        public static void CreateList(string sheetName, string[] properties)
        {
            if (!CheckMissingListName(sheetName))
                throw new ArgumentException();

            var addSheetRequest = new AddSheetRequest {Properties = new SheetProperties {Title = sheetName}};
            var batchUpdateSpreadsheetRequest = new BatchUpdateSpreadsheetRequest
            {
                Requests = new List<Request> {new Request {AddSheet = addSheetRequest}}
            };
            var batchUpdateRequest = Service.Spreadsheets.BatchUpdate(batchUpdateSpreadsheetRequest, SpreadsheetId);

            batchUpdateRequest.Execute();
            /*var properties = ParseProperties(GetDataFromList("Properties", "!A2:A7"));*/
            AppendList(sheetName, properties);
        }


        private static bool CheckMissingListName(string sheetName)
        {
            var spreadSheets = Service.Spreadsheets.Get(SpreadsheetId).Execute();
            return spreadSheets.Sheets.All(spreadSheet => spreadSheet.Properties.Title != sheetName);
        }

        public static List<string> OutputListNames()
        {
            var spreadSheets = Service.Spreadsheets.Get(SpreadsheetId).Execute();
            return spreadSheets.Sheets.Select(spreadSheet => spreadSheet.Properties.Title).ToList();
        }

        private static string[] ParseProperties(IList<IList<object>> list)
        {
            var arr = new string[list.Count];
            var i = 0;
            foreach (var e in list)
            {
                arr[i] = e[0].ToString();
                i++;
            }

            return arr;
        }
        
        public static string GetRules()
        {
            var str = ParseProperties(GetDataFromList("BotTexts", "!A2:A2"));
            return str[0];
        }

        public static string GetFaq()
        {
            var str = ParseProperties(GetDataFromList("BotTexts", "!B2:B2"));
            return str[0];
        }

        public static string GetHelloMessage()
        {
            var str = ParseProperties(GetDataFromList("BotTexts", "!C2:C2"));
            return str[0];
        }
        
        public static void CreateListTexts()
        {
            var text = new[] {
                @"Правила и порядок приема:
            1) Посмотри доступные стажировки в нашем боте
            2) Выбери интересующее теь направление
            3) Выполни тестовое задание по выбранному направлению
            4) Последовательно заполни анкету на стажировку и прикрепи тестовое задание
            5) Получите приглашение на встречу
            6) Пройдите интервью в компании
            7) Получите приглашение на стажировку
            Что ждет тебя на стажировке:
                — опытный наставник
                — работа с реальными задачами
                — использование передовых технологий
                — удобный график работы
                — стипендия
                — возможность попасть в штат компании
                — профессиональное развитие
                — яркие корпоративы
            При наличии вопросов обратитесь к FAQ или напишите нам на почту: nautrainee@naumen.ru",
                @"1. Что будет на интервью?
· кандидат рассказывает о себе: об учебе, в каких проектах участвовал, отвечает на дополнительные вопросы
· задачки по направлению (аналитикам — на логику и системное мышление, тестировщикам — на внимательность и тест-кейсы)
· подробное описание стажировки у нас и ответы на любые Ваши вопросы

2. Совмещение стажировки с учебой.
· важно уделять стажировке не менее 30 часов в неделю
· график согласуется с руководителем

3. Оплата стажировки.
· стажеры получают стипендию от 30 тысяч
· оплата зависит от количества отработанных часов

4. Что делать, если понравилось сразу несколько направлений?
· решение тестового задания по каждому направлению поможет определиться с выбором
· если выбор после прохождения тестов по каждому направлению не сделан, предлагаем написать нам на почту

5. Если нет объявления об интересной мне вакансии?
· отправляйте резюме, как только вакансия откроется, мы свяжемся с Вами

6. Что делать после отправки тестового задания? 
· ожидайте нашего приглашения на собеседование
    
— Стажировка зачитывается за учебную практику. 
— Удаленный формат работы отсутствует: очень важно работать очно в команде. 
— Лучших стажеров берем в штат на фулл-тайм работу или работу по индивидуальному графику.
— Проверка тестовых заданий занимает некоторое время, поэтому будьте терпеливы.",
                @"Нау-мяу! Я — NAU-TRAINEE бот. Я помогу тебе с выбором стажировки, объясню правила приема заявок, а также помогу связаться с работодателем! Используй следующие команды:
"
            };
            var localProperties = new[] {"Rules", "FAQ", "HelloMessage"};
            CreateList("BotTexts", localProperties);
            var objectText = text.Cast<object>().ToList();
            AppendList("BotTexts", objectText);
        }
    }
}