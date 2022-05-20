using OpenQA.Selenium.Chrome;
using SeleniumCompat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Example
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // xxx is a custom directory
            var driverExecutablePath = $@"D:\xxx\chromedriver.exe";

            // customized chrome options
            var options = new ChromeOptions();
            options.AddArgument("--mute-audio");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--disable-dev-shm-usage");

            // using keyword is required to dispose the chrome driver
            using var driver = UndetectedChromeDriver.Create(
                options: options,
                driverExecutablePath: driverExecutablePath);

            driver.GoToUrl("https://nowsecure.nl");

            Console.ReadLine();
        }

        public static void PrefsExample()
        {
            // xxx is a custom directory
            var driverExecutablePath = $@"D:\xxx\chromedriver.exe";

            // dict value can be value or json
            var prefs = new Dictionary<string, object>
            {
                ["download.default_directory"] =
                    @"D:\xxx\download",
                ["profile.default_content_setting_values"] =
                    Json.DeserializeData(@"
                        {
                            'notifications': 2
                        }
                    ")
            };

            using var driver = UndetectedChromeDriver.Create(
                options: new ChromeOptions(),
                driverExecutablePath: driverExecutablePath,
                userDataDir: @"D:\xxx\ChromeUserData",
                prefs: prefs);

            driver.GoToUrl("https://nowsecure.nl");

            Console.ReadLine();
        }

        public static void MultipleBrowserExample()
        {
            // xxx is a custom directory
            var driverExecutablePath = $@"D:\xxx\chromedriver.exe";

            // ChromeOptions must be independent
            var options1 = new ChromeOptions();
            var options2 = new ChromeOptions();

            using var driver1 = UndetectedChromeDriver.Create(
                options: options1,
                driverExecutablePath: driverExecutablePath,
                userDataDir: @"D:\xxx\ChromeUserData1");
            driver1.GoToUrl("https://nowsecure.nl");

            using var driver2 = UndetectedChromeDriver.Create(
                options: options2,
                driverExecutablePath: driverExecutablePath,
                userDataDir: @"D:\xxx\ChromeUserData2");
            driver2.GoToUrl("https://nowsecure.nl");

            Console.ReadLine();
        }
    }
}
