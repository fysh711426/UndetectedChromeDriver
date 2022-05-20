# UndetectedChromeDriver  

This repo is C# implementation of undetected_chromedriver.  

It optimizes Selenium chromedriver to avoid being detected by anti-bot services, the program will patch the specified chromedriver binary.  

### example  

```C#
// xxx is a custom directory
var driverExecutablePath =$@"D:\xxx\chromedriver.exe";

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

### parameters  

* **options:** ChromeOptions, optional, default: null  
　Used to define browser behavior.

* **userDataDir:** str, optional, default: null (creates temp profile)    
　Set chrome user profile directory.  
　if userDataDir is temp profile, it will be automatically deleted after exit.  

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

* **prefs:** Dictionary<string, object>, optional, default: null  
　Prefs is meant to store lightweight state that reflects user preferences.  
　dict value can be value or json.

---  

### prefs example  

```C#
// xxx is a custom directory
var driverExecutablePath =$@"D:\xxx\chromedriver.exe";

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
```  

### multiple instance example  

```C#
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
```