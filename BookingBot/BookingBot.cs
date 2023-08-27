using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Text.RegularExpressions;
using System.Globalization;
using System;

public class BookingTelegram : Telegram
{
    static Dictionary<string, Action> functions = new Dictionary<string, Action>
    {
        { "mainMenu", mainMenu },
        { "confirmCheckAvailability", confirmCheckAvailability },
        { "checkAvailability", checkAvailability },
        { "confirmBookRoom", confirmBookRoom },
        { "bookRoom", bookRoom },
        { "viewBookings", viewBookings },
        { "viewPreferences", viewPreferences },
        { "editPreferences", editPreferences },
        { "stopBot", stopBot },
        { "editStartTime", editStartTime },
        { "editEndTime", editEndTime },
        { "editDayOfWeek", editDayOfWeek },
        { "save", save }
    };

    static void mainMenu()
    {
        functions[sendInlineButtons
        (
            new List<List<InlineButton>>
            {
                new List<InlineButton> { new InlineButton("Check availability", "confirmCheckAvailability") },
                new List<InlineButton> { new InlineButton("Book room", "confirmBookRoom") },
                new List<InlineButton> { new InlineButton("View and cancel bookings", "viewBookings") },
                new List<InlineButton> { new InlineButton("View preferences", "viewPreferences") },
                new List<InlineButton> { new InlineButton("Edit preferences", "editPreferences") },
                new List<InlineButton> { new InlineButton("Stop bot", "stopBot") }
            },
            "Select an action."
        )
        ].Invoke();
    }

    static void confirmCheckAvailability()
    {
        functions[sendInlineButtons
        (
            new List<List<InlineButton>>
            {
                new List<InlineButton>
                {
                    new InlineButton("Yes", "checkAvailability"),
                    new InlineButton("No", "mainMenu")
                }
            },
            $"Do you wish to check the availability of SP's discussion rooms for " +
            $"{Local.startTime.ToString("dddd, dd/MM/yyyy")} from " +
            $"{Local.startTime.ToString("HH:mm")} to " +
            $"{Local.endTime.ToString("HH:mm")}?"
        )
        ].Invoke();
    }

    static void checkAvailability()
    {
        sendMessage("Checking availability...");
        BookingSelenium.checkAvailability();
        mainMenu();
    }

    static void confirmBookRoom()
    {
        if (Local.startTime.Hour * 60 + Local.startTime.Minute >= Local.endTime.Hour * 60 + Local.endTime.Minute)
        {
            sendMessage("The end time cannot be less than or equal to the start time.");
            mainMenu();
            return;
        }

        int difference = BookingSelenium.minutesLeft - (int)(Local.endTime - Local.startTime).TotalMinutes;
        if (difference < 0)
        {
            sendMessage($"You are short of {-difference} minutes for the specified time period.");
            mainMenu();
            return;
        }

        functions[sendInlineButtons
        (
            new List<List<InlineButton>>
            {
                new List<InlineButton>
                {
                    new InlineButton("Yes", "bookRoom"),
                    new InlineButton("No", "mainMenu")
                }
            },
            $"Do you wish to book a room on " +
            $"{Local.startTime.ToString("dddd, dd/MM/yyyy")} from " +
            $"{Local.startTime.ToString("HH:mm")} to " +
            $"{Local.endTime.ToString("HH:mm")}? " +
            $"You have {BookingSelenium.minutesLeft} minutes remaining."
        )
        ].Invoke();
    }

    static void bookRoom()
    {
        sendMessage("Booking room...");
        BookingSelenium.bookRoom();
        mainMenu();
    }

    static void viewBookings()
    {
        List<string> bookedRooms = BookingSelenium.viewBookings();

        string action = sendInlineButtons
        (
            Enumerable.Range(0, bookedRooms.Count).Select
            (
                x => new List<InlineButton>
                {
                    new InlineButton(bookedRooms[x], (x + 1).ToString())
                }
            ).Concat
            (
                new List<List<InlineButton>>
                {
                    new List<InlineButton>
                    {
                        new InlineButton("None", "mainMenu")
                    }
                }
                ).ToList(),
            "Select a booking that you would like to cancel."
        );

        if (action == "mainMenu")
            mainMenu();
        else
            confirmCancelBooking(Convert.ToInt32(action), bookedRooms[Convert.ToInt32(action) - 1]);
    }

