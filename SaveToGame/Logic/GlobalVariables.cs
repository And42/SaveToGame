using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using Bugsnag;
using SaveToGameWpf.Logic.Utils;

namespace SaveToGameWpf.Logic
{
    internal static class GlobalVariables
    {
        /// <summary>
        /// ../AppData/Local/SaveToGame
        /// </summary>
        public static readonly string AppDataPath;

        /// <summary>
        /// ../AppData/Local/SaveToGame/temp
        /// </summary>
        public static readonly string TempPath;

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
        /// exe_folder/Resources/apktool.jar
        /// </summary>
        public static readonly string ApktoolPath;

        /// <summary>
        /// exe_folder/Resources/baksmali.jar
        /// </summary>
        public static readonly string BaksmaliPath;

        /// <summary>
        /// exe_folder/Resources/smali.jar
        /// </summary>
        public static readonly string SmaliPath;

        /// <summary>
        /// exe_folder/Resources/signapk.jar
        /// </summary>
        public static readonly string SignApkPath;

        /// <summary>
        /// exe_folder/Resources/testkey.x509.pem
        /// </summary>
        public static readonly string DefaultKeyPemPath;

        /// <summary>
        /// exe_folder/Resources/testkey.pk8
        /// </summary>
        public static readonly string DefaultKeyPkPath;

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
            AppDataPath =
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    Assembly.GetExecutingAssembly().GetName().Name
                );
            TempPath = Path.Combine(AppDataPath, "temp");

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

            ApktoolPath = Path.Combine(PathToResources, "apktool.jar");
            BaksmaliPath = Path.Combine(PathToResources, "baksmali.jar");
            SmaliPath = Path.Combine(PathToResources, "smali.jar");
            SignApkPath = Path.Combine(PathToResources, "signapk.jar");
            DefaultKeyPemPath = Path.Combine(PathToResources, "testkey.x509.pem");
            DefaultKeyPkPath = Path.Combine(PathToResources, "testkey.pk8");

            IOUtils.CreateDir(AppDataPath);
        }
    }
}
