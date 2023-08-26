# Telegram Bot

*A bot to make my and my friends' lives a teeny bit more convenient*
## <mark>Usage</mark>
### <u>Running the application</u>

1. Clone the repository 
2. Import the following packages into your project via NPM (I mean it's literally called NuGet Packet Manager)
    1. RestSharp
    2. Selenium.Support
    3. Selenium.WebDriver
    4. Selenium.WebDriver.ChromeDriver
3. Modify the fields in SamplePreferences.json that contain <> 
    - If you don't have a Telegram bot, you can register one for free with BotFather
    - If you don't have an SIT or a Moodle account, I'm afraid that you won't be able to make use of the majority of the bot's services
    - To get the ID of a group chat, go to web.telegram.org, login, and locate the chat that you would like your bot to interact with, then the ID would simply be the string after the "#" in the URL
4. Rename SamplePreferences.json to Preferences.json

### <u>Using it on Telegram</u>

1. Add your bot to the group chat that you would like it to interact with
2. A list of commands and a brief description of what they do should appear when you run the program
3. Press the "/" button next to the message box to pull up the list, and click on any of the commands to run it
4. Do note that the program must be continuously running on your computer for the bot to be active (I'm using an old hand-me-down computer for my makeshift server)

## <mark>Features</mark>

### <u>Book discussion rooms</u>

This is the only feature that has been added as of v1.0. The other two below have already been developed and tested in Powershell but have yet to be ported over to C# and integrated into this catch-all Telegram bot. 

The core feature of this bot is its ability to book discussion rooms in SIT@SP, thus forgoing users the need to thrawl through SIT's cumbersome mobile website. It can pick the highest priority room that is available, and can even show users the available timeslots for each of the rooms. What's more, users can easily view and cancel their bookings at any point in time through Telegram.

### <u>Give homework report</u>

This bot can log into my Moodle account and fetch all upcoming submissions from the calendar page. Then, it can format the data nicely by grouping submissions on the same date together and send the result on Telegram for everyone in the group chat to see (and panic).

### <u>Find available badminton courts</u>

Lastly, this bot can make requests to the OnePA API (unfortunately I'm only limited to 10 per minute) to get the available timeslots for badminton courts in a select few CCs in the central or east region of Singapore (1 request per CC).

## <mark>Version Table</mark>

| Date | Version | Description |
| ---- | ------- | ----------- |
| 24/08/23 | 1.0 | Release version that was published to GitHub, contains only the first feature