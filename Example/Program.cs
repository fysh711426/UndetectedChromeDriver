using OpenQA.Selenium.Chrome;
using SeleniumCompat;
using System;
using System.Threading;

namespace Example
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // xxx is a custom directory
            var userDataPath = @"D:\xxx\ChromeUserData";
            var driverExecutablePath = $@"D:\xxx\chromedriver.exe";

            // customized chrome options
            var options = new ChromeOptions();
            options.AddArgument("--mute-audio");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--disable-dev-shm-usage");

            // using keyword is required to dispose the chrome driver
            using var driver = UndetectedChromeDriver.Create(
                options: options,
                userDataDir: userDataPath,
                driverExecutablePath: driverExecutablePath);

            driver.GoToUrl("https://nowsecure.nl");

            Console.ReadLine();
        }
    }
}
