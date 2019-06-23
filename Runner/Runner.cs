using System;
using System.Collections.Generic;
using System.Text;
using System.DrawingCore;
using System.DrawingCore.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Runner
{
    class Invoice
    {
        public string Path { set; get; }
        public Image Bitmap { set; get; }
        public string ExtractedText { set; get; }
        public DateTime Date { set; get; }
        public List<string> CNs { set; get; }
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

        public void ExtractTextFromImages()
        {
            Console.WriteLine("Extracting text from images");
            foreach (var image in _invoices)
            {
                string extTxt = _c2t.GetText(image.Path);
                image.ExtractedText = extTxt;
                Console.WriteLine($"[{image.Path}] - '{extTxt}'");
            }
        }

        public void ExtractDate()
        {
            Console.WriteLine("Extract date");
            foreach (var image in _invoices)
            {
                Match match = Regex.Match(image.ExtractedText, @"(?:(?:31(\/|-|\.)(?:0?[13578]|1[02]))\1|(?:(?:29|30)(\/|-|\.)(?:0?[1,3-9]|1[0-2])\2))(?:(?:1[6-9]|[2-9]\d)?\d{2})(?=\W)|\b(?:29(\/|-|\.)0?2\3(?:(?:(?:1[6-9]|[2-9]\d)?(?:0[48]|[2468][048]|[13579][26])?|(?:(?:16|[2468][048]|[3579][26])00)?)))(?=\W)|\b(?:0?[1-9]|1\d|2[0-8])(\/|-|\.)(?:(?:0?[1-9])|(?:1[0-2]))(\4)?(?:(?:1[6-9]|[2-9]\d)?\d{2})?(?=\b)");

                if (match.Success)
                {
                    DateTime res;
                    DateTime.TryParse(match.Groups[0].Value, out res);
                    image.Date = res;
                }
            }
        }


        public void ExtractCNs()
        {
            Console.WriteLine("Extract CNs");
            foreach (var image in _invoices)
            {

                string res = string.Join(",",Regex.Split(image.ExtractedText, "^[0-9]{9}$"));
                Match match = Regex.Match(image.ExtractedText, @"^[0-9]{9}$");
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