    static void confirmCancelBooking(int index, string booking)
    {
        if 
        (sendInlineButtons
        (
        new List<List<InlineButton>>
        {
            new List<InlineButton>
            {
                new InlineButton("Yes", "yes"),
                new InlineButton("No", "no")
            }
        },
        $"Are you sure you want to cancel {booking}?"
        ) == "yes")
            cancelBooking(index, booking);
        else
            mainMenu();
    }

    static void cancelBooking(int index, string booking)
    {
        BookingSelenium.cancelBooking(index);
        sendMessage($"{booking} was successfully cancelled.");
        mainMenu();
    }

    static void viewPreferences()
    {
        sendMessage
        (
            $"Start time: {Local.startTime.ToString("HH:mm")}\n\n" +
            $"End time: {Local.endTime.ToString("HH:mm")}\n\n" +
            $"Day of week: {Local.startTime.ToString("dddd")}\n\n" +
            $"Priority list: {string.Join(" > ", Local.priorities!.Select(x => x.Substring(5, 2)))}\n\n" +
            $"Do note that the period between the start and end times constitutes a half-open range, " +
            $"e.g. for 07:00 to 09:00, only timeslots 07:00, 07:30, 08:00, and 08:30 will be selected."
        );

        mainMenu();
    }

    static void editPreferences()
    {
        functions[sendInlineButtons
        (
            new List<List<InlineButton>>
            {
                new List<InlineButton> { new InlineButton("Edit start time", "editStartTime") },
                new List<InlineButton> { new InlineButton("Edit end time", "editEndTime") },
                new List<InlineButton> { new InlineButton("Edit day of week", "editDayOfWeek") },
                new List<InlineButton> { new InlineButton("Save", "save") }
            },
            "Select an action."
        )
        ].Invoke();
    }

    static void stopBot()
    {
        Local.log("Booking Bot has been stopped.", false, true);
    }

    static void editStartTime()
    {
        Local.editedPreferences!.startTime = sendInlineButtons
        (
            Enumerable.Range(0, Local.fixedIntervals.Count / 2).Select
            (x => new List<InlineButton>
            {
                new InlineButton(Local.fixedIntervals[x * 2].ToString("HH:mm"), Local.fixedIntervals[x * 2].ToString("HH:mm")),
                new InlineButton(Local.fixedIntervals[x * 2 + 1].ToString("HH:mm"), Local.fixedIntervals[x * 2 + 1].ToString("HH:mm"))
            }
            ).ToList(),
            "Select a new start time."
        );

        sendMessage($"The start time has been changed to {Local.editedPreferences.startTime}.");
        editPreferences();
    }

    static void editEndTime()
    {
        Local.editedPreferences!.endTime = sendInlineButtons
        (
            Enumerable.Range(0, Local.fixedIntervals.Count / 2).Select
            (x => new List<InlineButton>
            {
                new InlineButton(Local.fixedIntervals[x * 2].ToString("HH:mm"), Local.fixedIntervals[x * 2].ToString("HH:mm")),
                new InlineButton(Local.fixedIntervals[x * 2 + 1].ToString("HH:mm"), Local.fixedIntervals[x * 2 + 1].ToString("HH:mm"))
            }
            ).ToList(),
            "Select a new end time."
        );

        sendMessage($"The end time has been changed to {Local.editedPreferences.endTime}.");
        editPreferences();
    }

    static void editDayOfWeek()
    {
        Local.editedPreferences!.dayOfWeek = Convert.ToInt32(sendInlineButtons
        (
            Enumerable.Range(0, 7).Select
            (x => new List<InlineButton>
            {
                new InlineButton(((DayOfWeek)x).ToString(), x.ToString())
            }
            ).ToList(),
            "Select a new day of the week."
        ));

        sendMessage($"The day of the week has been changed to {(DayOfWeek)Local.editedPreferences.dayOfWeek}.");
        editPreferences();
    }

