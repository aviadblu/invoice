using System;
using System.Threading;

namespace Runner
{
    class Program
    {
        static void Main(string[] args)
        {

            string invoicesFolder = @"Invoices";
            var runner = new Runner();
            //runner.LoadInvoices(invoicesFolder);
            runner._invoices.Add(new Invoice()
            {
                ExtractedText = @"'f 9 my] 0.79 ""N @ΓÇÿ b I Zlb 0X JUN 71113 FIJI fΓÇîiTlfΓÇîl' onn - ' 03-6844889 :0r1903-5610382 :|I9ΓÇÿ70 514680909 9.n 514680909 0""511 man 03/02/2019 : 1mm : TIJJΓÇÿI DWI! D""0]J'9l D'NJIZIPJFI D'IΓÇÿII'I'PJ 11.3 D'7Wl1' 513716043 .9.n 02571 3812:1""3 haimofcpa@gmail.com 1m ' 80137 7901] [17117 OI] D'JIJWH 3""no I mm I '1an EJ135720 1.00 03/03 1mm? 11} 03/021nnnn OI'IIJII'J wlnlw InΓÇ£! 01,160.00 DWDJ 1""ΓÇ£  [19197.20 17.00% rm] mm  I'IΓÇÿJ1,357.20 unn 7.7!} 3""I'ID :nl'mnn 19m  (NIS) 13572010 :Ian 0'01) 0 'mwmo'un unis (NIS) (36601896) 11ΓÇÿ} 1,357.20 DIDO I 1 D'DIWJD 1901'] | 7'11 'NWWN :wn |9|N I 9740 00131901] 0 D'NJ Dir/TIM? D1111 :DIW'I |IJJΓÇÿD * awnlnn 1110f] * http://ww.cardcom.co.il'"
            });
            //runner.ExtractTextFromImages();
            runner.ExtractDate();
            runner.ExtractCNs();

            while (true)
            {
                Thread.Sleep(2000);
            }
        }
    }
}
