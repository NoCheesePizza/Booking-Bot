using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

public class Selenium
{
    protected static IWebDriver driver = new ChromeDriver();

    protected static void clickJavascript(IWebElement element)
    {
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", element);
    }

    protected static void sendScreenshot(string caption = "")
    {
        ((ITakesScreenshot)driver).GetScreenshot().SaveAsFile("screenshot.jpg");
        Telegram.sendPhoto("screenshot.jpg", caption);
    }

    protected static void clickWhenDisplayed(string xpath, bool isJavascript)
    {
        while (true)
            try
            {
                if (isJavascript)
                    clickJavascript(driver.FindElement(By.XPath(xpath)));
                else
                    driver.FindElement(By.XPath(xpath)).Click();
                break;
            }
            catch
            { }

        Console.WriteLine($"{xpath} is clicked");
    }

    /// <summary>Waits for an element to appear on screen</summary>
    /// <param name="xpath">the xpath of the element to check</param>
    /// <param name="canCheckOkButton">whether or not to check for a potential OK button</param>
    /// <returns>True if an OK button was found, which is usually indicative of an error</returns>
    protected static bool waitUntilDisplayed(string xpath, bool canCheckOkButton)
    {
        while (true)
        {
            try
            {
                IWebElement element = driver.FindElement(By.XPath(xpath));
                if (element.Displayed && element.Size.Width > 0 && element.Size.Height > 0)
                    break;
            }
            catch
            { }

            if (canCheckOkButton)
                try
                {
                    driver.FindElement(By.XPath("//*[@id=\"ModalFooter\"]/button")).Click();
                    string errorMessage = "No error message.";

                    try
                    { errorMessage = driver.FindElement(By.XPath("//*[@id=\"ModalBody\"]")).Text; }
                    catch
                    { /* no error message */ }

                    Local.log($"The OK button was clicked. Reason: {errorMessage}.", false, true);
                    return true;
                }
                catch
                { }
        }

        Console.WriteLine($"{xpath} is displayed");
        return false;
    }

    public static void stopWebDriver()
    {
        driver.Quit();
    }
}