    static void save()
    {
        Local.writePreferencesToFile();
        Local.setPreferences(Local.editedPreferences);
        sendMessage("Your preferences have been successfully saved.");
        mainMenu();
    }

    public static void startBot()
    {
        Local.log("Booking Bot has started.", false, true);
        BookingSelenium.startWebDriver();
        mainMenu();
    }
}

public class BookingSelenium : Selenium
{
    class Room
    {
        public IWebElement element;
        public int timeslotsCount = 0;
        public List<DateTime> timeslots = new List<DateTime>();

        public Room(IWebElement _element)
        {
            element = _element;
        }
    }

    const int totalSteps = 4;
    static int currentStep = 0;

    public static int minutesLeft = 0;

    static void goToWebsite(string url) // the url of the page you're trying to go to
    {
        driver.Navigate().GoToUrl(url);

        // check if need to login
        try
        {
            // if can find username box
            driver.FindElement(By.XPath("//*[@id=\"userNameInput\"]"));
            
            // login
            driver.Navigate().GoToUrl("https://rbs.singaporetech.edu.sg/DHB001/DHB001Page");
            driver.FindElement(By.XPath("//*[@id=\"userNameInput\"]")).SendKeys(Local.username);
            driver.FindElement(By.XPath("//*[@id=\"passwordInput\"]")).SendKeys(Local.password);
            clickWhenDisplayed("//*[@id=\"submitButton\"]", false);

            driver.Navigate().GoToUrl(url);
        }
        catch
        { }
    }

    static void getMinutesLeft()
    {
        goToWebsite("https://rbs.singaporetech.edu.sg/MQT001/MQT001Page");
        waitUntilDisplayed("//*[@id=\"tblMyQuota\"]/tbody/tr[1]/td[6]", false);
        minutesLeft = Convert.ToInt32(driver.FindElement(By.XPath("//*[@id=\"tblMyQuota\"]/tbody/tr[1]/td[6]")).Text);
    }

    static void resetCurrentStep()
    {
        currentStep = 0;
    }

    static void sendCurrentStep(string stepName)
    {
        Telegram.sendMessage($"[{++currentStep}/{totalSteps}]: {stepName}");
    }

    static void enterPreferences()
    {
        goToWebsite("https://rbs.singaporetech.edu.sg/SRB001/SRB001Page");

        while (true)
            try
            {
                // input date
                Thread.Sleep(1000);
                IWebElement date = driver.FindElement(By.XPath("//*[@id=\"searchSlotDate\"]"));
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].removeAttribute('readonly');", date);
                Thread.Sleep(1000);
                date.Clear();
                date.SendKeys(Local.startTime.ToString("dd MMM yyyy"));

                // select venue
                Thread.Sleep(1000);
                SelectElement venue = new SelectElement(driver.FindElement(By.XPath("//*[@id=\"resourceType\"]")));
                venue.SelectByText("Discussion Room");

                // check if inputted values are correct
                if (date.GetAttribute("value") == Local.startTime.ToString("dd MMM yyyy") && venue.SelectedOption.Text == "Discussion Room")
                    break;
            }
            catch
            { }

        // click on "search" button and wait for the page to load
        clickWhenDisplayed("//*[@id=\"btnBookingMainSearch\"]", false);
        waitUntilDisplayed("//*[@id=\"btnBooking\"]", false); // count(div) < 890

