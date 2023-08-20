using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SeleniumUndetectedChromeDriver
{
    // refer: https://swimburger.net/blog/dotnet/download-the-right-chromedriver-version-and-keep-it-up-to-date-on-windows-linux-macos-using-csharp-dotnet
    public class ChromeDriverInstaller
    {
        public async Task<string> Auto(string browserExecutablePath = null, bool force = false)
        {
            return await Install(await ChromeDriverInstaller.GetDynamicVersionAsync(), force);
        }
        private static async Task<string> GetDynamicVersionAsync()
        {
            HttpClient client = new HttpClient();   // actually only one object should be created by Application
            using (HttpResponseMessage response = await client.GetAsync("https://googlechromelabs.github.io/chrome-for-testing/"))
            {
                using (HttpContent content = response.Content)
                {
                    string pageContent = await content.ReadAsStringAsync();
                    string searchtext = "chrome/chrome-for-testing/";
                    int firstid = pageContent.IndexOf(searchtext);
                    if (firstid != -1)
                    {
                        firstid = firstid + searchtext.Count();
                        int lastid = firstid;
                        while (lastid < pageContent.Count() && pageContent[lastid] != '/')
                        {
                            lastid++;
                        }
                        return pageContent.Substring(firstid, lastid - firstid);
                    }
                }
            }
            return String.Empty;
        }
        public async Task<string> Install(string version, bool force = false)
        {
            if (string.IsNullOrWhiteSpace(version))
                throw new Exception("Parameter version is required.");
            // version = version.Substring(0, version.LastIndexOf('.'));

            var zipName = "";
            var path = "";
            var driverName = "";
            var tempPath = "";
            var ext = "";

#if (NET48 || NET47 || NET46 || NET45)
            zipName = "chromedriver_win32.zip";
            driverName = $"chromedriver_{version}.exe";
            tempPath = "AppData/Roaming/UndetectedChromeDriver";
            ext = ".exe";
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                zipName = "chromedriver-win64.zip";
                path = "win64";
                driverName = $"chromedriver-win64";
                tempPath = "AppData/Roaming/UndetectedChromeDriver";
                ext = ".exe";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                zipName = "chromedriver-linux64.zip";
                path = "linux64";
                driverName = $"chromedriver-linux64";
                tempPath = ".local/share/UndetectedChromeDriver";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                zipName = "chromedriver-mac-x64.zip";
                path = "mac-x64";
                driverName = $"chromedriver-mac-x64";
                tempPath = "Library/Application Support/UndetectedChromeDriver";
            }
            else
            {
                throw new PlatformNotSupportedException("Your operating system is not supported.");
            }
#endif

            var driverPath = Path.GetFullPath(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), tempPath, driverName));

            if (!force && File.Exists(driverPath))
                return driverPath;

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri($"https://edgedl.me.gvt1.com/edgedl/chrome/chrome-for-testing/");


                var dirPath = Path.GetDirectoryName(driverPath);
                if (dirPath == null)
                    throw new Exception("Get ChromeDriver directory faild.");
                if (!Directory.Exists(dirPath))
                    Directory.CreateDirectory(dirPath);

                if (force)
                {
                    void deleteDriver()
                    {
                        if (File.Exists(driverPath))
                            File.Delete(driverPath);
                    }
                    try
                    {
                        deleteDriver();
                    }
                    catch
                    {
                        try
                        {
                            await forceKillInstances(driverPath);
                            deleteDriver();
                        }
                        catch { }
                    }
                }

                // Use the URL created in the last step to retrieve a small file containing the version of ChromeDriver to use. For example, the above URL will get your a file containing "72.0.3626.69". (The actual number may change in the future, of course.)
                // Use the version number retrieved from the previous step to construct the URL to download ChromeDriver. With version 72.0.3626.69, the URL would be "https://chromedriver.storage.googleapis.com/index.html?path=72.0.3626.69/".
                var zipResponse = await httpClient.GetAsync($"{version}/{path}/{zipName}");
                if (!zipResponse.IsSuccessStatusCode)
                    throw new Exception($"ChromeDriver download request failed with status code: {zipResponse.StatusCode}, reason phrase: {zipResponse.ReasonPhrase}");

                // this reads the zipfile as a stream, opens the archive, 
                // and extracts the chromedriver executable to the targetPath without saving any intermediate files to disk
                using (var zipStream = await zipResponse.Content.ReadAsStreamAsync())
                using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read))
                using (var fs = new FileStream(driverPath, FileMode.Create))
                {
                    var entryName = $"{driverName}/chromedriver{ext}";
                    var entry = zipArchive.GetEntry(entryName);
                    if (entry == null)
                        throw new Exception($"Not found zip entry {entryName}.");
                    using (var stream = entry.Open())
                    {
                        await stream.CopyToAsync(fs);
                    }
                }

#if (NET48 || NET47 || NET46 || NET45)
#else
                // on Linux/macOS, you need to add the executable permission (+x) to allow the execution of the chromedriver
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    var args = $@"+x ""{driverPath}""";
                    var info = new ProcessStartInfo("chmod", args);
                    info.CreateNoWindow = true;
                    info.UseShellExecute = false;
                    info.RedirectStandardOutput = true;
                    info.RedirectStandardError = true;
                    var process = Process.Start(info);
                    if (process == null)
                        throw new Exception("Process start error.");
                    try
                    {
                        var error = await process.StandardError.ReadToEndAsync();
                        await process.WaitForExitPatchAsync();
                        process.Kill();
                        process.Dispose();

                        if (!string.IsNullOrEmpty(error))
                            throw new Exception("Failed to make chromedriver executable.");
                    }
                    catch
                    {
                        process.Dispose();
                        throw;
                    }
                }
#endif
            }

            return driverPath;
        }

        public async Task<string> GetDriverVersion(string driverExecutablePath)
        {
            if (driverExecutablePath == null)
                throw new Exception("Parameter driverExecutablePath is required.");

            var args = "--version";
            var info = new ProcessStartInfo(driverExecutablePath, args);
            info.CreateNoWindow = true;
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            var process = Process.Start(info);
            if (process == null)
                throw new Exception("Process start error.");
            try
            {
                var version = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitPatchAsync();
                process.Kill();
                process.Dispose();

                if (!string.IsNullOrEmpty(error))
                    throw new Exception("Failed to execute {driverName} --version.");
                return version.Split(' ').Skip(1).First();
            }
            catch
            {
                process.Dispose();
                throw;
            }
        }

        private async Task forceKillInstances(string driverExecutablePath)
        {
            var exeName = Path.GetFileName(driverExecutablePath);

            var cmd = "";
            var args = "";

#if (NET48 || NET47 || NET46 || NET45)
            cmd = "taskkill";
            args = $"/f /im {exeName}";
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                cmd = "taskkill";
                args = $"/f /im {exeName}";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                cmd = "kill";
                args = $"-f -9 $(pidof {exeName})";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                cmd = "kill";
                args = $"-f -9 $(pidof {exeName})";
            }
#endif
            var info = new ProcessStartInfo(cmd, args);
            info.CreateNoWindow = true;
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            var process = Process.Start(info);
            if (process == null)
                throw new Exception("Process start error.");
            try
            {
                await process.WaitForExitPatchAsync();
                process.Kill();
                process.Dispose();
            }
            catch
            {
                process.Dispose();
                throw;
            }
        }
    }
}
