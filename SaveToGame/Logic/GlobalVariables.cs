using System;
using System.IO;
using System.Reflection;
using Bugsnag;

namespace SaveToGameWpf.Logic
{
    public class GlobalVariables
    {
        public const int LatestSettingsVersion = 1;

        /// <summary>
        /// ../AppData/Roaming/SaveToGame or exe_folder/data
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
        /// exe_folder/Resources/apksigner.jar
        /// </summary>
        public readonly string ApkSignerPath;
        
        /// <summary>
        /// exe_folder/Resources/zipalign.exe
        /// </summary>
        public readonly string ZipalignPath;
        
        /// <summary>
        /// exe_folder/Resources/aapt2.exe
        /// </summary>
        public readonly string Aapt2Path;

        /// <summary>
        /// exe_folder/Resources/testkey.x509.pem
        /// </summary>
        public readonly string DefaultKeyPemPath;

        /// <summary>
        /// exe_folder/Resources/testkey.pk8
        /// </summary>]
        public readonly string DefaultKeyPkPath;

        /// <summary>
        /// ../AppData/Roaming/SaveToGame/jre
        /// </summary>
        public readonly string PathToPortableJre;

        /// <summary>
        /// exe_folder/Resources/jre/bin/java.exe
        /// </summary>
        public readonly string PathToPortableJavaExe;

        /// <summary>
        /// exe_folder/Resources/platform-tools/adb.exe
        /// </summary>
        public readonly string AdbPath;

        public readonly IClient ErrorClient = new Client(apiKey: "1065fd5bfd52ab837da209f8354b79cb");

        public readonly string AdditionalFilePassword = "Ub82X8:Hng6t=C+'mx";

        public readonly bool IsPortable;

        /// <summary>
        /// Is set only if <see cref="IsPortable"/> is true
        /// </summary>
        public readonly bool? CanWriteToAppData;

        /// <summary>
        /// exe_folder/portable
        /// </summary>
        public readonly string PortableSwitchFile;

        public string? LatestModdedApkPath { get; set; }
        
        public GlobalVariables()
        {
            PathToExe = Assembly.GetExecutingAssembly().Location;
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
            ApkSignerPath = Path.Combine(PathToResources, "apksigner.jar");
            ZipalignPath = Path.Combine(PathToResources, "zipalign.exe");
            Aapt2Path = Path.Combine(PathToResources, "aapt2.exe");
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
