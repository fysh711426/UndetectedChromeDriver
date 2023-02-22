using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SeleniumUndetectedChromeDriver
{
    public class Patcher
    {
        private string _driverExecutablePath;
        public Patcher(
            string driverExecutablePath = null)
        {
            _driverExecutablePath = driverExecutablePath;
        }

        public void Auto()
        {
            if (!isBinaryPatched())
                patchExe();
        }

        private bool isBinaryPatched()
        {
            if (_driverExecutablePath == null)
                throw new Exception("driverExecutablePath is required.");

            using (var fs = new FileStream(_driverExecutablePath,
                FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(fs, Encoding.GetEncoding("ISO-8859-1")))
            {
                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                        break;
                    if (line.Contains("undetected chromedriver"))
                        return true;
                }
                return false;
            }
        }

        private void patchExe()
        {
            using (var fs = new FileStream(_driverExecutablePath,
                FileMode.Open, FileAccess.ReadWrite))
            {
                var buffer = new byte[1024];
                var stringBuilder = new StringBuilder();

                var read = 0;
                while (true)
                {
                    read = fs.Read(buffer, 0, buffer.Length);
                    if (read == 0)
                        break;
                    stringBuilder.Append(
                        Encoding.GetEncoding("ISO-8859-1").GetString(buffer, 0, read));
                }

                var content = stringBuilder.ToString();
                var match = Regex.Match(content.ToString(), @"\{window\.cdc.*?;\}");
                if (match.Success)
                {
                    var target = match.Value;
                    var newTarget = "{console.log(\"undetected chromedriver 1337!\")}"
                        .PadRight(target.Length, ' ');
                    var newContent = content.Replace(target, newTarget);

                    fs.Seek(0, SeekOrigin.Begin);
                    var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(newContent);
                    fs.Write(bytes, 0, bytes.Length);
                }
            }
        }

        private string genRandomCdc()
        {
            var chars = "abcdefghijklmnopqrstuvwxyz";
            var random = new Random();
            var cdc = Enumerable.Repeat(chars, 27)
                .Select(s => s[random.Next(s.Length)]).ToArray();
            //var cdc = Enumerable.Repeat(chars, 26)
            //    .Select(s => s[random.Next(s.Length)]).ToArray();
            //for (var i = 4; i <= 6; i++)
            //    cdc[cdc.Length - i] = char.ToUpper(cdc[cdc.Length - i]);
            //cdc[2] = cdc[0];
            //cdc[3] = '_';
            return new string(cdc);
        }
    }
}
