using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using Bugsnag;

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
        /// exe_folder/Resources/jre/bin/java.exe
        /// </summary>
        public static readonly string PathToPortableJavaExe;

        /// <summary>
        /// .../user/AppData/Local/SaveToGame
        /// </summary>
        public static readonly string AppSettingsDir;

        public static readonly IClient ErrorClient = new Client(ConfigurationManager.AppSettings["BugsnagApiKey"]);

        public static string AdditionalFilePassword;
        
        static GlobalVariables()
        {
#if DEBUG
            // ReSharper disable once PossibleNullReferenceException
            PathToExe = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.Parent.FullName + @"\Release\SaveToGame.exe";
#else
            PathToExe = Assembly.GetExecutingAssembly().Location;
#endif
            PathToExeFolder = Path.GetDirectoryName(PathToExe);
            // ReSharper disable once AssignNullToNotNullAttribute
            PathToResources = Path.Combine(PathToExeFolder, "Resources");
            AppSettingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SaveToGame");

            var portableNearby = Path.Combine(PathToResources, "jre");

            PathToPortableJre = Directory.Exists(portableNearby) ? portableNearby : Path.Combine(AppSettingsDir, "jre");
            PathToPortableJavaExe = Path.Combine(PathToPortableJre, "bin", "java.exe");
        }
    }
}
