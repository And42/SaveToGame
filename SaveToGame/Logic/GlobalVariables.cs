using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using Bugsnag;
using LongPaths.Logic;

namespace SaveToGameWpf.Logic
{
    public class GlobalVariables
    {
        /// <summary>
        /// ../AppData/Roaming/SaveToGame
        /// </summary>
        public readonly string AppDataPath;

        /// <summary>
        /// ../AppData/Roaming/SaveToGame/temp
        /// </summary>
        public readonly string TempPath;

        /// <summary>
        /// exe_folder/exe
        /// </summary>
        public readonly string PathToExe;

        /// <summary>
        /// exe_folder
        /// </summary>
        public readonly string PathToExeFolder;

        /// <summary>
        /// exe_folder/Resources
        /// </summary>
        public readonly string PathToResources;

        /// <summary>
        /// exe_folder/Resources/apktool.jar
        /// </summary>
        public readonly string ApktoolPath;

        /// <summary>
        /// exe_folder/Resources/baksmali.jar
        /// </summary>
        public readonly string BaksmaliPath;

        /// <summary>
        /// exe_folder/Resources/smali.jar
        /// </summary>
        public readonly string SmaliPath;

        /// <summary>
        /// exe_folder/Resources/signapk.jar
        /// </summary>
        public readonly string SignApkPath;

        /// <summary>
        /// exe_folder/Resources/testkey.x509.pem
        /// </summary>
        public readonly string DefaultKeyPemPath;

        /// <summary>
        /// exe_folder/Resources/testkey.pk8
        /// </summary>
        public readonly string DefaultKeyPkPath;

        public readonly string PathToPortableJre;

        /// <summary>
        /// exe_folder/Resources/jre/bin/java.exe
        /// </summary>
        public readonly string PathToPortableJavaExe;

        public readonly IClient ErrorClient = new Client(ConfigurationManager.AppSettings["BugsnagApiKey"]);

        public readonly string AdditionalFilePassword = "Ub82X8:Hng6t=C+'mx";
        
        public GlobalVariables()
        {
            AppDataPath =
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Assembly.GetExecutingAssembly().GetName().Name
                );
            TempPath = Path.Combine(AppDataPath, "temp");

#if DEBUG
            // ReSharper disable once PossibleNullReferenceException
            PathToExe = Path.Combine(
                Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)),
                "Release",
                "SaveToGame.exe"
            );
#else
            PathToExe = Assembly.GetExecutingAssembly().Location;
#endif
            PathToExeFolder = Path.GetDirectoryName(PathToExe);
            // ReSharper disable once AssignNullToNotNullAttribute
            PathToResources = Path.Combine(PathToExeFolder, "Resources");

            var portableNearby = Path.Combine(PathToResources, "jre");

            PathToPortableJre = LDirectory.Exists(portableNearby) ? portableNearby : Path.Combine(AppDataPath, "jre");
            PathToPortableJavaExe = Path.Combine(PathToPortableJre, "bin", "java.exe");

            ApktoolPath = Path.Combine(PathToResources, "apktool.jar");
            BaksmaliPath = Path.Combine(PathToResources, "baksmali.jar");
            SmaliPath = Path.Combine(PathToResources, "smali.jar");
            SignApkPath = Path.Combine(PathToResources, "signapk.jar");
            DefaultKeyPemPath = Path.Combine(PathToResources, "testkey.x509.pem");
            DefaultKeyPkPath = Path.Combine(PathToResources, "testkey.pk8");

            LDirectory.CreateDirectory(AppDataPath);
        }
    }
}
