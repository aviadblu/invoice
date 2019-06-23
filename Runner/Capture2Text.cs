using System;
using System.Diagnostics;
using System.DrawingCore;
using System.DrawingCore.Imaging;
using System.IO;

namespace Runner
{
    /// <summary>
    /// Provides .NET access to the Capture2Text .exe (by their license, we can only use the .exe with no modifications allowed)
    /// </summary>
    public class Capture2Text
    {
        private static readonly string Capture2TextExecutablePath = @"\Capture2Text\Capture2Text_v4.6.0_64bit\Capture2Text_CLI.exe";
        private static readonly ImageFormat Format = ImageFormat.Png;

        private readonly string executable;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="executable">path the the capture2text executable cli</param>
        public Capture2Text(string executable)
        {
            this.executable = Environment.CurrentDirectory + executable;
        }

        /// <inheritdoc />
        /// <summary>
        /// Constrcutor with default path to executable
        /// </summary>
        public Capture2Text() : this(Capture2TextExecutablePath) { }

        public string GetText(string imagePath)
        {
            string output = "";
            if (imagePath == null)
                return string.Empty;

            if (!File.Exists(imagePath))
                return string.Empty;

            var processInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                RedirectStandardInput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Minimized,
                FileName = executable,
                Arguments = $"-i {imagePath}"
            };

            // starts the exe
            Process capture2Text = new Process();
            capture2Text = Process.Start(processInfo);
            capture2Text.OutputDataReceived += (sender, args) =>
            {
                if (!String.IsNullOrEmpty(args.Data))
                {
                    output = args.Data;
                }
            };

            capture2Text.BeginOutputReadLine();

            capture2Text.WaitForExit();

            return (output);
        }

        /// <summary>
        /// Extracts the text from an <see cref="Image"/>
        /// </summary>
        /// <param name="image">image</param>
        /// <returns>text found in image</returns>        
        public string GetText(Image image)
        {
            using (var bitmap = new Bitmap(image))
            {
                string temporaryFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.{Format.ToString().ToLower()}");
                bitmap.Save(temporaryFile, Format);

                try
                {
                    return GetText(temporaryFile);
                }
                finally
                {
                    File.Delete(temporaryFile);
                }
            }
        }
    }
}
