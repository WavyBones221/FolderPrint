using FolderPrint.Object;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Text;

Configuration.SetConfiguration();
string? defaultPrinter = GetDefaultPrinterName();
List<FileSystemWatcher> watchers = new();
Log($"Process Start");

foreach (FolderPrint.Object.Task task in Configuration.tasks!)
{
    Log($"Task Start {task.watchFolder}");
    if (!Directory.Exists(task.watchFolder))
    {
        Log($"Folder does not exist: {task.watchFolder}");
        continue;
    }
    if (string.IsNullOrWhiteSpace(task.printerName))
    {
        task.printerName = defaultPrinter;
    }

    if (!string.IsNullOrWhiteSpace(task.printerName) && !string.IsNullOrWhiteSpace(task.watchFolder))
    {
        Log($"Setting up watcher for folder: {task.watchFolder}");

        FileSystemWatcher watcher = new()
        {
            Path = task.watchFolder,
            Filter = "*.pdf",
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };

        watcher.Created += (sender, e) =>
        {
            Log($"File created event triggered for: {e.FullPath}");
            System.Threading.Tasks.Task.Run(() => OnNewPdf(sender, e, task.printerName, task.completedFolder, task.watchFolder, task.orientation));
        };

        watcher.Changed += (sender, e) =>
        {
            Log($"File changed event triggered for: {e.FullPath}");
            System.Threading.Tasks.Task.Run(() => OnNewPdf(sender, e, task.printerName, task.completedFolder, task.watchFolder, task.orientation));
        };

        watcher.Renamed += (sender, e) =>
        {
            Log($"File renamed event triggered for: {e.FullPath}");
            System.Threading.Tasks.Task.Run(() => OnNewPdf(sender, e, task.printerName, task.completedFolder, task.watchFolder, task.orientation));
        };
        watchers.Add(watcher);
    }
}

while (true)
{
    Thread.Sleep(1000);
}


static void OnNewPdf(object sender, FileSystemEventArgs e, string? printerName, string? completedFolder, string watchFolder, string? orientation = "portrait")
{
    const int maxRetries = 20;
    int retries = 0;

    while (!IsFileReady(e.FullPath) && retries < maxRetries)
    {
        Thread.Sleep(500);
        retries++;
    }

    if (retries == maxRetries)
    {
        Log($"ERROR: File never became ready: {e.FullPath}");
        return;
    }

    string? defaultPrinter = GetDefaultPrinterName();

    if (Configuration.debugMode)
    {
        Log($"DEBUGMODE @ [{DateTime.Now:F}] Default Printer Name => \"{defaultPrinter}\"");

        foreach (string printer in PrinterSettings.InstalledPrinters)
        {
            Log($"DEBUGMODE @ [{DateTime.Now:F}] Found Printer Named => \"{printer}\"");
        }

        Configuration.debugMode = false;
    }

    if (!PrinterSettings.InstalledPrinters.Cast<string>().Contains(printerName ?? defaultPrinter))
    {
        Log($"ERROR: Printer \"{printerName ?? defaultPrinter}\" not found.");
        return;
    }

    if (!File.Exists(Configuration.sumatraPath))
    {
        Log($"ERROR: SumatraPDF not found at path: {Configuration.sumatraPath}");
        return;
    }

    Log($"Attempting to print file: {e.FullPath}");

    try
    {
        ProcessStartInfo psi = new()
        {
            FileName = Configuration.sumatraPath,
            Arguments = $"-print-to \"{printerName ?? defaultPrinter}\" -print-settings \"{orientation},fit\" \"{e.FullPath}\"",
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using Process process = Process.Start(psi)!;
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Log($"SumatraPDF Output: {output}");
        Log($"SumatraPDF Error: {error}");
        Log($"SumatraPDF Exit Code: {process.ExitCode}");

        string fileName = Path.GetFileName(e.FullPath);
        if (string.IsNullOrWhiteSpace(completedFolder))
        {
            if (!TryDeleteFile(e.FullPath))
                Log($"ERROR: Failed to delete file after retries: {e.FullPath}");
            else
                Log($"Deleted file: {e.FullPath}");
        }
        else
        {
            string destinationPath = Path.Combine(completedFolder, fileName);
            if (!TryMoveFile(e.FullPath, destinationPath))
                Log($"ERROR: Failed to move file after retries: {e.FullPath} to {destinationPath}");
            else
                Log($"Moved file to: {destinationPath}");
        }
    }
    catch (Exception ex)
    {
        Log($"ERROR: Failed to print \"{e.FullPath}\" to \"{printerName ?? defaultPrinter}\": {ex.Message}");
    }
}

static bool TryMoveFile(string source, string destination, int maxRetries = 5)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            File.Move(source, destination);
            return true;
        }
        catch
        {
            Thread.Sleep(500);
        }
    }
    return false;
}

static bool TryDeleteFile(string path, int maxRetries = 5)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            File.Delete(path);
            return true;
        }
        catch
        {
            Thread.Sleep(500);
        }
    }
    return false;
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

static void Log(string message)
{
    string logFile = $"{Configuration.logPath}{Environment.CurrentManagedThreadId}.txt";
    try
    {
        lock (Configuration.logLock)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logFile)!);
            File.AppendAllText(logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}");
        }
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Logging failed: {ex.Message}");
    }
}