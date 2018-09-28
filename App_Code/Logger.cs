using System;
using System.Configuration;
using System.IO;

namespace Com.Alipay
{
    public class Logger
    {
        public static void Log(string text)
        {
            var path = ConfigurationManager.AppSettings["Path"];
            if (!File.Exists(path))
            {
                using (var file = File.Create(path))
                {
                    file.Close();
                }
            }

            using (var sw = new StreamWriter(path, true))
            {
                sw.WriteLine(string.Format("{0}: [{1}]", DateTime.Now.ToString("MM/dd/yyy HH:mm"), text));
                sw.Close();
            }
        }
    }
}