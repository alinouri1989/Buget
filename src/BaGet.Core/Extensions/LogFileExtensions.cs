using System;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace BaGet
{
    public static class LogFileExtensions
    {
        private const string V = "=>";

        public static void WriteToFile<T>(this T Message)
        {
            return;
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "Logs";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                string filepath = $"{path}\\ServiceLog_T_{DateTime.Now.Date.ToShortDateString().Replace('/', '_')}.txt";
                if (!File.Exists(filepath))
                {
                    // Create a file to write to.   
                    using (StreamWriter sw = File.CreateText(filepath))
                    {
                        sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} type => {typeof(T).Name}=> {JsonConvert.SerializeObject(Message)}");
                    }
                }
                else
                {
                    using (StreamWriter sw = File.AppendText(filepath))
                    {
                        sw.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} type => {typeof(T).Name}=> {JsonConvert.SerializeObject(Message)}");
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public static void WriteToFile(this string Message)
        {
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "Logs";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                string filepath = $"{path}\\ServiceLog_T_{DateTime.Now.Date.ToShortDateString().Replace('/', '_')}.txt";
                if (!File.Exists(filepath))
                {
                    // Create a file to write to.   
                    using (StreamWriter sw = File.CreateText(filepath))
                    {
                        sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} => {Message}");
                    }
                }
                else
                {
                    using (StreamWriter sw = File.AppendText(filepath))
                    {
                        sw.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} => {Message}");
                    }
                }
            }
            catch
            {
                throw;
            }
        }
        public static void WriteToFile(this Exception ex)
        {
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "Logs";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                string filepath = path + "\\ServiceLog_T_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
                if (!File.Exists(filepath))
                {
                    // Create a file to write to.   
                    using (StreamWriter sw = File.CreateText(filepath))
                    {
                        sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}{V}{JsonConvert.SerializeObject(ex)}");
                    }
                }
                else
                {
                    using (StreamWriter sw = File.AppendText(filepath))
                    {
                        sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}{V}{JsonConvert.SerializeObject(ex)}");
                    }
                }
            }
            catch
            {
                throw;
            }


        }

        public static string GetXCorrelationId(this HttpResponseHeaders keys)
        {
            //"x-correlation-id"
            if (keys != null)
            {
                if (keys.GetValues("x-correlation-id").Any())
                {
                    var xCorrelationId = keys.GetValues("x-correlation-id").FirstOrDefault();
                    return xCorrelationId;
                }
                else
                {
                    return "not found xCorrelationId";
                }

            }
            else
            {
                return "not found xCorrelationId";
            }

        }
    }
}
