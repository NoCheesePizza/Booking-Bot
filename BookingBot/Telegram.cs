using Microsoft.VisualBasic;
using Newtonsoft.Json;
using OpenQA.Selenium.DevTools;
using RestSharp;
using System;

class Telegram
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

    class MessageContent
    {
        [JsonProperty("result")]
        public Message? message;
    }

    class UpdatesContent
    {
        [JsonProperty("result")]
        public List<Update>? updates;
    }

    class Update
    {
        [JsonProperty("callback_query")]
        public CallbackQuery? callbackQuery;

        [JsonProperty("message")]
        public Message? message;

        [JsonProperty("update_id")]
        public int? updateId;
    }

    class Message
    {
        [JsonProperty("date")]
        public int? date;

        [JsonProperty("text")]
        public string? text;
    }

    class CallbackQuery
    {
        [JsonProperty("message")]
        public Message? message;

        [JsonProperty("data")]
        public string? data;
    }

    static int offset = 0;
    static string target = Local.chats!["george"];

    static RestClient messageClient = new RestClient($"https://api.telegram.org/bot{Local.token}/sendMessage"),
                      photoClient = new RestClient($"https://api.telegram.org/bot{Local.token}/sendPhoto"),
                      updatesClient = new RestClient($"https://api.telegram.org/bot{Local.token}/getUpdates");

    static Dictionary<string, Action?> commands = new Dictionary<string, Action?>
    {
        { "/help", sendHelp },
        { "/stop", null },
        { "/bookingbot", Booking.BookingTelegram.startBot }
    };

    static void sendHelp()
    {
        sendMessage
        (
            "<u>Commands</u>" +
            "\n\n<b>/help:</b> Gets a list of available commands and a brief description of what they do" +
            "\n\n<b>/stop:</b> Stops the bot" +
            "\n\n<b>/bookingbot:</b> Starts Booking Bot, allowing you to book, view, and cancel discussion rooms at SIT@SP via RBS" +
            "\n\n<b>/homeworkbot:</b> Starts Homework Bot, giving you a sorted list of all upcoming submissions (except those in xSiTe)" +
            "\n\n<b>/badmintonbot:</b> Starts Badminton Bot, allowing you to check the available badminton court timeslots for your preferred day and region"
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

        RestResponse response = updatesClient.Get(request);
        if (response.Content == null)
            return;

        UpdatesContent? content = JsonConvert.DeserializeObject<UpdatesContent>(response.Content);
        offset = content!.updates![content.updates.Count - 1].updateId!.Value;
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
        
        while (true)
        {
            command = null;
            RestResponse response = updatesClient.Get(request.AddParameter("offset", offset + 1));
            if (response.Content == null)
                continue;

            UpdatesContent? content = JsonConvert.DeserializeObject<UpdatesContent>(response.Content);

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
        RestRequest messageRequest = new RestRequest();
        RestRequest updatesRequest = new RestRequest();

        messageRequest.AddJsonBody
        (
            new
            {
                chat_id = target,
                text = message,
                parse_mode = "HTML",
                reply_markup = new { inline_keyboard = inlineButtons }
            }
        );

        updatesRequest.AddJsonBody
        (
            new
            {
                allowed_updates = new List<string>() { "callback_query" }
            }
        );

        int date;
        MessageContent? messageContent = null;
        RestResponse messageResponse = messageClient.Get(messageRequest);

        if (messageResponse.Content == null)
            Local.log("Unable to get content of sent message containing inline keyboard", true, false);
        else
            messageContent = JsonConvert.DeserializeObject<MessageContent>(messageResponse.Content);

        date = messageContent!.message!.date!.Value;

        // wait for response
        while (true)
        {
            RestResponse updatesResponse = updatesClient.Get(updatesRequest);
            if (updatesResponse.Content == null)
                continue;

            UpdatesContent? updatesContent = JsonConvert.DeserializeObject<UpdatesContent>(updatesResponse.Content);
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
        RestRequest request = new RestRequest();

        request.AddJsonBody
        (
        new
            {
                chat_id = target,
                text = message,
                parse_mode = "HTML"
            }
        );

        messageClient.Get(request);
    }

    public static void sendPhoto(string filePath, string caption = "")
    {
        RestRequest request = new RestRequest();
        request.AddParameter("chat_id", target)
               .AddParameter("caption", caption)
               .AddFile("photo", "screenshot.jpg");
        photoClient.Get(request);
    }
}