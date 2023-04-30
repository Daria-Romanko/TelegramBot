using System.Net.Mime;
using System.Reflection.Metadata.Ecma335;
using Telegram.Bot.Requests;

namespace SimpleTGBot;

using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using static SimpleTGBot.Movies;
using System.IO;

public class TelegramBot
{
    // Токен TG-бота. Можно получить у @BotFather
    private const string BotToken = "6071463204:AAFweciMMIF1XIGT3uSntgWzdDHOs9WIMgo";

    private ReplyKeyboardMarkup? replyKeyboardMarkup;
    private InlineKeyboardMarkup? searchMovieInlineKeyboard;
    private InlineKeyboardMarkup? urlInlineKeyboard;
    private InlineKeyboardMarkup? inlineKeyboard1;
    private InlineKeyboardMarkup? inlineKeyboard2;

    private Movies? movies;
    private Movies.Movie? movie;

    private bool checkTitle;
    private bool checkYear;
    private bool checkCountry;
    private bool checkVoteAverage;
    private bool checkDirector;
    
    /// <summary>
    /// Инициализирует и обеспечивает работу бота до нажатия клавиши Esc
    /// </summary>
    public async Task Run()
    {
        // Если вам нужно хранить какие-то данные во время работы бота (массив информации, логи бота,
        // историю сообщений для каждого пользователя), то это всё надо инициализировать в этом методе.
        // TODO: Инициализация необходимых полей      
        movies = new Movies();
        movies.FileConversion("files/kinopoisk-top250.csv");
        System.IO.File.WriteAllText("files/movie_viewing_history.txt", "История просмотра");
        checkTitle = false;
        checkYear = false;
        checkCountry = false;
        checkVoteAverage = false;
        checkDirector = false;

        replyKeyboardMarkup = new(new[]
        {
            new KeyboardButton[] { "Найти фильм", "Рандомный фильм"},
            new KeyboardButton[] { "Топ 10", "О боте" , "История"}
        });
        replyKeyboardMarkup.ResizeKeyboard = true;
        searchMovieInlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new []
            {
                InlineKeyboardButton.WithCallbackData("По названию", "callback_0"),
                InlineKeyboardButton.WithCallbackData("По году", "callback_1"),
                InlineKeyboardButton.WithCallbackData("По стране", "callback_2")
            },          
            new []
            {
                InlineKeyboardButton.WithCallbackData("По режиссеру", "callback_3"),
                InlineKeyboardButton.WithCallbackData("По оценке", "callback_4")
            }
           
        });
       
        urlInlineKeyboard = new(new[]
        {
            InlineKeyboardButton.WithUrl(
                text: "Перейти в репозиторий",
                url: "https://github.com/Daria-Romanko/TelegramBot.git")
        });
        inlineKeyboard1 = new(new[]
        {
           InlineKeyboardButton.WithUrl(
                text: "Трейлер фильма",
                url: "https://www.youtube.com/watch?v=o-YBDTqX_ZU"),
           InlineKeyboardButton.WithCallbackData("🡺", "next")
        });
        inlineKeyboard2 = new(new[]
        {
           InlineKeyboardButton.WithUrl(
                text: "Трейлер фильма",
                url: "https://www.youtube.com/watch?v=o-YBDTqX_ZU"),
           InlineKeyboardButton.WithCallbackData("🡸", "prev")
        });

        // Инициализируем наш клиент, передавая ему токен.
        var botClient = new TelegramBotClient(BotToken);

        // Служебные вещи для организации правильной работы с потоками
        using CancellationTokenSource cts = new CancellationTokenSource();

        // Разрешённые события, которые будет получать и обрабатывать наш бот.
        // Будем получать только сообщения. При желании можно поработать с другими событиями.
        ReceiverOptions receiverOptions = new ReceiverOptions()
        {
            AllowedUpdates = new[]
            {
                UpdateType.Message,
                UpdateType.CallbackQuery
            }
        };

        // Привязываем все обработчики и начинаем принимать сообщения для бота
        botClient.StartReceiving(
            updateHandler: OnMessageReceived,
            pollingErrorHandler: OnErrorOccured,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        // Проверяем что токен верный и получаем информацию о боте
        var me = await botClient.GetMeAsync(cancellationToken: cts.Token);
        Console.WriteLine($"Бот @{me.Username} запущен.\nДля остановки нажмите клавишу Esc...");

        // Ждём, пока будет нажата клавиша Esc, тогда завершаем работу бота
        while (Console.ReadKey().Key != ConsoleKey.Escape) { }

        // Отправляем запрос для остановки работы клиента.
        cts.Cancel();
    }

    /// <summary>
    /// Обработчик события получения сообщения.
    /// </summary>
    /// <param name="botClient">Клиент, который получил сообщение</param>
    /// <param name="update">Событие, произошедшее в чате. Новое сообщение, голос в опросе, исключение из чата и т. д.</param>
    /// <param name="cancellationToken">Служебный токен для работы с многопоточностью</param>
    async Task OnMessageReceived(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is null && update.CallbackQuery is null)
        {
            return;
        }

        // Получаем ID чата, в которое пришло сообщение. Полезно, чтобы отличать пользователей друг от друга.
        long chatId = 0;

        if (update.Message != null)
        {
            chatId = update.Message.Chat.Id;

        }
        else if (update.CallbackQuery != null)
        {
            chatId = update.CallbackQuery.Message.Chat.Id;
        }


        if (update.Type == UpdateType.Message)
        {
            var message = update.Message;
            var messageText = message?.Text;

            Console.WriteLine($"Получено сообщение в чате {chatId}: '{messageText}'");

            if ((new string[] { "/start", "привет", "о боте" }).Contains(messageText.ToLower()))
            {
                Message sentMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Привет! Этот бот предназначен для поиска фильмов." +
                    "\nВот что может этот бот:" +
                    "\n/start - выводит информацию о боте" +
                    "\n/menu - выводит меню внизу экрана" +
                    "\n/random - выводит рандомный фильм" +
                    "\n/top10 - выводит 10 самых лучших фильмов" +
                    "\n/searchMovie - находит фильм по запросу" +
                    "\n/movieHistory - выводит историю просмотренных фильмов" +
                    "\nТакже можно перейти в репозиторий:",
                    replyMarkup: urlInlineKeyboard,
                    cancellationToken: cancellationToken);
            }
            if ((new string[] { "/menu", "меню" }).Contains(messageText.ToLower()))
            {
                Message sentMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Внизу экрана появилось меню",
                    replyMarkup: replyKeyboardMarkup,
                    cancellationToken: cancellationToken);
            }
            if ((new string[] { "/top10", "топ 10" }).Contains(messageText.ToLower()))
            {
                Message sentMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: movies.Top10(),
                    replyMarkup: replyKeyboardMarkup,
                    cancellationToken: cancellationToken);
            }
            if ((new string[] { "/random", "рандомный фильм", "рандом" }).Contains(messageText.ToLower()))
            {
                movie = movies.Random();
                SendMessageForMovie1();
            }
            if ((new string[] { "/searchmovie", "найди фильм", "найти фильм" }).Contains(messageText.ToLower()))
            {
                Message sentMessage = await botClient.SendTextMessageAsync(
                     chatId: chatId,
                     text: "Выберите как будете искать фильм.",
                     replyMarkup: searchMovieInlineKeyboard,
                     cancellationToken: cancellationToken);
            }
            if((new string[] { "/moviehistory", "история", "история просмотра" }).Contains(messageText.ToLower()))
            {
                SendMovieHistory();
            }
            if (checkTitle == true)
            {
                checkTitle = false;
                movie = movies.SearchMovie(messageText);
                SendMessageForMovie2();
            }
            if (checkYear == true)
            {
                checkYear = false;
                movie = movies.SearchMovieYear(int.Parse(messageText));
                SendMessageForMovie2();
            }
            if (checkCountry == true)
            {
                checkCountry = false;
                movie = movies.SearchMovieCountry(messageText);
                SendMessageForMovie2();
            }
            if (checkVoteAverage == true)
            {
                checkVoteAverage = false;
                movie = movies.SearchMovieAverage(double.Parse(messageText.Replace('.', ',')));
                SendMessageForMovie2();
            }
            if (checkDirector == true)
            {
                checkDirector = false;
                movie = movies.SearchMovieDirector(messageText);
                SendMessageForMovie2();
            }

        }
        if (update.Type == UpdateType.CallbackQuery)
        {
            if (update.CallbackQuery?.Data == "next")
            {
                await botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: update.CallbackQuery.Message.MessageId,
                    text: "<b>" + "Год производства: " + "</b>" + movie.Year +
                          "<b>" + "\nСтрана: " + "</b>" + movie.Country +
                          "<b>" + "\nОценка: " + "</b>" + movie.VoteAverage +
                          "<b>" + "\nРежиссер: " + "</b>" + movie.Director.Replace(';', ',') +
                          "<b>" + "\nАктеры: " + "</b>" + movie.Actors.Replace(';', ','),
                    parseMode: ParseMode.Html,
                    replyMarkup: inlineKeyboard2,
                    cancellationToken: cancellationToken);
            }
            if (update.CallbackQuery?.Data == "prev")
            {
                await botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: update.CallbackQuery.Message.MessageId,
                    text: "<b>" + movie.Title + "</b>" + "\n" + movie.Description.Replace(';', ','),
                    parseMode: ParseMode.Html,
                    replyMarkup: inlineKeyboard1,
                    cancellationToken: cancellationToken);
            }
            if (update.CallbackQuery?.Data == "callback_0")
            {               
                await botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: update.CallbackQuery.Message.MessageId,
                    text: "Введите название:",
                    cancellationToken: cancellationToken);
                checkTitle = true;
                checkYear = false;
                checkCountry = false;
                checkVoteAverage = false;
                checkDirector = false;
            }
            if (update.CallbackQuery?.Data == "callback_1")
            {
                await botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: update.CallbackQuery.Message.MessageId,
                    text: "Введите год:",
                    cancellationToken: cancellationToken);      
                checkYear = true;
                checkTitle = false;
                checkCountry = false;
                checkVoteAverage = false;
                checkDirector = false;
            }
            if (update.CallbackQuery?.Data == "callback_2")
            {
                await botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: update.CallbackQuery.Message.MessageId,
                    text: "Введите страну:",
                    cancellationToken: cancellationToken);               
                checkCountry = true;
                checkTitle = false;
                checkYear = false;
                checkVoteAverage = false;
                checkDirector = false;
            }
            if (update.CallbackQuery?.Data == "callback_3")
            {
                await botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: update.CallbackQuery.Message.MessageId,
                    text: "Введите режиссера:",
                    cancellationToken: cancellationToken);                
                checkDirector = true;
                checkTitle = false;
                checkYear = false;
                checkCountry = false;
                checkVoteAverage = false;
            }
            if (update.CallbackQuery?.Data == "callback_4")
            {
                await botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: update.CallbackQuery.Message.MessageId,
                    text: "Введите оценку:",
                    cancellationToken: cancellationToken);
                checkVoteAverage = true;
                checkTitle = false;
                checkYear = false;
                checkCountry = false;              
                checkDirector = false;

            }
        }

        async void SendMessageForMovie1()
        {
            System.IO.File.AppendAllText("files/movie_viewing_history.txt", $"\n{movie.Title},{movie.Year},{movie.Country},{movie.Director},{movie.VoteAverage}");

            Message sentMessage = await botClient.SendPhotoAsync(
                chatId: chatId,
                photo: movie.Image,
                cancellationToken: cancellationToken);
            sentMessage = await botClient.SendTextMessageAsync(
                 chatId: chatId,
                 text: "<b>" + movie.Title + "</b>" + "\n" + movie.Description.Replace(';', ','),
                 parseMode: ParseMode.Html,
                 replyMarkup: inlineKeyboard1,
                 cancellationToken: cancellationToken);
        }

        async void SendMessageForMovie2()
        {
            if (movie == null)
            {
                await botClient.SendTextMessageAsync(
                     chatId: chatId,
                     text: "Фильм не найден, попробуйте еще раз. \n Выберите как будете искать фильм.",
                     parseMode: ParseMode.Html,
                     replyMarkup: searchMovieInlineKeyboard,
                     cancellationToken: cancellationToken);
                return;
            }
            System.IO.File.AppendAllText("files/movie_viewing_history.txt", $"\n{movie.Title},{movie.Year},{movie.Country},{movie.Director},{movie.VoteAverage}");

            Message sentMessage = await botClient.SendPhotoAsync(
            chatId: chatId,
            photo: movie.Image,
            cancellationToken: cancellationToken);
            sentMessage = await botClient.SendTextMessageAsync(
                 chatId: chatId,
                 text: "<b>" + movie.Title + "</b>" + "\n" + movie.Description.Replace(';', ','),
                 parseMode: ParseMode.Html,
                 replyMarkup: inlineKeyboard1,
                 cancellationToken: cancellationToken);
            
        }

        async void SendMovieHistory()
        {
            string[] history = System.IO.File.ReadAllLines("files/movie_viewing_history.txt");

            var s = "";
            if(history.Length == 1)
            {
                Message sentMessage = await botClient.SendTextMessageAsync(
                      chatId: chatId,
                      text: "История просмотра пуста.",
                      parseMode: ParseMode.Html,
                      replyMarkup: replyKeyboardMarkup,
                      cancellationToken: cancellationToken);
            }
            else
            {
                for (int i = 1; i < history.Length; i++)
                {
                    s += $"\n{history[i].Split(",")[0]}";
                }

                Message sentMessage = await botClient.SendTextMessageAsync(
                     chatId: chatId,
                     text: "<b>" + "История просмотра" + "</b>" + "\n" + s,
                     parseMode: ParseMode.Html,
                     replyMarkup: replyKeyboardMarkup,
                     cancellationToken: cancellationToken);
            }
            
        }
    }

    /// <summary>
    /// Обработчик исключений, возникших при работе бота
    /// </summary>
    /// <param name="botClient">Клиент, для которого возникло исключение</param>
    /// <param name="exception">Возникшее исключение</param>
    /// <param name="cancellationToken">Служебный токен для работы с многопоточностью</param>
    /// <returns></returns>
    Task OnErrorOccured(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        //В зависимости от типа исключения печатаем различные сообщения об ошибке
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",

            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);

        //Завершаем работу
        return Task.CompletedTask;

    }

}