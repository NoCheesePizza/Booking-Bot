using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Linq.Expressions;

class Local
{
    public class Preferences
    {
        [JsonProperty("token")]
        public string? token;

        [JsonProperty("chats")]
        public Dictionary<string, string>? chats;

        [JsonProperty("username")]
        public string? username;

        [JsonProperty("password")]
        public string? password;

        [JsonProperty("startTime")]
        public string? startTime;

        [JsonProperty("endTime")]
        public string? endTime;

        [JsonProperty("dayOfWeek")]
        public int? dayOfWeek;

        [JsonProperty("priorities")]
        public Queue<string>? priorities;
    }

    static StreamWriter wLogFile = new StreamWriter("..\\..\\..\\Logs.txt", true);

    public static Preferences? readPreferences,
                               editedPreferences;

    public static string? token { get; private set; }
    public static Dictionary<string, string>? chats { get; private set; }
    public static string? username { get; private set; }
    public static string? password { get; private set; }
    public static DateTime startTime { get; private set; } = new DateTime(2023, 1, 1, 7, 0, 0);
    public static DateTime endTime { get; private set; } = new DateTime(2023, 1, 1, 22, 0, 0);
    public static Queue<string>? priorities { get; private set; }
    public static List<DateTime> fixedIntervals { get; private set; } = new List<DateTime>();
    public static List<DateTime>? startEndIntervals { get; private set; }

    public static void closeLogFile()
    {
        Local.log("", false, false);
        wLogFile.Close();
    }

    public static void log(string message, bool canExit, bool canSendMessage)
    {
        wLogFile.WriteLine($"{(message == "" ? "" : $"[{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}]")} {message}");

        if (canSendMessage)
            Telegram.sendMessage(message);

        if (canExit)
        {
            Console.WriteLine("Exited with failure");
            Selenium.stopWebDriver(); // uncomment after debugging
            closeLogFile();
            Environment.Exit(1);
        }
    }

    public static void readPreferencesFromFile()
    {
        // unrelated, but create list of datetimes with 30 min intervals from 07:00 to 22:00 to edit preferences
        for (DateTime currentTime = startTime; currentTime < endTime; currentTime = currentTime.AddMinutes(30))
            fixedIntervals.Add(currentTime);

        using (StreamReader rPreferencesFile = new StreamReader("..\\..\\..\\Preferences.json"))
        {
            editedPreferences = readPreferences = JsonConvert.DeserializeObject<Preferences>(rPreferencesFile.ReadToEnd());
            rPreferencesFile.Close();
        }
    }

    public static void writePreferencesToFile()
    {
        using (StreamWriter wPreferencesFile = new StreamWriter("..\\..\\..\\Preferences.json"))
        {
            wPreferencesFile.WriteLine(JsonConvert.SerializeObject(editedPreferences));
            wPreferencesFile.Close();
        }
    }

    public static void setPreferences(Preferences? preferences)
    {
        if (preferences == null)
            log("Invalid preferences", true, false);

        token = new string(preferences!.token);
        chats = new Dictionary<string, string>(preferences.chats!);
        username = new string(preferences.username);
        password = new string(preferences.password);
        priorities = new Queue<string>(preferences.priorities!);

        DateTime start,
                 end;
        DateTime.TryParseExact(preferences.startTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out start);
        DateTime.TryParseExact(preferences.endTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out end);
        startTime = start;
        endTime = end;

        // create list of datetimes with 30 min intervals from start time to end time
        startEndIntervals = new List<DateTime>();
        for (DateTime currentTime = startTime; currentTime < endTime; currentTime = currentTime.AddMinutes(30))
            startEndIntervals.Add(currentTime);

        // set the date of startTime according to the preferred day (remove equal sign if want to prevent booking on same day)
        int days = (int)(preferences.dayOfWeek - startTime.DayOfWeek >= 0 ? preferences.dayOfWeek - startTime.DayOfWeek : preferences.dayOfWeek + 7 - startTime.DayOfWeek)!;
        days += days == 0 && DateTime.Now > new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, startTime.Hour, startTime.Minute, 0) ? 7 : 0;
        startTime = startTime.AddDays(days);
        endTime = endTime.AddDays(days);
    }
}