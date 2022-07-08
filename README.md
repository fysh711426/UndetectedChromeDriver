# UndetectedChromeDriver  

This repo is C# implementation of [undetected_chromedriver](https://github.com/ultrafunkamsterdam/undetected-chromedriver).  

It optimizes Selenium chromedriver to avoid being detected by anti-bot services.  

### Nuget install  

```
PM> Install-Package Selenium.UndetectedChromeDriver
```

```C#
using SeleniumUndetectedChromeDriver;
```

### Example  

```C#
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
```  

> **Note**  
> If you need to reload chrome driver, you must use a new options.  

```C#
var createOptions = () =>
{
    // options cannot be shared
    var options = new ChromeOptions();
    ...
    return options;
};

driver = UndetectedChromeDriver.Create(
    options: createOptions());
driver.Dispose();

driver = UndetectedChromeDriver.Create(
    options: createOptions());
driver.Dispose();
```  

---  

### Parameters  

* **options:** ChromeOptions, optional, default: null  
　Used to define browser behavior.

* **userDataDir:** str, optional, default: null    
　Set chrome user profile directory.  
　creates a temporary profile if userDataDir is null,  
　and automatically deletes it after exiting.  

* **driverExecutablePath:** str, required  
　Set chrome driver executable file path. (patches new binary)

* **browserExecutablePath:** str, optional, default: null  
　Set browser executable file path.  
　default using $PATH to execute.  

* **logLevel:** int, optional, default: 0  
　Set chrome logLevel.  

* **headless:** bool, optional, default: false  
　Specifies to use the browser in headless mode.  
　warning: This reduces undetectability and is not fully supported.  

* **suppressWelcome:** bool, optional, default: true  
　First launch using the welcome page.  

* **hideCommandPromptWindow:** bool, optional, default: false  
Hide selenium command prompt window.

* **prefs:** Dictionary<string, object>, optional, default: null  
　Prefs is meant to store lightweight state that reflects user preferences.  
　dict value can be value or json.

---  

### Prefs example  

```C#
// dict value can be value or json
var prefs = new Dictionary<string, object>
{
    ["download.default_directory"] =
        @"D:\xxx\download",
    ["profile.default_content_setting_values"] =
        Json.DeserializeData(@"
            {
                'notifications': 1
            }
        ")
};

using var driver = UndetectedChromeDriver.Create(
    ...
    prefs: prefs);
```  

### Multiple instance example  

```C#
// options cannot be shared
var options1 = new ChromeOptions();
var options2 = new ChromeOptions();

// userDataDir cannot be shared
var userDataDir1 = @"D:\xxx\ChromeUserData1";
var userDataDir2 = @"D:\xxx\ChromeUserData2";

using var driver1 = UndetectedChromeDriver.Create(
    ...
    options: options1,
    userDataDir: userDataDir1);

using var driver2 = UndetectedChromeDriver.Create(
    ...
    options: options2,
    userDataDir: userDataDir2);
```

### WPF example  

```C#
public partial class MainWindow : Window
{
    private UndetectedChromeDriver _driver;
    public MainWindow()
    {
        InitializeComponent();
        this.Loaded += MainWindow_Loaded;
        this.Closed += MainWindow_Closed;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var driverExecutablePath = $@"D:\xxx\chromedriver.exe";

        var options = new ChromeOptions();
        options.AddArgument("--mute-audio");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--disable-dev-shm-usage");

        _driver = UndetectedChromeDriver.Create(
            options: options,
            driverExecutablePath: driverExecutablePath,
            // hide selenium command prompt window  
            hideCommandPromptWindow: true);
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _driver.Dispose();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        _driver.GoToUrl("https://nowsecure.nl");
    }
}
```

### Chrome argument example  

```C#
// Set language.
options.AddArguments("--lang=en");
```

```C#
// Set screen.
options.AddArguments("--window-size=1920,1080");
options.AddArguments("--start-maximized");
```

```C#
// Enable geolocation.
var prefs = new Dictionary<string, object>
{
    ["profile.default_content_setting_values.geolocation"] = 1
};

using var driver = UndetectedChromeDriverCreate(
    ...
    prefs: prefs);
```

```C#
// Set geolocation.
driver.ExecuteCdpCommand(
    "Emulation.setGeolocationOverride",
    new Dictionary<string, object>
    {
        ["latitude"] = 42.1408845,
        ["longitude"] = -72.5033907,
        ["accuracy"] = 100
    });
```

```C#
// Set timezone.
driver.ExecuteCdpCommand(
    "Emulation.setTimezoneOverride",
    new Dictionary<string, object>
    {
        ["timezoneId"] = "America/New_York"
    });
```

```C#
// Disable WebGL.
options.AddArgument("--disable-gpu");
options.AddArgument("--disable-software-rasterizer");
```

```C#
// Disable WebRTC.
var prefs = new Dictionary<string, object>
{
    ["webrtc.ip_handling_policy"] = "disable_non_proxied_udp",
    ["webrtc.multiple_routes_enabled"] = false,
    ["webrtc.nonproxied_udp_enabled"] = false
};
using var driver = UndetectedChromeDriverCreate(
    ...
    prefs: prefs);
```

```C#
// Add Extensions.
var extension1 = @"D:\ChromeExtensions\Extension1";
var extension2 = @"D:\ChromeExtensions\Extension2";
options.AddArgument($"--load-extension={extension1},{extension2}");
```