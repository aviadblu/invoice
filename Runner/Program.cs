using System;
using System.Threading;
using System.Threading.Tasks;

namespace Runner
{
    class Program
    {
        private static string _invoicesFolder = @"Invoices";
        static void Main(string[] args)
        {
            Task.Factory.StartNew(async () =>
            {
                await ProcessInvoices();
            });

            while (true)
            {
                Thread.Sleep(2000);
            }
        }

        private static async Task ProcessInvoices()
        {
            
            var runner = new Runner();

            runner.LoadInvoices(_invoicesFolder);

            await runner.ExtractRectsFromImages();
            runner.CreateOutputFiles(@"c:\\temp\\invoiceTest");
            Console.WriteLine("ExtractRectsFromImages done!");
            //Console.WriteLine(runner.GetInvoicesAsJson());
        }
    }
}
