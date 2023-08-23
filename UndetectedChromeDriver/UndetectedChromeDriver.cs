using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SeleniumUndetectedChromeDriver
{
    public class UndetectedChromeDriver : ChromeDriver
    {
        private UndetectedChromeDriver(ChromeDriverService service, ChromeOptions options, 
            TimeSpan commandTimeout) : base(service, options, commandTimeout) { }

        private bool _headless = false;
        private ChromeOptions? _options = null;
        private ChromeDriverService? _service = null;
        private Process? _browser = null;
        private bool _keepUserDataDir = true;
        private string? _userDataDir = null;

        /// <summary>
        /// Creates a new instance of the chrome driver.
        /// </summary>
        /// <param name="options">Used to define browser behavior.</param>
        /// <param name="userDataDir">Set chrome user profile directory.
        /// creates a temporary profile if userDataDir is null,
        /// and automatically deletes it after exiting.</param>
        /// <param name="driverExecutablePath">Set chrome driver executable file path. (patches new binary)</param>
        /// <param name="browserExecutablePath">Set browser executable file path.
        /// default using $PATH to execute.</param>
        /// <param name="port">Set the port used by the chromedriver executable. (not debugger port)</param>
        /// <param name="logLevel">Set chrome logLevel.</param>
        /// <param name="headless">Specifies to use the browser in headless mode.
        /// warning: This reduces undetectability and is not fully supported.</param>
        /// <param name="noSandbox">Set use --no-sandbox, and suppress the "unsecure option" status bar.
        /// this option has a default of true since many people seem to run this as root(....) ,
        /// and chrome does not start when running as root without using --no-sandbox flag.</param>
        /// <param name="suppressWelcome">First launch using the welcome page.</param>
        /// <param name="hideCommandPromptWindow">Hide selenium command prompt window.</param>
        /// <param name="commandTimeout">The maximum amount of time to wait for each command.
        /// default value is 60 seconds.</param>
        /// <param name="prefs">Prefs is meant to store lightweight state that reflects user preferences.
        /// dict value can be value or json.</param>
        /// <param name="configureService">Initialize configuration ChromeDriverService.</param>
        /// <returns>UndetectedChromeDriver</returns>
        public static UndetectedChromeDriver Create(
            ChromeOptions? options = null,
            string? userDataDir = null,
            string? driverExecutablePath = null,
            string? browserExecutablePath = null,
            int port = 0,
            int logLevel = 0,
            bool headless = false,
            bool noSandbox = true,
            bool suppressWelcome = true,
            bool hideCommandPromptWindow = false,
            TimeSpan? commandTimeout = null,
            Dictionary<string, object>? prefs = null,
            Action<ChromeDriverService>? configureService = null)
        {
            //----- Patcher ChromeDriver -----
            if (driverExecutablePath == null)
                throw new Exception("Parameter driverExecutablePath is required.");
            var patcher = new Patcher(
                driverExecutablePath);
            patcher.Auto();
            //----- Patcher ChromeDriver -----

            //----- Options -----
            if (options == null)
                options = new ChromeOptions();
            //----- Options -----

            //----- DebugPort -----
            if (options.DebuggerAddress != null)
                throw new Exception("Options is already used, please create new ChromeOptions.");
            var debugHost = "127.0.0.1";
            var debugPort = findFreePort();
            options.AddArgument($"--remote-debugging-host={debugHost}");
            options.AddArgument($"--remote-debugging-port={debugPort}");
            options.DebuggerAddress = $"{debugHost}:{debugPort}";
            //----- DebugPort -----

            //----- UserDataDir -----
            var keepUserDataDir = true;
            var userDataDirArg = options.Arguments
                .Select(it => Regex.Match(it,
                    @"(?:--)?user-data-dir(?:[ =])?(.*)"))
                .Select(it => it.Groups[1].Value)
                .FirstOrDefault(it => !string.IsNullOrEmpty(it));
            if (userDataDirArg != null)
                userDataDir = userDataDirArg;
            else
            {
                if (userDataDir == null)
                {
                    keepUserDataDir = false;
                    userDataDir = Path.Combine(
                        Path.GetTempPath(), Guid.NewGuid().ToString());
                }
                options.AddArgument($"--user-data-dir={userDataDir}");
            }
            //----- UserDataDir -----

            //----- Language -----
            var language = CultureInfo.CurrentCulture.Name;
            if (!options.Arguments.Any(it => it.Contains("--lang")))
                options.AddArgument($"--lang={language}");
            //----- Language -----

            //----- BinaryLocation -----
            if (browserExecutablePath == null)
            {
                var executable = new ChromeExecutable();
                browserExecutablePath = executable.GetExecutablePath();
                if (browserExecutablePath == null)
                    throw new Exception("Not found chrome.exe.");
            }
            options.BinaryLocation = browserExecutablePath;
            //----- BinaryLocation -----

            //----- SuppressWelcome -----
            if (suppressWelcome)
                options.AddArguments("--no-default-browser-check", "--no-first-run");
            //----- SuppressWelcome -----

            //----- NoSandbox -----
            if (noSandbox)
                options.AddArguments("--no-sandbox", "--test-type");
            //----- NoSandbox -----

            //----- Headless -----
            if (headless)
            {
                try
                {
                    var installer = new ChromeDriverInstaller();
                    var version = installer.GetDriverVersion(driverExecutablePath)
                        .GetAwaiter().GetResult();
                    var versionMain = version.Substring(0, version.IndexOf('.'));
                    if (int.Parse(versionMain) < 108)
                        options.AddArguments("--headless=chrome");
                    else
                        options.AddArguments("--headless=new");
                }
                catch
                {
                    options.AddArguments("--headless=new");
                }
            }
            //----- Headless -----

            options.AddArguments("--window-size=1920,1080");
            options.AddArguments("--start-maximized");
            // options.AddArguments("--no-sandbox");

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
                var filePath = Path.Combine(userDataDir, @"Default/Preferences");
                var json = File.ReadAllText(filePath,
                    Encoding.GetEncoding("ISO-8859-1"));
                var regex = new Regex(@"(?<=exit_type"":)(.*?)(?=,)");
                var exitType = regex.Match(json).Value;
                if (exitType != "" && exitType != "null")
                {
                    json = regex.Replace(json, "null");
                    File.WriteAllText(filePath, json,
                        Encoding.GetEncoding("ISO-8859-1"));
                }
            }
            catch (Exception) { }
            //----- Fix exit_type -----

            //----- Start Process -----
            var args = options.Arguments
                .Select(it => it.Trim())
                .Aggregate("", (r, it) => r + " " +
                    (it.Contains(" ") ? $"\"{it}\"" : it));
            var info = new ProcessStartInfo(options.BinaryLocation, args);
            info.UseShellExecute = false;
            info.RedirectStandardInput = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            var browser = Process.Start(info);
            if (browser == null)
                throw new Exception("Browser process start error.");
            //----- Start Process -----
            
            try
            {
                //----- Create ChromeDriver -----
                var service = ChromeDriverService.CreateDefaultService(
                    Path.GetDirectoryName(driverExecutablePath),
                    Path.GetFileName(driverExecutablePath));
                service.HideCommandPromptWindow = hideCommandPromptWindow;
                if (port != 0)
                    service.Port = port;
                if (configureService != null)
                    configureService(service);
                if (commandTimeout == null)
                    commandTimeout = TimeSpan.FromSeconds(60);
                var driver = new UndetectedChromeDriver(service, options, commandTimeout.Value);
                //----- Create ChromeDriver -----

                driver._headless = headless;
                driver._options = options;
                driver._service = service;
                driver._browser = browser;
                driver._keepUserDataDir = keepUserDataDir;
                driver._userDataDir = userDataDir;
                return driver;
            }
            catch
            {
                disposeBrowser(browser, userDataDir, keepUserDataDir);
                throw;
            }
        }

        // override this.Navigate().GoToUrl()
        public void GoToUrl(string url)
        {
            if (_headless)
                configureHeadless();
            //if (hasCdcProps())
            //    hookRemoveCdcProps();
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
                            Object.defineProperty(window, ""navigator"", {
                                Object.defineProperty(window, ""navigator"", {
                                  value: new Proxy(navigator, {
                                    has: (target, key) => (key === ""webdriver"" ? false : key in target),
                                    get: (target, key) =>
                                      key === ""webdriver""
                                        ? false
                                        : typeof target[key] === ""function""
                                        ? target[key].bind(target)
                                        : target[key],
                                  }),
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
                            Object.defineProperty(navigator, 'maxTouchPoints', {get: () => 1});
                            Object.defineProperty(navigator.connection, 'rtt', {get: () => 100});

                            // https://github.com/microlinkhq/browserless/blob/master/packages/goto/src/evasions/chrome-runtime.js
                            window.chrome = {
                                app: {
                                    isInstalled: false,
                                    InstallState: {
                                        DISABLED: 'disabled',
                                        INSTALLED: 'installed',
                                        NOT_INSTALLED: 'not_installed'
                                    },
                                    RunningState: {
                                        CANNOT_RUN: 'cannot_run',
                                        READY_TO_RUN: 'ready_to_run',
                                        RUNNING: 'running'
                                    }
                                },
                                runtime: {
                                    OnInstalledReason: {
                                        CHROME_UPDATE: 'chrome_update',
                                        INSTALL: 'install',
                                        SHARED_MODULE_UPDATE: 'shared_module_update',
                                        UPDATE: 'update'
                                    },
                                    OnRestartRequiredReason: {
                                        APP_UPDATE: 'app_update',
                                        OS_UPDATE: 'os_update',
                                        PERIODIC: 'periodic'
                                    },
                                    PlatformArch: {
                                        ARM: 'arm',
                                        ARM64: 'arm64',
                                        MIPS: 'mips',
                                        MIPS64: 'mips64',
                                        X86_32: 'x86-32',
                                        X86_64: 'x86-64'
                                    },
                                    PlatformNaclArch: {
                                        ARM: 'arm',
                                        MIPS: 'mips',
                                        MIPS64: 'mips64',
                                        X86_32: 'x86-32',
                                        X86_64: 'x86-64'
                                    },
                                    PlatformOs: {
                                        ANDROID: 'android',
                                        CROS: 'cros',
                                        LINUX: 'linux',
                                        MAC: 'mac',
                                        OPENBSD: 'openbsd',
                                        WIN: 'win'
                                    },
                                    RequestUpdateCheckStatus: {
                                        NO_UPDATE: 'no_update',
                                        THROTTLED: 'throttled',
                                        UPDATE_AVAILABLE: 'update_available'
                                    }
                                }
                            }

                            // https://github.com/microlinkhq/browserless/blob/master/packages/goto/src/evasions/navigator-permissions.js
                            if (!window.Notification) {
                                window.Notification = {
                                    permission: 'denied'
                                }
                            }

                            const originalQuery = window.navigator.permissions.query
                            window.navigator.permissions.__proto__.query = parameters =>
                                parameters.name === 'notifications'
                                    ? Promise.resolve({ state: window.Notification.permission })
                                    : originalQuery(parameters)

                            const oldCall = Function.prototype.call
                            function call() {
                                return oldCall.apply(this, arguments)
                            }
                            Function.prototype.call = call

                            const nativeToStringFunctionString = Error.toString().replace(/Error/g, 'toString')
                            const oldToString = Function.prototype.toString

                            function functionToString() {
                                if (this === window.navigator.permissions.query) {
                                    return 'function query() { [native code] }'
                                }
                                if (this === functionToString) {
                                    return nativeToStringFunctionString
                                }
                                return oldCall.call(oldToString, this)
                            }
                            // eslint-disable-next-line
                            Function.prototype.toString = functionToString
                         "
                    });
            }
        }

        /// <summary>
        /// This can be useful in case of heavy detection methods.
        /// -stops the chromedriver service which runs in the background
        /// -starts the chromedriver service which runs in the background
        /// -recreate session
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task Reconnect(int timeout = 100)
        {
            if (_service == null)
                throw new Exception("ChromeDriverService cannot be null.");

            var methodInfo = typeof(DriverService).GetMethod("Stop",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (methodInfo == null)
                throw new Exception(@"Not found ChromeDriverService.Stop method.");

            try
            {
                methodInfo.Invoke(_service, new object[] { });
            }
            catch { }
            await Task.Delay(timeout);

            try
            {
                _service.Start();
            }
            catch { }

            try
            {
                StartSession();
            }
            catch { }
        }

        /// <summary>
        /// Differentiates from the regular method in that it does not
        /// require a capabilities argument.The capabilities are automatically
        /// recreated from the options at creation time.
        /// </summary>
        /// <param name="capabilities"></param>
        /// <exception cref="Exception"></exception>
        public new void StartSession(ICapabilities? capabilities = null)
        {
            if (_options == null)
                throw new Exception("ChromeOptions cannot be null.");
            if (capabilities == null)
                capabilities = _options.ToCapabilities();
            base.StartSession(capabilities);
        }

        //private bool hasCdcProps()
        //{
        //    var props = (ReadOnlyCollection<object>)ExecuteScript(
        //        @"
        //            let objectToInspect = window,
        //                result = [];
        //            while(objectToInspect !== null)
        //            { result = result.concat(Object.getOwnPropertyNames(objectToInspect));
        //              objectToInspect = Object.getPrototypeOf(objectToInspect); }
        //            return result.filter(i => i.match(/^([a-zA-Z]){27}(Array|Promise|Symbol)$/ig))
        //         ");
        //    return props.Count > 0;
        //}

        //private void hookRemoveCdcProps()
        //{
        //    ExecuteCdpCommand(
        //        "Page.addScriptToEvaluateOnNewDocument",
        //        new Dictionary<string, object>
        //        {
        //            ["source"] =
        //            @"
        //                let objectToInspect = window,
        //                    result = [];
        //                while(objectToInspect !== null) 
        //                { result = result.concat(Object.getOwnPropertyNames(objectToInspect));
        //                  objectToInspect = Object.getPrototypeOf(objectToInspect); }
        //                result.forEach(p => p.match(/^([a-zA-Z]){27}(Array|Promise|Symbol)$/ig)
        //                                    &&delete window[p]&&console.log('removed',p))
        //             "
        //        });
        //}

        private static int findFreePort()
        {
            var socket = new Socket(
                AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                var localEP = new IPEndPoint(IPAddress.Any, 0);
                socket.Bind(localEP);
                var freeEP = (IPEndPoint?)socket.LocalEndPoint;
                if (freeEP == null)
                    throw new Exception("Not found free port.");
                return freeEP.Port;
            }
            finally
            {
                socket.Close();
            }
        }

        protected override void Dispose(bool disposing)
        {
            //_service.Dispose();
            base.Dispose(disposing);
            if (disposing)
                disposeBrowser(_browser, _userDataDir, _keepUserDataDir);
        }

        private static void disposeBrowser(
            Process? browser, string? userDataDir, bool keepUserDataDir)
        {
            try
            {
                if (browser != null)
                {
                    browser.Kill();
                    browser.Dispose();
                }
            }
            catch (Exception) { }

            if (!keepUserDataDir)
            {
                for (var i = 0; i < 5; i++)
                {
                    try
                    {
                        if (userDataDir != null)
                        {
                            if (Directory.Exists(userDataDir))
                                Directory.Delete(userDataDir, true);
                        }
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
                using (var reader = new StreamReader(fs, Encoding.GetEncoding("ISO-8859-1")))
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
            void undotMerge(string key, object value, Dictionary<string, object> dict)
            {
                if (key.Contains("."))
                {
                    var split = key.Split(new char[] { '.' }, 2);
                    var k1 = split[0];
                    var k2 = split[1];
                    if (!dict.ContainsKey(k1))
                        dict[k1] = new Dictionary<string, object>();
                    undotMerge(k2, value, (Dictionary<string, object>)dict[k1]);
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
            using (var writer = new StreamWriter(fs, Encoding.GetEncoding("ISO-8859-1")))
            {
                var json = JsonConvert.SerializeObject(newPrefs);
                writer.Write(json);
            }
        }
    }
}
