using System.Reflection;
using Alphaleonis.Win32.Filesystem;

#if !DEBUG
using System.Reflection;
#endif

namespace SaveToGameWpf.Logic
{
    internal static class GlobalVariables
    {
        /// <summary>
        /// exe_folder/exe
        /// </summary>
        public static string PathToExe;

        /// <summary>
        /// exe_folder
        /// </summary>
        public static string PathToExeFolder;

        /// <summary>
        /// exe_folder/Resources
        /// </summary>
        public static string PathToResources;

        /// <summary>
        /// exe_folder/Resources/jre
        /// </summary>
        public static string PathToPortableJre;

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
            PathToPortableJre = Path.Combine(PathToResources, "jre");
        }
    }
}
