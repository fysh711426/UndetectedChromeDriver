using System;
using System.IO;
using System.Linq;
using System.Text;

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
                    if (line.Contains("cdc_"))
                        return false;
                }
                return true;
            }
        }

        private int patchExe()
        {
            var linect = 0;
            var replacement = genRandomCdc();

            using (var fs = new FileStream(_driverExecutablePath,
                FileMode.Open, FileAccess.ReadWrite))
            {
                var buffer = new byte[1];
                var check = new StringBuilder("....");

                var read = 0;
                while (true)
                {
                    read = fs.Read(buffer, 0, buffer.Length);
                    if (read == 0)
                        break;

                    check.Remove(0, 1);
                    check.Append((char)buffer[0]);

                    if (check.ToString() == "cdc_")
                    {
                        fs.Seek(-4, SeekOrigin.Current);
                        var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(replacement);
                        fs.Write(bytes, 0, bytes.Length);
                        linect++;
                    }
                }
            }
            return linect;
        }

        private string genRandomCdc()
        {
            var chars = "abcdefghijklmnopqrstuvwxyz";
            var random = new Random();
            var cdc = Enumerable.Repeat(chars, 26)
                .Select(s => s[random.Next(s.Length)]).ToArray();
            for (var i = 4; i <= 6; i++)
                cdc[cdc.Length - i] = char.ToUpper(cdc[cdc.Length - i]);
            cdc[2] = cdc[0];
            cdc[3] = '_';
            return new string(cdc);
        }
    }
}
