# UndetectedChromeDriver  

This repo is C# implementation of undetected_chromedriver.  

It optimizes Selenium chromedriver to avoid being detected by anti-bot services, the program will patch the specified chromedriver binary.  

### example  

```C#
// xxx is a custom directory
var userDataPath =@"D:\xxx\ChromeUserData";
var driverExecutablePath =$@"D:\xxx\chromedriver.exe";

// customized chrome options
var options = new ChromeOptions();
options.AddArgument("--mute-audio");
options.AddArgument("--disable-gpu");
options.AddArgumen("--disable-dev-shm-usage");

// using keyword is required to dispose the chrome driver
using var driver = UndetectedChromeDriverCreate(
    options: options,
    userDataDir: userDataPath,
    driverExecutablePath: driverExecutablePath);

driver.GoToUrl("https://nowsecure.nl");
```  

### parameters  

* **options:** ChromeOptions, required  
　Used to define browser behavior.

* **userDataDir:** str, required  
　Set chrome user profile directory.

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

---  