using System;
using System.IO;
using System.Diagnostics;
using System.DrawingCore;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;

namespace Runner
{
    public enum SupportedOCRLanguages
    {
        English,
        Hebrew
    }

    static class Ocr
    {
        private static string[] GetTextJS(string imagePath, Rectangle[] rects, SupportedOCRLanguages lang)
        {

            string output = "";
            if (imagePath == null)
                return null;

            if (!File.Exists(imagePath))
                return null;

            string algoExePath = Path.Combine(@"C:\code\invoice\OCR\index.js");

            string rectStr = "[";
            if (rects.Length > 0)
            {
                for (int i = 0; i < rects.Length; i++)
                {
                    if (i > 0)
                        rectStr += ",";
                    rectStr += $"[{rects[i].X},{rects[i].Y},{rects[i].Width},{rects[i].Height}]";
                }
            }
            rectStr += "]";

            var processInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                WorkingDirectory = Directory.GetCurrentDirectory(),
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Minimized,
                Arguments = $@" {algoExePath} ""{imagePath}"" ""{rectStr}""",
                FileName = @"C:\Program Files\nodejs\node.exe"
            };

            Process OCRrocess = Process.Start(processInfo);

            Console.WriteLine($@" {algoExePath} ""{imagePath}"" ""{rectStr}""");

            OCRrocess.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    output = e.Data;
                }
            };

            OCRrocess.BeginOutputReadLine();

            OCRrocess.WaitForExit();

            string[] processResponseStrings;

            try
            {
                byte[][] processResponseBytes = JsonConvert.DeserializeObject<byte[][]>(output);
                processResponseStrings = new string[processResponseBytes.Length];
                for (int i = 0; i < processResponseBytes.Length; i++)
                {
                    processResponseStrings[i] = Encoding.UTF8.GetString(processResponseBytes[i]);
                }
            }
            catch
            {
                throw new Exception("Response error!");
            }

            return processResponseStrings;
        }


        public static string[] GetText(string imagePath, SupportedOCRLanguages lang = SupportedOCRLanguages.English)
        {
            return GetTextJS(imagePath, null, lang);
        }

        public static string[] GetText(string imagePath, Rectangle[] rects, SupportedOCRLanguages lang = SupportedOCRLanguages.English)
        {
            return GetTextJS(imagePath, rects, lang);
        }
    }
}
