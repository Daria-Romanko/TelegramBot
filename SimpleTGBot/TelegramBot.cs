using System.Reflection.Metadata.Ecma335;

namespace SimpleTGBot;

using System.Linq;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static SimpleTGBot.Movies;

public class TelegramBot
{
    // Токен TG-бота. Можно получить у @BotFather
    private const string BotToken = "6071463204:AAEbqJU6oii2Rsx3XZjQINyJmbrWHiPHzCc";

    private Movies? movies;
    private Movies.Movie? m;
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
        
        // Инициализируем наш клиент, передавая ему токен.
        var botClient = new TelegramBotClient(BotToken);

        // Служебные вещи для организации правильной работы с потоками
        using CancellationTokenSource cts = new CancellationTokenSource();

        // Разрешённые события, которые будет получать и обрабатывать наш бот.
        // Будем получать только сообщения. При желании можно поработать с другими событиями.
        ReceiverOptions receiverOptions = new ReceiverOptions()
        {
            AllowedUpdates = new[] { UpdateType.Message }
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
        // Работаем только с сообщениями. Остальные события игнорируем
        var message = update.Message;
     
        if (message.Text is not { } messageText || update.CallbackQuery is not null )
        {
            return;
        }

        // Получаем ID чата, в которое пришло сообщение. Полезно, чтобы отличать пользователей друг от друга.
        var chatId = message.Chat.Id;

        // Печатаем на консоль факт получения сообщения
        Console.WriteLine($"Получено сообщение в чате {chatId}: '{messageText}'");

        // TODO: Обработка пришедших сообщений

        ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
        {
            new KeyboardButton[] { "О боте" },          
            new KeyboardButton[] { "Рандомный фильм" },
            new KeyboardButton[] { "Топ 10" }
        });

        InlineKeyboardMarkup inlineKeyboard1 = new(new[]
        {
            InlineKeyboardButton.WithUrl(
                text: "Перейти в репозиторий",
                url: "https://github.com/Daria-Romanko/TelegramBot.git")
        });

        InlineKeyboardMarkup inlineKeyboard2 = new(new[]
        {
           InlineKeyboardButton.WithCallbackData("инфа","инфа"),
        });
        
        
        Message sentMessage;
        var m = movies.Random();

        if ((new string[] { "/start", "Привет", "О боте" }).Contains(message.Text) )
        {
            sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Привет! Этот бот предназначен для поиска фильмов." +
                "\nВот что может этот бот:"+ 
                "\n/start - выводит информацию о боте" +
                "\n/menu - выводит меню внизу экрана"+
                "\n/random - выводит рандомный фильм" +
                "\n/top10 - выводит 10 самых лучших фильмов" +
                "\n/searchMovie - находит фильм по запросу"+
                "\nТакже можно перейти в репозиторий:",
                replyMarkup: inlineKeyboard1,
                cancellationToken: cancellationToken);
            return;
        }
        if((new string[] { "/menu", "Меню" }).Contains(message.Text))
        {
            sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Внизу экрана появилось меню",
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);
        }
        if((new string[] { "/top10", "Топ 10" }).Contains(message.Text))
        {
            sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: Extensions.Top10(movies.movies),
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);
        }
        if((new string[] { "/random", "Рандомный фильм" }).Contains(message.Text))
        {         
            sentMessage = await botClient.SendPhotoAsync(
            chatId: chatId,
            photo: m.Image,
            caption: "<b>" + m.Title + "</b>" + m.Description,
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard2,
            cancellationToken: cancellationToken);
            return;

        }
        

        //if (update.CallbackQuery.Data == "инфа")
        //{
        //    sentMessage = await botClient.SendTextMessageAsync(
        //            chatId: chatId,
        //            text: m.Director + m.Screenwriter + m.Actors,
        //            replyMarkup: inlineKeyboard2
        //            );
        //    return;
        //}



        // Отправляем обратно то же сообщение, что и получили
        //sentMessage = await botClient.SendTextMessageAsync(
        //    chatId: chatId,
        //    text: "Ты написал:\n" + message.Text,
        //    cancellationToken: cancellationToken);
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

    /// <summary>
    ///// Отправляет рандомный фильм
    ///// </summary>
    ///// <param name="botClient"></param>
    ///// <param name="message"></param>
    ///// <param name="collectionItem"></param>
    ///// <param name="cancellationToken"></param>
    ///// <returns></returns>
    //private static async Task SendRandomMovie(ITelegramBotClient botClient, long chatId, Update update, CancellationToken cancellationToken)
    //{
        
              
              
    //}

    
        
    

    
}