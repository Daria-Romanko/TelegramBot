using System.Net.Mime;
using System.Reflection.Metadata.Ecma335;
using Telegram.Bot.Requests;

namespace SimpleTGBot;

using System.Linq;
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
        movies.FileConversion("database/kinopoisk-top250.csv");

        checkYear = false;
        checkCountry = false;
        checkVoteAverage = false;
        checkDirector = false;

        replyKeyboardMarkup = new(new[]
        {
            new KeyboardButton[] { "О боте", "Найти фильм"},
            new KeyboardButton[] { "Топ 10", "Рандомный фильм" }        
        });
        replyKeyboardMarkup.ResizeKeyboard = true;
        searchMovieInlineKeyboard = new(new[]
        {          
           InlineKeyboardButton.WithCallbackData("По году", "callback_1"),
           InlineKeyboardButton.WithCallbackData("По стране", "callback_2"),
           InlineKeyboardButton.WithCallbackData("По режиссеру", "callback_3"),
           InlineKeyboardButton.WithCallbackData("По оценке", "callback_4")
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
           InlineKeyboardButton.WithCallbackData("->", "next")
        });
        inlineKeyboard2 = new(new[]
        {
           InlineKeyboardButton.WithUrl(
                text: "Трейлер фильма",
                url: "https://www.youtube.com/watch?v=o-YBDTqX_ZU"),
           InlineKeyboardButton.WithCallbackData("<-", "prev")
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

            if ((new string[] { "/start", "Привет", "О боте" }).Contains(messageText))
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
                    "\nТакже можно перейти в репозиторий:",
                    replyMarkup: urlInlineKeyboard,
                    cancellationToken: cancellationToken);
                return;
            }
            if ((new string[] { "/menu", "Меню" }).Contains(messageText))
            {
                Message sentMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Внизу экрана появилось меню",
                    replyMarkup: replyKeyboardMarkup,
                    cancellationToken: cancellationToken);
            }
            if ((new string[] { "/top10", "Топ 10" }).Contains(messageText))
            {
                Message sentMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: movies.Top10(),
                    replyMarkup: replyKeyboardMarkup,
                    cancellationToken: cancellationToken);
            }
            if ((new string[] { "/random", "Рандомный фильм" }).Contains(messageText))
            {
                movie = movies.Random();
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
                return;
            }
            if ((new string[] { "/searchMovie", "Найди фильм", "Найти фильм" }).Contains(messageText))
            {
                Message sentMessage = await botClient.SendTextMessageAsync(
                     chatId: chatId,
                     text: "Выберите как будете искать фильм.",
                     replyMarkup: searchMovieInlineKeyboard,
                     cancellationToken: cancellationToken);               
            }
            if(checkYear == true)
            {
                checkYear = false;

                movie = movies.SearchMovieYear(int.Parse(messageText));

                if(movie == null) 
                {
                    await botClient.SendTextMessageAsync(
                         chatId: chatId,
                         text: "К сожалению фильм не найден, попробуйте еще раз. \n Выберите как будете искать фильм.",
                         parseMode: ParseMode.Html,
                         replyMarkup: inlineKeyboard1,
                         cancellationToken: cancellationToken);
                    return;
                }

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
                
                return;
            }
            if(checkCountry == true)
            {
                checkCountry = false;

                movie = movies.SearchMovieCountry(messageText);

                if (movie == null)
                {
                    await botClient.SendTextMessageAsync(
                         chatId: chatId,
                         text: "К сожалению фильм не найден, попробуйте еще раз. \n Выберите как будете искать фильм.",
                         parseMode: ParseMode.Html,
                         replyMarkup: searchMovieInlineKeyboard,
                         cancellationToken: cancellationToken);
                    return;
                }

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
                
                return;
            }
            if(checkVoteAverage == true)
            {
                checkVoteAverage = false;

                movie = movies.SearchMovieAverage(double.Parse(messageText));
                
                if (movie == null)
                {
                    await botClient.SendTextMessageAsync(
                         chatId: chatId,
                         text: "К сожалению фильм не найден, попробуйте еще раз. \n Выберите как будете искать фильм.",
                         parseMode: ParseMode.Html,
                         replyMarkup: searchMovieInlineKeyboard,
                         cancellationToken: cancellationToken);
                    return;
                }

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
                
                return;
            }
            if(checkDirector == true)
            {
                checkDirector = false;

                movie = movies.SearchMovieDirector(messageText);
                
                if (movie == null)
                {
                    await botClient.SendTextMessageAsync(
                         chatId: chatId,
                         text: "К сожалению фильм не найден, попробуйте еще раз. \n Выберите как будете искать фильм.",
                         parseMode: ParseMode.Html,
                         replyMarkup: searchMovieInlineKeyboard,
                         cancellationToken: cancellationToken);
                    return;
                }

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
                checkDirector = false;
                return;
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
                          "<b>" + "\nРежиссер: " + "</b>" + movie.Director.Replace(';', ',') +
                          "<b>" + "\nВ главных ролях: " + "</b>" + movie.Actors.Replace(';', ','),
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
            if (update.CallbackQuery?.Data == "callback_1")
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Введите год"
                    );                         
                checkYear = true;
            }
            if(update.CallbackQuery?.Data == "callback_2")
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Введите страну"
                    );
                checkCountry = true;
            }
            if (update.CallbackQuery?.Data == "callback_3")
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Введите режиссера"
                    );
                checkDirector = true;
            }
            if (update.CallbackQuery?.Data == "callback_4")
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Введите оценку"
                    );
                checkVoteAverage = true;
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