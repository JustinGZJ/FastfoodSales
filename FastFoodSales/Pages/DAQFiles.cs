using System.IO;

namespace DAQ
{
    public static class DAQFiles
    {
        /// <summary>
        /// %AppData%\Accelerider\daq.json
        /// </summary>
        public static readonly string Configure = Path.Combine(AppFolders.AppData, "DAQ.json");
    }

}
