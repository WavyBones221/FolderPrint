using Microsoft.Extensions.Configuration;
using System.Linq;

namespace FolderPrint.Object
{
    public static class Configuration
    {
        private static readonly string ConfigurationFile = "application.properties.json";
        public static readonly string logPath = Path.Combine(Environment.CurrentDirectory, "FolderPrinterLogFile");
        public static ICollection<Task>? tasks { get; set; }
        public static string? sumatraPath { get; set; }
        public static bool debugMode { get; set; }

        public static void SetConfiguration()
        {
            ConfigurationBuilder builder = new();
            IConfiguration root = builder.AddJsonFile(ConfigurationFile).Build() ?? throw new Exception($"{ConfigurationFile} Not Found");
            tasks = root.GetSection("tasks").Get<List<Task>>()?.ToArray() ?? [];
            sumatraPath = root.GetValue<string>("sumatraPath")?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"SumatraPDF.exe");
            debugMode = root.GetValue<bool>("debugMode");
        }
    }
}
