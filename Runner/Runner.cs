using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.DrawingCore;
using System.DrawingCore.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Security.Cryptography;

namespace Runner
{
    public enum TextAreaType
    {
        NONE,
        ID,
        DATE,
        AMOUNT
    }

    class InvoiceTextArea
    {
        public Rectangle AreaRect { set; get; }
        public Image RectImage { set; get; }
        public string ExtractedText { set; get; }
        public TextAreaType TextAreaType { set; get; } = TextAreaType.NONE;
        public int Id { set; get; }
        public float Amount { set; get; }
        public DateTime Date { set; get; }
        public bool Redundent { set; get; } = true;
    }
    class Invoice
    {
        public string Path { set; get; }
        // generated as first OCR element hash
        public string UniqueName { set; get; }
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
                tasks[i] = LoadRects(invoice);
                tasks[i].Wait();
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

                        var tasks = new List<Task<bool>>();
                        int relevantBlocks = 0;
                        Console.WriteLine($"Working on {invoice.Path}");
                        for (int i = 0; i < rectsArr.Length; i++)
                        {
                            Console.Write("\rAnalyzing area [{0}/{1}]                              ", i + 1, rectsArr.Length);
                            int[] rect = rectsArr[i];
                            var ta = new InvoiceTextArea() { AreaRect = new Rectangle(rect[0], rect[1], rect[2], rect[3]) };

                            ta = ExtractTextFromRectSync(ta, invoice.Bitmap);
                            if (!ta.Redundent)
                            {
                                invoice.TextAreaList.Add(ta);
                                relevantBlocks++;

                                if (string.IsNullOrEmpty(invoice.UniqueName))
                                {
                                    var hash = new SHA1Managed().ComputeHash(Encoding.UTF8.GetBytes(ta.ExtractedText));
                                    invoice.UniqueName = string.Concat(hash.Select(b => b.ToString("x2")));
                                }
                            }
                            else
                            {
                                invoice.TextAreaList.Add(ta);
                            }
                        }
                        Console.WriteLine("");
                        Console.WriteLine($"{relevantBlocks} relevant areas found.");
                        tcs.SetResult(true);

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

        public InvoiceTextArea ExtractTextFromRectSync(InvoiceTextArea textArea, Image invoiceImage)
        {
            try
            {
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

                    float amount = 0;
                    if (id == 0)
                    {
                        string extractedAmount = Regex.Match(txt, @"\d{1,3}(,\d{3})*(\.\d\d)?|\.\d\d").Value;
                        if (!string.IsNullOrEmpty(extractedAmount))
                            float.TryParse(extractedAmount, out amount);
                    }

                    Match matchDate = Regex.Match(txt, @"(?:(?:31(\/|-|\.)(?:0?[13578]|1[02]))\1|(?:(?:29|30)(\/|-|\.)(?:0?[1,3-9]|1[0-2])\2))(?:(?:1[6-9]|[2-9]\d)?\d{2})(?=\W)|\b(?:29(\/|-|\.)0?2\3(?:(?:(?:1[6-9]|[2-9]\d)?(?:0[48]|[2468][048]|[13579][26])?|(?:(?:16|[2468][048]|[3579][26])00)?)))(?=\W)|\b(?:0?[1-9]|1\d|2[0-8])(\/|-|\.)(?:(?:0?[1-9])|(?:1[0-2]))(\4)?(?:(?:1[6-9]|[2-9]\d)?\d{2})?(?=\b)");

                    if (matchDate.Success)
                    {
                        DateTime date;
                        DateTime.TryParse(matchDate.Groups[0].Value, out date);
                        textArea.Date = date;
                        textArea.Redundent = false;
                        textArea.TextAreaType = TextAreaType.DATE;
                    }

                    if (id > 0)
                    {
                        textArea.Id = id;
                        textArea.Redundent = false;
                        textArea.TextAreaType = TextAreaType.ID;
                    }

                    if (amount > 0)
                    {
                        textArea.Amount = amount;
                        textArea.Redundent = false;
                        textArea.TextAreaType = TextAreaType.AMOUNT;
                    }

                    textArea.ExtractedText = txt;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return textArea;
        }

        public void CreateOutputFiles(string destPath)
        {

            foreach (var invoice in _invoices)
            {
                if (string.IsNullOrEmpty(invoice.UniqueName))
                {
                    Console.WriteLine("No name for invoice, sakipping...");
                }
                else
                {
                    Graphics graphics = Graphics.FromImage(invoice.Bitmap);
                    StringFormat drawFormat = new StringFormat();
                    SolidBrush textDrawBrush = new SolidBrush(Color.Red);
                    SolidBrush rectDrawBrush = new SolidBrush(Color.FromArgb(200, 255, 255, 102));
                    foreach (var ta in invoice.TextAreaList)
                    {

                        Color clr = ta.Redundent ? Color.Green : Color.Red;
                        string text = "";
                        switch (ta.TextAreaType)
                        {
                            case TextAreaType.ID:
                                text = $"I {ta.Id}";
                                break;
                            case TextAreaType.DATE:
                                text = $"D {ta.Date}";
                                break;
                            case TextAreaType.AMOUNT:
                                text = $"A {ta.Amount}";
                                break;
                        }
                        using (Pen thick_pen = new Pen(clr, 1))
                        {
                            if (!ta.Redundent)
                            {
                                graphics.FillRectangle(rectDrawBrush, new Rectangle(ta.AreaRect.X + 1, ta.AreaRect.Y + 1, ta.AreaRect.Width - 2, ta.AreaRect.Height - 2));
                                graphics.DrawString(text, new Font("Arial", 8), textDrawBrush, ta.AreaRect.X, ta.AreaRect.Y, drawFormat);
                            }
                            graphics.DrawRectangle(thick_pen, ta.AreaRect);
                        }
                    }

                    invoice.Bitmap.Save(Path.Combine(destPath, $"{invoice.UniqueName}.png"), ImageFormat.Png);
                    string jsonText = JsonConvert.SerializeObject(invoice);
                    using (StreamWriter file = new StreamWriter(Path.Combine(destPath, $"{invoice.UniqueName}.json")))
                    {
                        file.Write(jsonText);
                    }
                }
            }
        }

        public string GetInvoicesAsJson()
        {
            return JsonConvert.SerializeObject(_invoices);
        }
    }
}
