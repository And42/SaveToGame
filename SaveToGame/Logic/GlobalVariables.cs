using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using Bugsnag;
using JetBrains.Annotations;
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

        public readonly string AdbPath;

        public readonly IClient ErrorClient = new Client(ConfigurationManager.AppSettings["BugsnagApiKey"]);

        public readonly string AdditionalFilePassword = "Ub82X8:Hng6t=C+'mx";

        public readonly bool IsPortable;

        /// <summary>
        /// Is set only if <see cref="IsPortable"/> is true
        /// </summary>
        public readonly bool? CanWriteToAppData;

        public readonly string PortableSwitchFile;

        [CanBeNull]
        public string LatestModdedApkPath { get; set; }
        
        public GlobalVariables()
        {
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
            PortableSwitchFile = Path.Combine(PathToExeFolder, "portable");
            IsPortable = LFile.Exists(PortableSwitchFile);

            AppDataPath =
                IsPortable
                    ? Path.Combine(PathToExeFolder, "data")
                    : Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        Assembly.GetExecutingAssembly().GetName().Name
                    );
            TempPath = Path.Combine(AppDataPath, "temp");

            PathToResources = Path.Combine(PathToExeFolder, "Resources");

            PathToPortableJre = Path.Combine(AppDataPath, "jre");
            PathToPortableJavaExe = Path.Combine(PathToPortableJre, "bin", "java.exe");

            ApktoolPath = Path.Combine(PathToResources, "apktool.jar");
            BaksmaliPath = Path.Combine(PathToResources, "baksmali.jar");
            SmaliPath = Path.Combine(PathToResources, "smali.jar");
            SignApkPath = Path.Combine(PathToResources, "signapk.jar");
            DefaultKeyPemPath = Path.Combine(PathToResources, "testkey.x509.pem");
            DefaultKeyPkPath = Path.Combine(PathToResources, "testkey.pk8");
            AdbPath = Path.Combine(PathToResources, "platform-tools", "adb.exe");

            if (IsPortable)
            {
                try
                {
                    LDirectory.CreateDirectory(AppDataPath);
                    string tempFile = Path.Combine(AppDataPath, "write_test");
                    using (LFile.Create(tempFile))
                    { }
                    LFile.Delete(tempFile);
                    CanWriteToAppData = true;
                }
                catch (UnauthorizedAccessException)
                {
                    CanWriteToAppData = false;
                }
            }
            else
            {
                LDirectory.CreateDirectory(AppDataPath);
            }
        }
    }
}
