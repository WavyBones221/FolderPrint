using Microsoft.Extensions.Configuration;

namespace FolderPrint.Object
{
    public static class Configuration
    {
        private static readonly string ConfigurationFile = "application.properties.json";
        public static readonly string logPath = Path.Combine(Environment.CurrentDirectory, "FolderPrinterLogFile.txt");
        public static string watchFolder { get; set; }
        public static string printerName { get; set; }
        public static string sumatraPath { get; set; }
        public static string completedFolder { get; set; }
        public static bool debugMode { get; set; }

        public static void SetConfiguration()
        {
            ConfigurationBuilder builder = new();
            IConfiguration root = builder.AddJsonFile(ConfigurationFile).Build() ?? throw new Exception($"{ConfigurationFile} Not Found");
            watchFolder = root.GetValue<string>("watchFolder");
            printerName = root.GetValue<string>("printerName");
            sumatraPath = root.GetValue<string>("sumatraPath")?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"SumatraPDF.exe");
            completedFolder = root.GetValue<string>("completedFolder");
            debugMode = root.GetValue<bool>("debugMode");

            File.AppendAllText(Path.Combine(Environment.CurrentDirectory, "logfile.txt"), $"{watchFolder},{printerName},{sumatraPath},{completedFolder}{Environment.NewLine}");
        }
    }
}
