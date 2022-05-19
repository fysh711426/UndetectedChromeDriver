using OpenQA.Selenium.Chrome;
using SeleniumCompat;
using System;
using System.Collections.Generic;
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

            // customized chrome options
            var options = new ChromeOptions();
            options.AddArgument("--mute-audio");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--disable-dev-shm-usage");

            // dict value can be value or json
            var prefs = new Dictionary<string, object>
            {
                ["download.default_directory"] =
                    @"D:\xxx\download",
                ["profile.default_content_setting_values"] =
                    @"
                        {
                            'notifications': 1
                        }
                    "
            };

            // using keyword is required to dispose the chrome driver
            using var driver = UndetectedChromeDriver.Create(
                options: options,
                driverExecutablePath: driverExecutablePath,
                prefs: prefs);

            driver.GoToUrl("https://nowsecure.nl");

            Console.ReadLine();
        }
    }
}
