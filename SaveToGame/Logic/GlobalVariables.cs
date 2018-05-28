using System;
using System.Reflection;
using Alphaleonis.Win32.Filesystem;

namespace SaveToGameWpf.Logic
{
    internal static class GlobalVariables
    {
        /// <summary>
        /// exe_folder/exe
        /// </summary>
        public static readonly string PathToExe;

        /// <summary>
        /// exe_folder
        /// </summary>
        public static readonly string PathToExeFolder;

        /// <summary>
        /// exe_folder/Resources
        /// </summary>
        public static readonly string PathToResources;

        /// <summary>
        /// exe_folder/Resources/jre
        /// </summary>
        public static readonly string PathToPortableJre;

        /// <summary>
        /// .../user/AppData/Local/SaveToGame
        /// </summary>
        public static readonly string AppSettingsDir;

        public static string AdditionalFilePassword;

        static GlobalVariables()
        {
#if DEBUG
            PathToExe = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.Parent.FullName + @"\Release\SaveToGame.exe";
#else
            PathToExe = Assembly.GetExecutingAssembly().Location;
#endif
            PathToExeFolder = Path.GetDirectoryName(PathToExe);
            PathToResources = Path.Combine(PathToExeFolder, "Resources");
            AppSettingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SaveToGame");

            var portableNearby = Path.Combine(PathToResources, "jre");

            PathToPortableJre = Directory.Exists(portableNearby) ? portableNearby : Path.Combine(AppSettingsDir, "jre");
        }
    }
}
