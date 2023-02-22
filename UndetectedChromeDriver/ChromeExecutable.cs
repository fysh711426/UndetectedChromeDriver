using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SeleniumUndetectedChromeDriver
{
    public class ChromeExecutable
    {
        // refer: https://swimburger.net/blog/dotnet/download-the-right-chromedriver-version-and-keep-it-up-to-date-on-windows-linux-macos-using-csharp-dotnet
        public async Task<string> GetVersion()
        {
#if (NET48 || NET47 || NET46 || NET45)
            var executablePath = findChromeExecutable();
            if (executablePath == null)
                throw new Exception("Not found chrome.exe.");
            return FileVersionInfo.GetVersionInfo(executablePath).FileVersion;
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var executablePath = findChromeExecutable();
                if (executablePath == null)
                    throw new Exception("Not found chrome.exe.");
                return FileVersionInfo.GetVersionInfo(executablePath).FileVersion;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var args = "--product-version";
                var executablePath = findChromeExecutableLinux();

                var info = new ProcessStartInfo(executablePath, args);
                info.CreateNoWindow = true;
                info.UseShellExecute = false;
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;
                var process = Process.Start(info);

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitPatchAsync();
                process.Kill();

                if (!string.IsNullOrEmpty(error))
                    throw new Exception(error);
                return output;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var args = "--version";
                var executablePath = findChromeExecutableMacos();

                var info = new ProcessStartInfo(executablePath, args);
                info.CreateNoWindow = true;
                info.UseShellExecute = false;
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;
                var process = Process.Start(info);

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitPatchAsync();
                process.Kill();

                if (!string.IsNullOrEmpty(error))
                    throw new Exception(error);
                return output.Replace("Google Chrome ", "");
            }
            else
                throw new PlatformNotSupportedException("Your operating system is not supported.");
#endif
        }

        public string GetExecutablePath()
        {
            var result = null as string;
#if (NET48 || NET47 || NET46 || NET45)
            result = findChromeExecutable();
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                result = findChromeExecutable();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                result = findChromeExecutableLinux();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                result = findChromeExecutableMacos();
#endif
            return result;
        }

        private static string findChromeExecutable()
        {
            var candidates = new List<string>();

            foreach (var item in new[] {
                "PROGRAMFILES", "PROGRAMFILES(X86)", "LOCALAPPDATA", "PROGRAMW6432"
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

        private static string findChromeExecutableLinux()
        {
            var candidates = new List<string>();

            var variables = Environment.GetEnvironmentVariable("PATH")
                .Split(Path.PathSeparator);
            foreach (var item in variables)
            {
                foreach (var subitem in new[] {
                    "google-chrome",
                    "chromium",
                    "chromium-browser",
                    "chrome",
                    "google-chrome-stable",
                })
                {
                    candidates.Add(Path.Combine(item, subitem));
                }
            }

            foreach (var candidate in candidates)
                if (File.Exists(candidate))
                    return candidate;
            return null;
        }

        private static string findChromeExecutableMacos()
        {
            var candidates = new List<string>();

            var variables = Environment.GetEnvironmentVariable("PATH")
                .Split(Path.PathSeparator);
            foreach (var item in variables)
            {
                foreach (var subitem in new[] {
                    "google-chrome",
                    "chromium",
                    "chromium-browser",
                    "chrome",
                    "google-chrome-stable",
                })
                {
                    candidates.Add(Path.Combine(item, subitem));
                }
            }

            candidates.AddRange(new string[] {
                "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
                "/Applications/Chromium.app/Contents/MacOS/Chromium"
            });

            foreach (var candidate in candidates)
                if (File.Exists(candidate))
                    return candidate;
            return null;
        }
    }
}
