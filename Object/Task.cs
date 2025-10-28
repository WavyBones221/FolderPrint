using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolderPrint.Object
{
    public class Task
    {
        public string watchFolder { get; set; }
        public string? completedFolder { get; set; }
        public string? printerName { get; set; }
        public string? orientation { get; set; } = "portrait";
    }
}
