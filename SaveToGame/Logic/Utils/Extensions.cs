﻿using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using JetBrains.Annotations;
using MVVM_Tools.Code.Commands;
using MVVM_Tools.Code.Providers;

namespace SaveToGameWpf.Logic.Utils
{
    public static class Extensions
    {
        public static T As<T>(this object obj) => (T) obj;

        public static void ExtractAll(this ZipFile zip, string folder)
        {
            Directory.Delete(folder, true);
            Directory.CreateDirectory(folder);

            foreach (ZipEntry entry in zip)
            {
                if (entry.IsDirectory)
                    continue;

                Directory.CreateDirectory(Path.Combine(folder, Path.GetDirectoryName(entry.Name) ?? String.Empty));

                using (var zipStream = zip.GetInputStream(entry))
                using (var outputStream = File.Create(Path.Combine(folder, entry.Name)))
                {
                    zipStream.CopyTo(outputStream, 4096);
                }
            }
        }

        public static IActionCommand BindCanExecute<TProp>([NotNull] this IActionCommand actionCommand, [NotNull] IReadonlyProperty<TProp> observable)
        {
            observable.PropertyChanged += (sender, args) => actionCommand.RaiseCanExecuteChanged();
            return actionCommand;
        }

        public static IActionCommand<T> BindCanExecute<T, TProp>([NotNull] this IActionCommand<T> actionCommand, [NotNull] IReadonlyProperty<TProp> observable)
        {
            observable.PropertyChanged += (sender, args) => actionCommand.RaiseCanExecuteChanged();
            return actionCommand;
        }
    }
}
