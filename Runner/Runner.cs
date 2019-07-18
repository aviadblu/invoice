using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.DrawingCore;
using System.DrawingCore.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Runner
{
    class InvoiceTextArea
    {
        public Rectangle AreaRect { set; get; }
        public Image RectImage { set; get; }
        public string ExtractedText { set; get; }
    }
    class Invoice
    {
        public string Path { set; get; }
        public Image Bitmap { set; get; }
        public DateTime Date { set; get; }
        public List<string> CNs { set; get; }
        public List<InvoiceTextArea> TextAreaList { set; get; }
    }

    class Runner
    {
        private string[] _supportedExt = { "png", "jpg" };
        public List<Invoice> _invoices;
        private Capture2Text _c2t;

        public Runner()
        {
            _invoices = new List<Invoice>();
            _c2t = new Capture2Text();
        }

        public void LoadInvoices(string path)
        {

            Console.WriteLine($"Load invoices from {Path.GetFullPath(path)}");

            List<string> filesList = new List<string>();

            foreach (var ext in _supportedExt)
            {
                filesList = filesList.Concat(Directory.GetFiles(path, $"*.{ext}", SearchOption.AllDirectories).ToList()).ToList();
            }

            foreach (var imagePath in filesList)
            {
                try
                {
                    string ipath = Path.GetFullPath(imagePath);
                    _invoices.Add(new Invoice()
                    {
                        Path = ipath,
                        Bitmap = Image.FromFile(imagePath)
                    });
                    Console.WriteLine($"{imagePath} loaded");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error loading {imagePath}");
                    Console.WriteLine(e.Message);
                }
            }
        }

        public async Task ExtractRectsFromImages()
        {
            Task[] tasks = new Task[_invoices.Count];
            Console.WriteLine("Extracting rects from images");
            for (int i = 0; i < _invoices.Count; i++)
            {
                Invoice invoice = _invoices[i];
                Console.WriteLine($"----------- working on {invoice.Path}");
                tasks[i] = LoadRects(invoice);
            }

            await Task.WhenAll(tasks).ContinueWith((t) =>
            {
                Console.WriteLine("All extracting rects tasks completed");
            });
        }

        public Task LoadRects(Invoice invoice)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            string algoExePath = Path.Combine(@"C:\code\invoice\Algo\rectsExtraction.py");

            var processInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                WorkingDirectory = Directory.GetCurrentDirectory(),
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Minimized,
                Arguments = $@" C:\code\invoice\Algo\rectsExtraction.py --image ""{invoice.Path}""",
                FileName = @"C:\Users\aviad.blumenfeld\AppData\Local\Programs\Python\Python37-32\python.exe"
            };

            Process Algorocess = Process.Start(processInfo);

            Algorocess.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    JToken token = JObject.Parse(e.Data);
                    if ((string)token.SelectToken("status") == "success")
                    {
                        invoice.TextAreaList = new List<InvoiceTextArea>();
                        JArray jArr = (JArray)token.SelectToken("rects");
                        int[][] rectsArr = (from item in jArr select item.Values<int>().ToArray()).ToArray();

                        foreach (int[] rect in rectsArr)
                            invoice.TextAreaList.Add(new InvoiceTextArea() { AreaRect = new Rectangle(rect[0], rect[1], rect[2], rect[3]) });

                        tcs.SetResult(true);
                        Console.WriteLine($"{invoice.Path} rects ready");
                    }
                    else if ((string)token.SelectToken("status") == "error")
                    {
                        tcs.SetResult(false);
                    }
                }
            };
            Algorocess.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    tcs.SetResult(false);
            };
            Algorocess.Exited += (object sender, EventArgs e) =>
            {

            };
            Algorocess.BeginOutputReadLine();
            Algorocess.BeginErrorReadLine();
            Algorocess.EnableRaisingEvents = true;

            return tcs.Task;
        }

        public async Task ExtractTextFromRects()
        {
            Task[] tasks = new Task[_invoices.Count];
            Console.WriteLine("Extracting text from rects");
            for (int i = 0; i < _invoices.Count; i++)
            {
                Invoice invoice = _invoices[i];
                Console.WriteLine($"----------- working on {invoice.Path}");

                foreach (var textArea in invoice.TextAreaList)
                    tasks[i] = ExtractTextFromRect(textArea, invoice.Bitmap);
            }

            await Task.WhenAll(tasks).ContinueWith((t) =>
            {
                Console.WriteLine("All extracting rects texts tasks completed");
            });
        }

        public Task ExtractTextFromRect(InvoiceTextArea textArea, Image invoiceImage)
        {

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            using (var bmp = new Bitmap(textArea.AreaRect.Width, textArea.AreaRect.Height))
            {

                using (var gr = Graphics.FromImage(bmp))
                {
                    gr.DrawImage(invoiceImage, new Rectangle(0, 0, bmp.Width, bmp.Height), textArea.AreaRect, GraphicsUnit.Pixel);
                }
                string txt = _c2t.GetText(bmp, SupportedOCRLanguages.English);
                string extractedID = Regex.Match(txt, "\\d{9}").Value;
                int id = 0;
                if (!string.IsNullOrEmpty(extractedID))
                    id = int.Parse(extractedID);


                Match matchDate = Regex.Match(txt, @"(?:(?:31(\/|-|\.)(?:0?[13578]|1[02]))\1|(?:(?:29|30)(\/|-|\.)(?:0?[1,3-9]|1[0-2])\2))(?:(?:1[6-9]|[2-9]\d)?\d{2})(?=\W)|\b(?:29(\/|-|\.)0?2\3(?:(?:(?:1[6-9]|[2-9]\d)?(?:0[48]|[2468][048]|[13579][26])?|(?:(?:16|[2468][048]|[3579][26])00)?)))(?=\W)|\b(?:0?[1-9]|1\d|2[0-8])(\/|-|\.)(?:(?:0?[1-9])|(?:1[0-2]))(\4)?(?:(?:1[6-9]|[2-9]\d)?\d{2})?(?=\b)");
                Console.WriteLine($"============== new row {txt}");
                DateTime date;
                if (matchDate.Success)
                {
                    DateTime.TryParse(matchDate.Groups[0].Value, out date);
                    Console.WriteLine($"Maybe date: {date}");
                }

                if (id > 0)
                {
                    Console.WriteLine($"Maybe ID: {id}");
                }


                tcs.SetResult(true);
            }
            return tcs.Task;
        }

        //public void ExtractDate()
        //{
        //    Console.WriteLine("Extract date");
        //    foreach (var image in _invoices)
        //    {
        //        Match match = Regex.Match(image.ExtractedText, @"(?:(?:31(\/|-|\.)(?:0?[13578]|1[02]))\1|(?:(?:29|30)(\/|-|\.)(?:0?[1,3-9]|1[0-2])\2))(?:(?:1[6-9]|[2-9]\d)?\d{2})(?=\W)|\b(?:29(\/|-|\.)0?2\3(?:(?:(?:1[6-9]|[2-9]\d)?(?:0[48]|[2468][048]|[13579][26])?|(?:(?:16|[2468][048]|[3579][26])00)?)))(?=\W)|\b(?:0?[1-9]|1\d|2[0-8])(\/|-|\.)(?:(?:0?[1-9])|(?:1[0-2]))(\4)?(?:(?:1[6-9]|[2-9]\d)?\d{2})?(?=\b)");

        //        if (match.Success)
        //        {
        //            DateTime res;
        //            DateTime.TryParse(match.Groups[0].Value, out res);
        //            image.Date = res;
        //        }
        //    }
        //}

        public void ExtractCNs()
        {
            Console.WriteLine("Extract CNs");
            foreach (var image in _invoices)
            {

                //string res = string.Join(",", Regex.Split(image.ExtractedText, "^[0-9]{9}$"));
                //Match match = Regex.Match(image.ExtractedText, @"^[0-9]{9}$");
                //Match match = Regex.Match(image.ExtractedText, @"\d+");

                Console.WriteLine("dsdsdsd");
                //if (match.Success)
                //{
                //    DateTime res;
                //    DateTime.TryParse(match.Groups[0].Value, out res);
                //    image.Date = res;
                //}
            }
        }

    }
}
