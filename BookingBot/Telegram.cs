using Microsoft.VisualBasic;
using Newtonsoft.Json;
using OpenQA.Selenium.DevTools;
using RestSharp;
using System;

public class Telegram
{
    protected class InlineButton
    {
        public string text { get; set; }
        public string callback_data { get; set; }

        public InlineButton(string _text, string _callback_data)
        {
            text = _text;
            callback_data = _callback_data;
        }
    }

    /// <remarks>
    /// cannot be blank
    /// </remarks>
    class MessageContent
    {
        [JsonProperty("result")]
        public Message? message;
    }

    /// <remarks>
    /// cannot be blank
    /// </remarks>
    class UpdatesContent
    {
        [JsonProperty("result")]
        public List<Update>? updates;
    }

    /// <remarks>
    /// cannot be blank but updates.Count may be 0
    /// </remarks>
    class Update
    {
        [JsonProperty("callback_query")]
        public CallbackQuery? callbackQuery;

        [JsonProperty("message")]
        public Message? message;

        [JsonProperty("update_id")]
        public int? updateId;
    }

    /// <remarks>
    /// can be blank
    /// </remarks>
    class Message
    {
        [JsonProperty("date")]
        public int? date;

        [JsonProperty("text")]
        public string? text;
    }

    /// <remarks>
    /// can be blank
    /// </remarks>
    class CallbackQuery
    {
        [JsonProperty("message")]
        public Message? message;

        [JsonProperty("data")]
        public string? data;
    }

    public const string chat = "george";
    static string target = Local.chats![chat];
    static int offset = 0;

    static RestClient messageClient = new RestClient($"https://api.telegram.org/bot{Local.token}/sendMessage"),
                      photoClient = new RestClient($"https://api.telegram.org/bot{Local.token}/sendPhoto"),
                      updatesClient = new RestClient($"https://api.telegram.org/bot{Local.token}/getUpdates");

    static Dictionary<string, Action?> commands = new Dictionary<string, Action?>
    {
        { "/help", sendHelp },
        { "/stop", null },
        { "/bookingbot", BookingTelegram.startBot }
    };

    static void sendHelp()
    {
        sendMessage
        (
            "<u>Commands</u>" +
            "\n\n/help: Gets a list of available commands and a brief description of what they do" +
            "\n\n/stop: Stops the bot" +
            "\n\n/bookingbot: Starts Booking Bot, allowing you to book, view, and cancel discussion rooms at SIT@SP via RBS" +
            "\n\n/homeworkbot: Starts Homework Bot, giving you a sorted list of all upcoming submissions (except those in xSiTe)" +
            "\n\n/badmintonbot: Starts Badminton Bot, allowing you to check the available badminton court timeslots for your preferred day and region"
        );
    }

    public static void getOffset()
    {
        RestRequest request = new RestRequest().AddJsonBody
        (
            new
            {
                allowed_updates = new List<string>() { "message" }
            }
        );

        RestResponse? response = null;

        try
        { response = updatesClient.Get(request); }
        catch
        { Local.log($"Unable to get request from {updatesClient.BuildUri(request)}", true, false); }

        UpdatesContent? content = JsonConvert.DeserializeObject<UpdatesContent>(response!.Content!);
        if (content!.updates!.Count == 0)
            return;

        offset = content.updates[content.updates.Count - 1].updateId!.Value;
    }

    public static bool getCommand()
    {
        RestRequest request = new RestRequest().AddJsonBody
        (
            new
            {
                allowed_updates = new List<string>() { "message" }
            }
        );

        string? command;
        RestResponse? response;
        
        while (true)
        {
            try
            { response = updatesClient.Get(request.AddParameter("offset", offset + 1)); }
            catch
            { continue; }

            command = null;
            UpdatesContent? content = JsonConvert.DeserializeObject<UpdatesContent>(response!.Content!);

            foreach (Update update in content!.updates!)
            {
                offset = update.updateId!.Value;

                if (update.message != null && commands.ContainsKey(update.message.text!))
                {
                    command = update.message.text;
                    break;
                }
            }

            if (command != null)
                break;

            Thread.Sleep(1000);
        }

        if (command == "/stop")
            return false;
        else
        {
            commands[command]!.Invoke();
            return true;
        }
    }

    protected static string sendInlineButtons(List<List<InlineButton>> inlineButtons, string message)
    {
        RestRequest messageRequest = new RestRequest().AddJsonBody
        (
            new
            {
                chat_id = target,
                text = message,
                parse_mode = "HTML",
                reply_markup = new { inline_keyboard = inlineButtons }
            }
        );

        RestRequest updatesRequest = new RestRequest().AddJsonBody
        (
            new
            {
                allowed_updates = new List<string>() { "callback_query" }
            }
        );

        RestResponse? messageResponse = null,
                      updatesResponse; 

        try
        { messageResponse = messageClient.Get(messageRequest); }
        catch
        { Local.log($"Unable to get request from {messageClient.BuildUri(messageRequest)}", true, false); }
        
        MessageContent? messageContent = JsonConvert.DeserializeObject<MessageContent>(messageResponse!.Content!);
        int date = messageContent!.message!.date!.Value;

        // wait for response
        while (true)
        {
            try
            { updatesResponse = updatesClient.Get(updatesRequest); }
            catch
            { continue; }

            UpdatesContent? updatesContent = JsonConvert.DeserializeObject<UpdatesContent>(updatesResponse!.Content!);
            if (updatesContent!.updates!.Count == 0)
                continue;

            Update lastUpdate = updatesContent.updates[updatesContent.updates.Count - 1];
            if (lastUpdate.callbackQuery != null && lastUpdate.callbackQuery.message!.date == date)
                return lastUpdate.callbackQuery.data!;

            Thread.Sleep(1000);
        }
    }

    public static void sendMessage(string message)
    {
        messageClient.Get(new RestRequest().AddJsonBody
        (
            new
            {
                chat_id = target,
                text = message,
                parse_mode = "HTML"
            }
        ));
    }

    public static void sendPhoto(string filePath, string caption = "")
    {
        photoClient.Get(new RestRequest().AddParameter("chat_id", target)
                                         .AddParameter("caption", caption)
                                         .AddFile("photo", "screenshot.jpg"));
    }
}