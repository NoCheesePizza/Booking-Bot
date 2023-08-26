using OpenQA.Selenium.Edge;
using static Booking;

class Program
{
    static void Main()
    {
        // init
        Local.openLogFile();
        Local.readPreferencesFromFile();
        Local.setPreferences(Local.readPreferences);
        Telegram.sendMessage("GCC-Bot has started.");
        Telegram.getOffset();

        // update
        while (Telegram.getCommand());

        // exit
        Telegram.sendMessage("GCC-Bot has been stopped.");
        Local.closeLogFile();
    }
}

