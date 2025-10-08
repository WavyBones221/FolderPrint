
using FolderPrint.Object;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Text;

Configuration.SetConfiguration();
string? defaultPrinter = GetDefaultPrinterName();

if (!Directory.Exists(Configuration.watchFolder))
{
    File.AppendAllText(Configuration.logPath, $"Folder does not exist: {Configuration.watchFolder}");
    return;
}

FileSystemWatcher watcher = new()
{
    Path = Configuration.watchFolder,
    Filter = "*.pdf",
    NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime
};

watcher.Created += OnNewPdf;
watcher.EnableRaisingEvents = true;

while (true)
{
   Thread.Sleep(1000);
}

static void OnNewPdf(object sender, FileSystemEventArgs e)
{

    while (!IsFileReady(e.FullPath))
    {
        Thread.Sleep(500);
    }

    string? defaultPrinter = GetDefaultPrinterName();
    if (Configuration.debugMode)
    {
        File.AppendAllText(Configuration.logPath, $"DEBUGMODE @ [{DateTime.Now:F}] Defualt Printer Name => \"{defaultPrinter}\"{Environment.NewLine}");

        foreach (string printer in PrinterSettings.InstalledPrinters)
        {
            File.AppendAllText(Configuration.logPath, $"DEBUGMODE @ [{DateTime.Now:F}] Found Printer Named => \"{printer}\"{Environment.NewLine}");
        }
        //no need for this to constantly spill out into a file each time
        Configuration.debugMode = false;
    }
    try
    {
        ProcessStartInfo psi = new()
        {
            FileName = Configuration.sumatraPath,
            Arguments = $"-print-to \"{Configuration.printerName ?? defaultPrinter}\" \"{e.FullPath}\"",
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        Process.Start(psi);
        try
        {
            File.Move($"{Path.Combine(Configuration.watchFolder, Path.GetFileName(e.FullPath))}", $"{Path.Combine(Configuration.completedFolder, Path.GetFileName(e.FullPath))}");
        }
        catch
        {
            File.AppendAllText(Configuration.logPath, $"ERROR @ [{DateTime.Now:F}] Failed to Move: {Path.Combine(Configuration.watchFolder, Path.GetFileName(e.FullPath))}{Environment.NewLine}");
        }
    }
    catch (Exception ex)
    {
        File.AppendAllText(Configuration.logPath, $"ERROR @ [{DateTime.Now:F}] Failed to print: {ex.Message}{Environment.NewLine}");
    }
}
[DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
static extern bool GetDefaultPrinter(StringBuilder pszBuffer, ref int pcchBuffer);

static string? GetDefaultPrinterName()
{
    int pcchBuffer = 256;
    StringBuilder buffer = new(pcchBuffer);
    if (GetDefaultPrinter(buffer, ref pcchBuffer))
    {
        return buffer.ToString();
    }
    else
    {
        return null;
    }
}
static bool IsFileReady(string path)
{
    try
    {
        using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            return true;
        }
    }
    catch
    {
        return false;
    }
}
