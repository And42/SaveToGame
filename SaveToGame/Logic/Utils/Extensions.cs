using System;
using System.ComponentModel;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using JetBrains.Annotations;
using LongPaths.Logic;
using MVVM_Tools.Code.Commands;

namespace SaveToGameWpf.Logic.Utils
{
    public static class Extensions
    {
        public static T As<T>(this object obj) => (T) obj;

        public static void ExtractAll(this ZipFile zip, string folder)
        {
            LDirectory.Delete(folder, true);
            LDirectory.CreateDirectory(folder);

            foreach (ZipEntry entry in zip)
            {
                if (entry.IsDirectory)
                    continue;

                LDirectory.CreateDirectory(Path.Combine(folder, Path.GetDirectoryName(entry.Name) ?? String.Empty));

                using (var zipStream = zip.GetInputStream(entry))
                using (var outputStream = LFile.Create(Path.Combine(folder, entry.Name)))
                {
                    zipStream.CopyTo(outputStream, 4096);
                }
            }
        }

        public static IActionCommand BindCanExecute([NotNull] this IActionCommand actionCommand, [NotNull] INotifyPropertyChanged observable)
        {
            observable.PropertyChanged += (sender, args) => actionCommand.RaiseCanExecuteChanged();
            return actionCommand;
        }

        public static IActionCommand<T> BindCanExecute<T>([NotNull] this IActionCommand<T> actionCommand, [NotNull] INotifyPropertyChanged observable)
        {
            observable.PropertyChanged += (sender, args) => actionCommand.RaiseCanExecuteChanged();
            return actionCommand;
        }
    }
}
