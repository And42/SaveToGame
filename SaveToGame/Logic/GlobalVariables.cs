using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Bugsnag;
using JetBrains.Annotations;

namespace SaveToGameWpf.Logic
{
    public class GlobalVariables
    {
        public const int LatestSettingsVersion = 1;

        /// <summary>
        /// ../AppData/Roaming/SaveToGame or exe_folder/data
        /// </summary>
        [NotNull]
        public readonly string AppDataPath;

        /// <summary>
        /// ../AppData/Roaming/SaveToGame/temp
        /// </summary>
        [NotNull]
        public readonly string TempPath;

        /// <summary>
        /// exe_folder/exe
        /// </summary>
        [NotNull]
        public readonly string PathToExe;

        /// <summary>
        /// exe_folder
        /// </summary>
        [NotNull]
        public readonly string PathToExeFolder;

        /// <summary>
        /// exe_folder/Resources
        /// </summary>
        [NotNull]
        public readonly string PathToResources;

        /// <summary>
        /// exe_folder/Resources/apktool.jar
        /// </summary>
        [NotNull]
        public readonly string ApktoolPath;

        /// <summary>
        /// exe_folder/Resources/baksmali.jar
        /// </summary>
        [NotNull]
        public readonly string BaksmaliPath;

        /// <summary>
        /// exe_folder/Resources/smali.jar
        /// </summary>
        [NotNull]
        public readonly string SmaliPath;

        /// <summary>
        /// exe_folder/Resources/signapk.jar
        /// </summary>
        [NotNull]
        public readonly string SignApkPath;

        /// <summary>
        /// exe_folder/Resources/testkey.x509.pem
        /// </summary>
        [NotNull]
        public readonly string DefaultKeyPemPath;

        /// <summary>
        /// exe_folder/Resources/testkey.pk8
        /// </summary>]
        [NotNull]
        public readonly string DefaultKeyPkPath;

        /// <summary>
        /// ../AppData/Roaming/SaveToGame/jre
        /// </summary>
        [NotNull]
        public readonly string PathToPortableJre;

        /// <summary>
        /// exe_folder/Resources/jre/bin/java.exe
        /// </summary>
        [NotNull]
        public readonly string PathToPortableJavaExe;

        /// <summary>
        /// exe_folder/Resources/platform-tools/adb.exe
        /// </summary>
        [NotNull]
        public readonly string AdbPath;

        [NotNull]
        public readonly IClient ErrorClient = new Client(apiKey: "1065fd5bfd52ab837da209f8354b79cb");

        [NotNull]
        public readonly string AdditionalFilePassword = "Ub82X8:Hng6t=C+'mx";

        public readonly bool IsPortable;

        /// <summary>
        /// Is set only if <see cref="IsPortable"/> is true
        /// </summary>
        public readonly bool? CanWriteToAppData;

        /// <summary>
        /// exe_folder/portable
        /// </summary>
        [NotNull]
        public readonly string PortableSwitchFile;

        [CanBeNull]
        public string LatestModdedApkPath { get; set; }
        
        public GlobalVariables()
        {
            PathToExe = Process.GetCurrentProcess().MainModule.FileName;
            PathToExeFolder = Path.GetDirectoryName(PathToExe) ?? "";
            PortableSwitchFile = Path.Combine(PathToExeFolder, "portable");
            IsPortable = File.Exists(PortableSwitchFile);

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
                    Directory.CreateDirectory(AppDataPath);
                    string tempFile = Path.Combine(AppDataPath, "write_test");
                    using (File.Create(tempFile)) {}
                    File.Delete(tempFile);
                    CanWriteToAppData = true;
                }
                catch (UnauthorizedAccessException)
                {
                    CanWriteToAppData = false;
                }
            }
            else
            {
                Directory.CreateDirectory(AppDataPath);
            }
        }
    }
}
