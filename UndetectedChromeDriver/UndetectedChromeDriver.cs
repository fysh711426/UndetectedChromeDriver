using Newtonsoft.Json;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace SeleniumCompat
{
    public class UndetectedChromeDriver : ChromeDriver
    {
        private UndetectedChromeDriver(ChromeDriverService service,
            ChromeOptions options) : base(service, options) { }

        private bool _headless = false;
        private ChromeOptions _options = null;
        private ChromeDriverService _service = null;
        private Process _browser = null;
        private bool _keepUserDataDir = true;
        private string _userDataDir = null;

        /*
            Creates a new instance of the chrome driver.
            
            Parameters
            ----------

            options: ChromeOptions, optional, default: null 
                Used to define browser behavior.

            userDataDir: str, optional, default: null (creates temp profile)
                Set chrome user profile directory.
                if userDataDir is temp profile, it will be automatically deleted after exit.

            driverExecutablePath: str, required
                Set chrome driver executable file path. (patches new binary)

            browserExecutablePath: optional, default: null
                Set browser executable file path.
                default using $PATH to execute.

            logLevel: int, optional, default: 0
                Set chrome logLevel.

            headless: bool, optional, default: false
                Specifies to use the browser in headless mode.
                warning: This reduces undetectability and is not fully supported.

            suppressWelcome: bool, optional, default: true
                First launch using the welcome page.

            prefs: Dictionary<string, object>, optional, default: null
                Prefs is meant to store lightweight state that reflects user preferences.
                dict value can be value or json.  
        */

        public static UndetectedChromeDriver Create(
            ChromeOptions options = null,
            string userDataDir = null,
            string driverExecutablePath = null,
            string browserExecutablePath = null,
            int logLevel = 0,
            bool headless = false,
            bool suppressWelcome = true,
            Dictionary<string, object> prefs = null)
        {
            //----- Patcher ChromeDriver -----
            var patcher = new Patcher(
                driverExecutablePath);
            patcher.Auto();
            //----- Patcher ChromeDriver -----

            //----- Options -----
            if (options == null)
                options = new ChromeOptions();
            //----- Options -----

            //----- DebugPort -----
            var debugHost = "127.0.0.1";
            var debugPort = findFreePort();
            if (options.DebuggerAddress == null)
                options.DebuggerAddress = $"{debugHost}:{debugPort}";
            options.AddArgument($"--remote-debugging-host={debugHost}");
            options.AddArgument($"--remote-debugging-port={debugPort}");
            //----- DebugPort -----

            //----- UserDataDir -----
            var keepUserDataDir = true;
            if (userDataDir == null)
            {
                keepUserDataDir = false;
                userDataDir = Path.Combine(
                    Path.GetTempPath(), Guid.NewGuid().ToString());
            }
            options.AddArgument($"--user-data-dir={userDataDir}");
            //----- UserDataDir -----

            //----- Language -----
            var language = CultureInfo.CurrentCulture.Name;
            options.AddArgument($"--lang={language}");
            //----- Language -----

            //----- BinaryLocation -----
            if (browserExecutablePath == null)
            {
                browserExecutablePath = findChromeExecutable();
                if (browserExecutablePath == null)
                    throw new Exception("Not found chrome.exe.");
            }
            options.BinaryLocation = browserExecutablePath;
            //----- BinaryLocation -----

            //----- SuppressWelcome -----
            if (suppressWelcome)
                options.AddArguments("--no-default-browser-check", "--no-first-run");
            //----- SuppressWelcome -----

            //----- Headless -----
            if (headless)
            {
                options.AddArguments("--headless");
                options.AddArguments("--window-size=1920,1080");
                options.AddArguments("--start-maximized");
                options.AddArguments("--no-sandbox");
            }
            //----- Headless -----

            //----- LogLevel -----
            options.AddArguments($"--log-level={logLevel}");
            //----- LogLevel -----

            //----- Prefs -----
            if (prefs != null)
                handlePrefs(userDataDir, prefs);
            //----- Prefs -----

            //----- Fix exit_type -----
            try
            {
                var filePath = Path.Combine(userDataDir, @"Default\Preferences");
                var json = File.ReadAllText(filePath, Encoding.Latin1);
                var regex = new Regex(@"(?<=exit_type"":)(.*?)(?=,)");
                var exitType = regex.Match(json).Value;
                if (exitType != "" && exitType != "null")
                {
                    json = regex.Replace(json, "null");
                    File.WriteAllText(filePath, json, Encoding.Latin1);
                }
            }
            catch (Exception) { }
            //----- Fix exit_type -----

            //----- Start Process -----
            var info = new ProcessStartInfo(options.BinaryLocation,
                string.Join(" ", options.Arguments));
            info.UseShellExecute = false;
            info.RedirectStandardInput = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            var browser = Process.Start(info);
            //----- Start Process -----

            //----- Create ChromeDriver -----
            if (driverExecutablePath == null)
                throw new Exception("driverExecutablePath is required.");
            var service = ChromeDriverService.CreateDefaultService(
                Path.GetDirectoryName(driverExecutablePath),
                Path.GetFileName(driverExecutablePath));
            var driver = new UndetectedChromeDriver(service, options);
            //----- Create ChromeDriver -----

            driver._headless = headless;
            driver._options = options;
            driver._service = service;
            driver._browser = browser;
            driver._keepUserDataDir = keepUserDataDir;
            driver._userDataDir = userDataDir;
            return driver;
        }

        // override this.Navigate().GoToUrl()
        public void GoToUrl(string url)
        {
            if (_headless)
                configureHeadless();
            if (hasCdcProps())
                hookRemoveCdcProps();
            Navigate().GoToUrl(url);
        }

        private void configureHeadless()
        {
            if (ExecuteScript("return navigator.webdriver") != null)
            {
                ExecuteCdpCommand(
                    "Page.addScriptToEvaluateOnNewDocument",
                    new Dictionary<string, object>
                    {
                        ["source"] =
                        @"
                            Object.defineProperty(window, 'navigator', {
                                value: new Proxy(navigator, {
                                        has: (target, key) => (key === 'webdriver' ? false : key in target),
                                        get: (target, key) =>
                                                key === 'webdriver' ?
                                                false :
                                                typeof target[key] === 'function' ?
                                                target[key].bind(target) :
                                                target[key]
                                        })
                            });
                         "
                    });
                ExecuteCdpCommand(
                    "Network.setUserAgentOverride",
                    new Dictionary<string, object>
                    {
                        ["userAgent"] =
                        ((string)ExecuteScript(
                            "return navigator.userAgent"
                        )).Replace("Headless", "")
                    });
                ExecuteCdpCommand(
                    "Page.addScriptToEvaluateOnNewDocument",
                    new Dictionary<string, object>
                    {
                        ["source"] =
                        @"
                            Object.defineProperty(navigator, 'maxTouchPoints', {
                                    get: () => 1
                            });
                         "
                    });
            }
        }

        private bool hasCdcProps()
        {
            var props = (ReadOnlyCollection<object>)ExecuteScript(
                @"
                    let objectToInspect = window,
                        result = [];
                    while(objectToInspect !== null)
                    { result = result.concat(Object.getOwnPropertyNames(objectToInspect));
                      objectToInspect = Object.getPrototypeOf(objectToInspect); }
                    return result.filter(i => i.match(/.+_.+_(Array|Promise|Symbol)/ig))
                 ");
            return props.Count > 0;
        }

        private void hookRemoveCdcProps()
        {
            ExecuteCdpCommand(
                "Page.addScriptToEvaluateOnNewDocument",
                new Dictionary<string, object>
                {
                    ["source"] =
                    @"
                        let objectToInspect = window,
                            result = [];
                        while(objectToInspect !== null) 
                        { result = result.concat(Object.getOwnPropertyNames(objectToInspect));
                          objectToInspect = Object.getPrototypeOf(objectToInspect); }
                        result.forEach(p => p.match(/.+_.+_(Array|Promise|Symbol)/ig)
                                            &&delete window[p]&&console.log('removed',p))
                     "
                });
        }

        private static string findChromeExecutable()
        {
            var candidates = new List<string>();

            foreach (var item in new[] {
                "PROGRAMFILES", "PROGRAMFILES(X86)", "LOCALAPPDATA"
            })
            {
                foreach (var subitem in new[] {
                    @"Google\Chrome\Application",
                    @"Google\Chrome Beta\Application",
                    @"Google\Chrome Canary\Application"
                })
                {
                    var variable = Environment.GetEnvironmentVariable(item);
                    if (variable != null)
                        candidates.Add(Path.Combine(variable, subitem, "chrome.exe"));
                }
            }

            foreach (var candidate in candidates)
                if (File.Exists(candidate))
                    return candidate;
            return null;
        }

        private static int findFreePort()
        {
            var socket = new Socket(
                AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                var localEP = new IPEndPoint(IPAddress.Any, 0);
                socket.Bind(localEP);
                localEP = (IPEndPoint)socket.LocalEndPoint;
                return localEP.Port;
            }
            finally
            {
                socket.Close();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            try
            {
                _browser.Kill();
            }
            catch (Exception) { }

            if (!_keepUserDataDir)
            {
                for (var i = 0; i < 5; i++)
                {
                    try
                    {
                        Directory.Delete(_userDataDir, true);
                        break;
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(100);
                    }
                }
            }
        }

        private static void handlePrefs(string userDataDir, Dictionary<string, object> prefs)
        {
            var defaultPath = Path.Combine(userDataDir, "Default");
            if (!Directory.Exists(defaultPath))
                Directory.CreateDirectory(defaultPath);

            var newPrefs = new Dictionary<string, object>();

            var prefsFile = Path.Combine(defaultPath, "Preferences");
            if (File.Exists(prefsFile))
            {
                using (var fs = File.Open(prefsFile, FileMode.Open, FileAccess.Read))
                using (var reader = new StreamReader(fs, Encoding.Latin1))
                {
                    try
                    {
                        var json = reader.ReadToEnd();
                        newPrefs = Json.DeserializeData(json);
                    }
                    catch (Exception) { }
                }
            }

            // merge key value into dict
            void undotMerge(string key, object value, 
                Dictionary<string, object> dict)
            {
                if (key.Contains("."))
                {
                    var split = key.Split('.', 2);
                    var k1 = split[0];
                    var k2 = split[1];
                    if (!dict.ContainsKey(k1))
                        dict[k1] = new Dictionary<string, object>();
                    undotMerge(k2, value, dict[k1] as Dictionary<string, object>);
                    return;
                }
                dict[key] = value;
            }

            try
            {
                foreach (var pair in prefs)
                {
                    undotMerge(pair.Key, pair.Value, newPrefs);
                }
            }
            catch(Exception)
            {
                throw new Exception("Prefs merge faild.");
            }

            using (var fs = File.Open(prefsFile, FileMode.OpenOrCreate, FileAccess.Write))
            using (var writer = new StreamWriter(fs, Encoding.Latin1))
            {
                var json = JsonConvert.SerializeObject(newPrefs);
                writer.Write(json);
            }
        }
    }
}
