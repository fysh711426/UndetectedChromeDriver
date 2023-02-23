using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SeleniumUndetectedChromeDriver
{
    // refer: https://swimburger.net/blog/dotnet/download-the-right-chromedriver-version-and-keep-it-up-to-date-on-windows-linux-macos-using-csharp-dotnet
    public class ChromeDriverInstaller
    {
        public async Task<string> Auto(
            string? version = null, bool force = false)
        {
            if (version == null)
            {
                var executable = new ChromeExecutable();
                version = await executable.GetVersion();
            }
            version = version.Substring(0, version.LastIndexOf('.'));

            var zipName = "";
            var driverName = "";
            var tempPath = "";
            var ext = "";

#if (NET48 || NET47 || NET46 || NET45)
            zipName = $"chromedriver_win32.zip";
            driverName = $"chromedriver_{version}.exe";
            tempPath = "AppData/Roaming/UndetectedChromeDriver";
            ext = ".exe";
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                zipName = $"chromedriver_win32.zip";
                driverName = $"chromedriver_{version}.exe";
                tempPath = "AppData/Roaming/UndetectedChromeDriver";
                ext = ".exe";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                zipName = $"chromedriver_linux64.zip";
                driverName = $"chromedriver_{version}";
                tempPath = ".local/share/UndetectedChromeDriver";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                zipName = $"chromedriver_mac64.zip";
                driverName = $"chromedriver_{version}";
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

            var httpClient = Http.Client;
            httpClient.BaseAddress = new Uri("https://chromedriver.storage.googleapis.com/");

            // Append the result to URL "https://chromedriver.storage.googleapis.com/LATEST_RELEASE_". 
            // For example, with Chrome version 72.0.3626.81, you'd get a URL "https://chromedriver.storage.googleapis.com/LATEST_RELEASE_72.0.3626".
            var versionResponse = await httpClient.GetAsync($"LATEST_RELEASE_{version}");
            if (!versionResponse.IsSuccessStatusCode)
            {
                if (versionResponse.StatusCode == HttpStatusCode.NotFound)
                    throw new Exception($"ChromeDriver version not found for Chrome version {version}");
                else
                    throw new Exception($"ChromeDriver version request failed with status code: {versionResponse.StatusCode}, reason phrase: {versionResponse.ReasonPhrase}");
            }

            var driverVersion = await versionResponse.Content.ReadAsStringAsync();

            var dirPath = Path.GetDirectoryName(driverPath);
            if (dirPath == null)
                throw new Exception("Get ChromeDriver directory faild.");
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            // Use the URL created in the last step to retrieve a small file containing the version of ChromeDriver to use. For example, the above URL will get your a file containing "72.0.3626.69". (The actual number may change in the future, of course.)
            // Use the version number retrieved from the previous step to construct the URL to download ChromeDriver. With version 72.0.3626.69, the URL would be "https://chromedriver.storage.googleapis.com/index.html?path=72.0.3626.69/".
            var zipResponse = await httpClient.GetAsync($"{driverVersion}/{zipName}");
            if (!zipResponse.IsSuccessStatusCode)
                throw new Exception($"ChromeDriver download request failed with status code: {zipResponse.StatusCode}, reason phrase: {zipResponse.ReasonPhrase}");

            // this reads the zipfile as a stream, opens the archive, 
            // and extracts the chromedriver executable to the targetPath without saving any intermediate files to disk
            using (var zipStream = await zipResponse.Content.ReadAsStreamAsync())
            using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            using (var fs = new FileStream(driverPath, FileMode.Create))
            {
                var entryName = $"chromedriver{ext}";
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
                var args = @$"+x ""{driverPath}""";
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
    }
}
