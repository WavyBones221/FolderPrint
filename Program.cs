using FolderPrint.Object;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Text;

Configuration.SetConfiguration();
string? defaultPrinter = GetDefaultPrinterName();

foreach (FolderPrint.Object.Task task in Configuration.tasks)
{
    if (!Directory.Exists(task.watchFolder))
    {
        try
        {
            File.AppendAllText(Configuration.logPath, $"Folder does not exist: {task.watchFolder}{Environment.NewLine}");
            continue;
        }
        catch { continue; }//@TODO add log lock so that threads arent fighting over logfile. or can add log file per thread?? which would be a lazy way of doing it i guess
    }

    if (!string.IsNullOrWhiteSpace(task.printerName) &&
        !string.IsNullOrWhiteSpace(task.watchFolder))
    {
        FileSystemWatcher watcher = new()
        {
            Path = task.watchFolder,
            Filter = "*.pdf",
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };
        watcher.Created += (sender, e) =>
            System.Threading.Tasks.Task.Run(() => OnNewPdf(sender, e, task.printerName, task.completedFolder, task.watchFolder));
    }
}

while (true)
{
    Thread.Sleep(1000);
}

static void OnNewPdf(object sender, FileSystemEventArgs e, string? printerName, string? completedFolder, string watchFolder)
{
    while (!IsFileReady(e.FullPath))
    {
        Thread.Sleep(500);
    }

    string? defaultPrinter = GetDefaultPrinterName();

    if (Configuration.debugMode)
    {
        try
        {
            File.AppendAllText(Configuration.logPath, $"DEBUGMODE @ [{DateTime.Now:F}] Default Printer Name => \"{defaultPrinter}\"{Environment.NewLine}");
        }
        catch { }

        foreach (string printer in PrinterSettings.InstalledPrinters)
        {
            try
            {
                File.AppendAllText(Configuration.logPath, $"DEBUGMODE @ [{DateTime.Now:F}] Found Printer Named => \"{printer}\"{Environment.NewLine}");
            }
            catch { }
        }

        Configuration.debugMode = false;
    }

    try
    {
        ProcessStartInfo psi = new()
        {
            FileName = Configuration.sumatraPath,
            Arguments = $"-print-to \"{printerName ?? defaultPrinter}\" \"{e.FullPath}\"",
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        Process.Start(psi);

        string fileName = Path.GetFileName(e.FullPath);
        if (string.IsNullOrWhiteSpace(completedFolder))
        {
            try
            {
                File.Delete(e.FullPath);
            }
            catch
            {
                try
                {
                    File.AppendAllText(Configuration.logPath, $"ERROR @ [{DateTime.Now:F}] Failed to Delete: {e.FullPath}{Environment.NewLine}");
                }
                catch { }
            }
        }
        else
        {
            string destinationPath = Path.Combine(completedFolder, fileName);

            try
            {
                File.Move(e.FullPath, destinationPath);
            }
            catch
            {
                try
                {
                    File.AppendAllText(Configuration.logPath, $"ERROR @ [{DateTime.Now:F}] Failed to Move: {e.FullPath}{Environment.NewLine}");
                }
                catch { }
            }
        }
    }
    catch (Exception ex)
    {
        try
        {
            File.AppendAllText(Configuration.logPath, $"ERROR @ [{DateTime.Now:F}] Failed to print \"{e.FullPath}\" to \"{printerName ?? defaultPrinter}\": {ex.Message}{Environment.NewLine}");
        }
        catch { }
    }
}

[DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
static extern bool GetDefaultPrinter(StringBuilder pszBuffer, ref int pcchBuffer);

static string? GetDefaultPrinterName()
{
    int pcchBuffer = 256;
    StringBuilder buffer = new(pcchBuffer);
    return GetDefaultPrinter(buffer, ref pcchBuffer) ? buffer.ToString() : null;
}

static bool IsFileReady(string path)
{
    try
    {
        using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
        return true;
    }
    catch
    {
        return false;
    }
}