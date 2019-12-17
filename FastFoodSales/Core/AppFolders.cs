using System;
using System.IO;

namespace DAQ
{
    public static class AppFolders
    {
        static AppFolders()
        {
            Directory.CreateDirectory(Apps);
            Directory.CreateDirectory(Logs);
            Directory.CreateDirectory(Users);
        }

        /// <summary>
        /// It represents the path where the "Accelerider.Windows.exe" is located.
        /// </summary>
        public static readonly string MainProgram = AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// %AppData%\DAQ
        /// </summary>
        public static readonly string AppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DAQ");

        /// <summary>
        /// %AppData%\DAQ\Apps
        /// </summary>
        public static readonly string Apps = Path.Combine(AppData, nameof(Apps));

        /// <summary>
        /// %AppData%\DAQ\Logs
        /// </summary>
        public static readonly string Logs = Path.Combine(AppData, nameof(Logs));

        /// <summary>
        /// %AppData%\DAQ\Users
        /// </summary>
        public static readonly string Users = Path.Combine(AppData, nameof(Users));
    }
}
