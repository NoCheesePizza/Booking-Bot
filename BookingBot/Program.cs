using OpenQA.Selenium.Edge;

class Program
{
    static void Main()
    {
        // init
        Local.readPreferencesFromFile();
        Local.setPreferences(Local.readPreferences);
        Local.log($"GCC-Bot has started in {Telegram.chat} chat.", false, true);

        // update
        do
        {
            Telegram.getOffset();
        } while (Telegram.getCommand());

        // exit
        Local.log($"GCC-Bot has been stopped in {Telegram.chat} chat.", false, true);
        Selenium.stopWebDriver();
        Local.closeLogFile();
    }
}