        // get automated start time (RBS will automatically set the start time to the next half hour)
        DateTime autoStartTime = Local.startTime.Date == DateTime.Now.Date ?
                                    DateTime.Now < new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).AddHours(DateTime.Now.Hour + 0.5) ?
                                    new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).AddHours(DateTime.Now.Hour + 0.5) :
                                    new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).AddHours(DateTime.Now.Hour + 1) :
                                    new DateTime(2023, 1, 1, 7, 0, 0);

        while (true)
            try
            {
                // wait for start and end time to default to "xx:xx" and "22:00" respectively
                if (new SelectElement(driver.FindElement(By.XPath("//*[@id=\"SearchHoursFrom\"]"))).SelectedOption.Text == autoStartTime.ToString("HH:mm")
                    && new SelectElement(driver.FindElement(By.XPath("//*[@id=\"SearchHoursTo\"]"))).SelectedOption.Text == "22:00")
                    break;
                Thread.Sleep(1000);
            }
            catch
            { }
    }

    public static void startWebDriver()
    {
        getMinutesLeft();
    }

    public static void checkAvailability()
    {
        enterPreferences();

        IList<IWebElement>? buttons = null;
        IList<IWebElement> venues = driver.FindElements(By.TagName("div"));
        Dictionary<string, Room> rooms = new Dictionary<string, Room>();
        int iterations = -1;
        string message = "Discussion rooms sorted in descending order of available timeslots:\n";

        foreach (IWebElement venue in venues)
            if (Regex.IsMatch(venue.Text, "^SP-DR.{2}/Discussion Room$"))
                rooms[venue.Text.Substring(0, venue.Text.IndexOf("/"))] = new Room(venue);

        while (true)
        {
            IEnumerable<KeyValuePair<string, Room>> selectedRooms = rooms.Skip(++iterations * 10).Take(10);

            if (selectedRooms.Count() == 0)
                break;

            foreach (KeyValuePair<string, Room> selectedRoom in selectedRooms)
                clickJavascript(selectedRoom.Value.element);

            // click on "book" button and wait for page to load
            clickWhenDisplayed("//*[@id=\"btnBooking\"]", false);

            if (selectedRooms.Count() == 1) //! process takes 10 s
            {
                if (waitUntilDisplayed("//*[@id=\"divNormalBooking\"]/div[2]/span", true)) // count(div) < 891
                    return;

                // click "see more" button if it exists (only for singular room)
                try
                { clickJavascript(driver.FindElement(By.XPath("//*[@id=\"divTimeSlot\"]/div[6]/div/div"))); }
                catch
                { }

                buttons = driver.FindElements(By.TagName("div"));
                IEnumerable<IWebElement> timeslotButtons = from button in buttons
                                                            where !string.IsNullOrEmpty(button.GetAttribute("data-slttime"))
                                                            select button;

                foreach (IWebElement timeslotButton in timeslotButtons)
                {
                    DateTime timeslot; // date will default to DateTime.Now after parsing
                    if (!DateTime.TryParseExact(timeslotButton.Text.Substring(0, 5), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out timeslot))
                        continue;

                    timeslot = timeslot.AddYears(Local.startTime.Year - timeslot.Year);
                    timeslot = timeslot.AddMonths(Local.startTime.Month - timeslot.Month);
                    timeslot = timeslot.AddDays(Local.startTime.Day - timeslot.Day);

                    if (timeslot >= Local.startTime && timeslot < Local.endTime)
                        rooms[selectedRooms.First().Key].timeslots.Add(timeslot);
                }

                // click on "back" button
                clickWhenDisplayed("//*[@id=\"btnDetailEdit\"]", false);
            }   
            else //! process takes 20 s
            {
                if (waitUntilDisplayed("//*[@id=\"unselectAllSlot\"]", true)) // count(div) < 891
                    return;

                buttons = driver.FindElements(By.TagName("div"));
                IEnumerable<IWebElement> timeslotButtons = from button in buttons
                                                            where !string.IsNullOrEmpty(button.GetAttribute("data-slot"))
                                                            select button;

                foreach (IWebElement timeslotButton in timeslotButtons)
                {
                    DateTime timeslot; // date will default to DateTime.Now after parsing
                    if (!DateTime.TryParseExact(timeslotButton.Text.Substring(0, 5), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out timeslot))
                        continue;

                    timeslot = timeslot.AddYears(Local.startTime.Year - timeslot.Year);
                    timeslot = timeslot.AddMonths(Local.startTime.Month - timeslot.Month);
                    timeslot = timeslot.AddDays(Local.startTime.Day - timeslot.Day);

                    if (timeslot >= Local.startTime && timeslot < Local.endTime)
                        rooms[timeslotButton.GetAttribute("data-rsrcname")].timeslots.Add(timeslot);
                }

                // click on "back" button
                clickWhenDisplayed("//*[@id=\"btnMultipleBookingEdit\"]", false);
            }

            // click on "clear selection" button
            clickWhenDisplayed("//*[@id=\"btnclearselection\"]", false);
        }

        IEnumerable<KeyValuePair<string, Room>> sortedRooms = from room in rooms
                                                                where room.Value.timeslots.Count > 0
                                                                orderby room.Value.timeslots.Count descending
                                                                select room;

        foreach (KeyValuePair<string, Room> sortedRoom in sortedRooms)
        {
            List<DateTime> timeslots = sortedRoom.Value.timeslots;
            int count = timeslots.Count;
            DateTime startTime = new DateTime(timeslots[0].Ticks).AddMinutes(-30),
                        firstStartTime = new DateTime(timeslots[0].Ticks),
                        endTime = startTime.AddMinutes(30);

            message += $"\n{sortedRoom.Key} ({count} timeslot{(count == 1 ? "" : "s")})\n------------------------";
                
            for (int i = 0; i < count; ++i)
            {
                if (startTime.AddMinutes(30) != timeslots[i])
                {
                    message += $"\n{firstStartTime.ToString("HH:mm")} - {startTime.AddMinutes(30).ToString("HH:mm")}";
                    firstStartTime = new DateTime(timeslots[i].Ticks);

                    if (i + 1 == count)
                        message += $"\n{timeslots[i].ToString("HH:mm")} - {timeslots[i].AddMinutes(30).ToString("HH:mm")}\n";
                }
                else if (i + 1 == count)
                        message += $"\n{firstStartTime.ToString("HH:mm")} - {startTime.AddMinutes(60).ToString("HH:mm")}\n";

                startTime = new DateTime(timeslots[i].Ticks);
            }
        }

        if (message == "Discussion rooms sorted in descending order of available timeslots:\n")
            message = "All discussion rooms for the specified time period are fully booked";

        Telegram.sendMessage(message);
    }

    public static void bookRoom()
    {
        resetCurrentStep();
        enterPreferences();
        sendCurrentStep("Preferences entered");

        IList<IWebElement> venues = driver.FindElements(By.TagName("div"));
        List<IWebElement> rooms = new List<IWebElement>();
        IList<IWebElement>? buttons = null;
        IWebElement? chosenRoom = null;
        Queue<string> priorities = new Queue<string>(Local.priorities!); // deep copy

        foreach (IWebElement venue in venues)
            if (Regex.IsMatch(venue.Text, "^SP-DR.{2}/Discussion Room$"))
                rooms.Add(venue);

        if (rooms.Count == 0)
        {
            Local.log("There are no available rooms for the specified parameters", false, true);
            return;
        }

        while (true)
        {
            // find first listing in priority list that is available in rooms list
            Dictionary<string, Room> selectedRooms = new Dictionary<string, Room>();

            for (int i = 0; i < 10; ++i)
            {
                if (priorities.Count == 0 || rooms.Count == 0)
                    break;

                string currentRoom = priorities.Dequeue() + "/Discussion Room";

                foreach (IWebElement room in rooms)
                    if (currentRoom == room.Text)
                    {
                        selectedRooms[currentRoom.Substring(0, currentRoom.IndexOf("/"))] = new Room(room);
                        rooms.Remove(room);
                        break;
                    }
            }

            if (selectedRooms.Count == 0)
                break;
                
            foreach (KeyValuePair<string, Room> selectedRoom in selectedRooms)
                clickJavascript(selectedRoom.Value.element);

            // click on "book" button and wait for page to load
            clickWhenDisplayed("//*[@id=\"btnBooking\"]", false);

            if (selectedRooms.Count == 1)
            {
                if (waitUntilDisplayed("//*[@id=\"divNormalBooking\"]/div[2]/span", true)) // count(div) < 891
                    return;

                // click "see more" button if it exists (only for singular room)
                try
                { clickJavascript(driver.FindElement(By.XPath("//*[@id=\"divTimeSlot\"]/div[6]/div/div"))); }
                catch
                { }

                buttons = driver.FindElements(By.TagName("div"));
                IEnumerable<IWebElement> timeslotButtons = from button in buttons
                                                            where !string.IsNullOrEmpty(button.GetAttribute("data-slttime"))
                                                            select button;

                foreach (IWebElement timeslotButton in timeslotButtons)
                {
                    DateTime timeslot; // date will default to DateTime.Now after parsing
                    if (!DateTime.TryParseExact(timeslotButton.Text.Substring(0, 5), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out timeslot))
                        continue;

                    timeslot = timeslot.AddYears(Local.startTime.Year - timeslot.Year);
                    timeslot = timeslot.AddMonths(Local.startTime.Month - timeslot.Month);
                    timeslot = timeslot.AddDays(Local.startTime.Day - timeslot.Day);

                    if (timeslot >= Local.startTime && timeslot < Local.endTime)
                        ++selectedRooms.First().Value.timeslotsCount;
                }

                // press "back" button
                clickWhenDisplayed("//*[@id=\"btnDetailEdit\"]", false);
            }
            else
            {
                if (waitUntilDisplayed("//*[@id=\"unselectAllSlot\"]", true)) // count(div) < 891
                    return;

                buttons = driver.FindElements(By.TagName("div"));
                IEnumerable<IWebElement> timeslotButtons = from button in buttons
                                                            where !string.IsNullOrEmpty(button.GetAttribute("data-slot"))
                                                            select button;

                foreach (IWebElement timeslotButton in timeslotButtons)
                {
                    DateTime timeslot; // date will default to DateTime.Now after parsing
                    if (!DateTime.TryParseExact(timeslotButton.Text.Substring(0, 5), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out timeslot))
                        continue;

                    timeslot = timeslot.AddYears(Local.startTime.Year - timeslot.Year);
                    timeslot = timeslot.AddMonths(Local.startTime.Month - timeslot.Month);
                    timeslot = timeslot.AddDays(Local.startTime.Day - timeslot.Day);

                    if (timeslot >= Local.startTime && timeslot < Local.endTime)
                        ++selectedRooms[timeslotButton.GetAttribute("data-rsrcname")].timeslotsCount;
                }

                // press "back" button
                clickWhenDisplayed("//*[@id=\"btnMultipleBookingEdit\"]", false);
            }

            // choose the highest priority room with enough slots
            foreach (KeyValuePair<string, Room> selectedRoom in selectedRooms)
                if (selectedRoom.Value.timeslotsCount == Local.startEndIntervals.Count)
                {
                    chosenRoom = selectedRoom.Value.element;
                    break;
                }

            // click on "clear selection" button
            clickWhenDisplayed("//*[@id=\"btnclearselection\"]", false);

            // repeat loop until a suitable room has been found or no rooms are available
            if (chosenRoom != null)
                break;
        }

        if (chosenRoom == null)
        {
            Local.log("There are no available rooms for the specified parameters", false, true);
            return;
        }

        // click on "book" button and wait for page to load (everything below is singular room)
        sendCurrentStep("Room chosen");
        clickJavascript(chosenRoom);
        clickWhenDisplayed("//*[@id=\"btnBooking\"]", false); // count(div) < 1417
        if (waitUntilDisplayed("//*[@id=\"divNormalBooking\"]/div[2]/span", true))
            return;

        // click "see more" button if it exists (only for singular room)
        try
        { clickJavascript(driver.FindElement(By.XPath("//*[@id=\"divTimeSlot\"]/div[6]/div/div"))); }
        catch
        { }

        // get elements of tagname "div" and click on those that match the given interval
        buttons = driver.FindElements(By.TagName("div"));
        IEnumerable<IWebElement> toClickButtons = from button in buttons
                                                    where !string.IsNullOrEmpty(button.GetAttribute("data-slttime"))
                                                    select button;

        foreach (IWebElement toClickButton in toClickButtons)
        {
            DateTime timeslot; // date will default to DateTime.Now after parsing
            if (!DateTime.TryParseExact(toClickButton.Text.Substring(0, 5), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out timeslot))
                continue;

            timeslot = timeslot.AddYears(Local.startTime.Year - timeslot.Year);
            timeslot = timeslot.AddMonths(Local.startTime.Month - timeslot.Month);
            timeslot = timeslot.AddDays(Local.startTime.Day - timeslot.Day);

            if (timeslot >= Local.startTime && timeslot < Local.endTime)
                clickJavascript(toClickButton);
        }

        // click on "next" button and wait for next page to load
        sendCurrentStep("Timeslots selected");
        clickWhenDisplayed("//*[@id=\"btnBookingNext\"]", false);
        if (waitUntilDisplayed("//*[@id=\"noofattendee\"]", true))
            return;

        // fill in required fields and confirm booking
        while (true)
            try
            {
                driver.FindElement(By.XPath("//*[@id=\"noofattendee\"]")).SendKeys("5");
                driver.FindElement(By.XPath("//*[@id=\"purpose\"]")).SendKeys("study");
                clickWhenDisplayed("//*[@id=\"btnBookingSave\"]", false);
                break;
            }
            catch
            { }

        // click on "yes" button twice
        clickWhenDisplayed("//*[@id=\"btnConfirmBookingSave\"]", false);
        waitUntilDisplayed("//*[@id=\"Modal-emailInfoPopup\"]/div/div/div[2]/div/div[2]/div/a", false);

        try 
        { clickWhenDisplayed("//*[@id=\"btnConfirmBookingSave\"]", false); }
        catch 
        { }

        sendCurrentStep("Booking confirmed");
        Thread.Sleep(3000);

        // send screenshot via telegram api
        sendScreenshot
        (
            $"{chosenRoom.Text} has been booked " +
            $"for {Local.startTime.ToString("dd MMM yyyy")} " +
            $"from {Local.startTime.ToString("HH:mm")} " +
            $"to {Local.endTime.ToString("HH:mm")}."
        );

        getMinutesLeft();
    }

    public static List<string> viewBookings()
    {
        getMinutesLeft();
        goToWebsite("https://rbs.singaporetech.edu.sg/DHB001/DHB001Page");

        while (true)
            try
            {
                if (driver.FindElement(By.XPath("//*[@id=\"tblDashBoard\"]/tbody/tr/td")).Text != "Loading data...")
                    break;
            }
            catch
            { }

        sendScreenshot();
        List<string> bookedRooms = new List<string>();

        if (driver.FindElement(By.XPath("//*[@id=\"tblDashBoard\"]/tbody/tr/td")).Text == "No Bookings Made")
            return bookedRooms;

        // send list of confirmed rooms
        IList<IWebElement> elements = driver.FindElements(By.TagName("td"));
        for (int i = 0; i < elements.Count; i += 8)
            if (elements[i + 5].Text == "Confirmed")
                bookedRooms.Add($"{elements[i].Text} | {elements[i + 1].Text} | {elements[i + 2].Text}");

        return bookedRooms;
    }

    public static void cancelBooking(int index) // index starts from 1
    {
        // click on the "+" button to reveal the "x" button, click on that, then wait for the remark box to appear
        goToWebsite("https://rbs.singaporetech.edu.sg/DHB001/DHB001Page");
        clickWhenDisplayed($"//*[@id=\"tblDashBoard\"]/tbody/tr[{index}]/td[1]", true);
        clickWhenDisplayed("//*[@id=\"bookingcancel\"]/i", true);
        waitUntilDisplayed("//*[@id=\"cancelremark\"]", false);

        // click on "save" button and confirm booking
        driver.FindElement(By.XPath("//*[@id=\"cancelremark\"]")).SendKeys("accident");
        clickWhenDisplayed("//*[@id=\"cancelBookingSave\"]", false);
        clickWhenDisplayed("//*[@id=\"btnSaveConfirm\"]", false);
    }
}
