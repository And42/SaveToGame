using System;
using System.Linq;
using System.Windows;
using JetBrains.Annotations;

namespace SaveToGameWpf.Logic.Utils
{
    public static class DragDropUtils
    {
        private static readonly string[] EmptyStrings = new string[0];

        [NotNull]
        public static string[] GetFilesDrop(this DragEventArgs args)
        {
            return (string[])args.Data.GetData(DataFormats.FileDrop) ?? EmptyStrings;
        }

        [NotNull]
        public static string[] GetFilesDrop(this DragEventArgs args, string ending)
        {
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (string.IsNullOrEmpty(ending))
                return args.GetFilesDrop();

            return args.GetFilesDrop(it => it.EndsWith(ending, StringComparison.Ordinal));
        }

        [NotNull]
        public static string[] GetFilesDrop(this DragEventArgs args, Func<string, bool> filter)
        {
            var items = args.GetFilesDrop();

            if (items == null)
                return EmptyStrings;

            return filter == null ? items : items.Where(filter).ToArray();
        }

        public static void CheckDragOver(this DragEventArgs e, params string[] extensions)
        {
            string[] files = e.GetFilesDrop();

            if (files.Length == 1 && extensions.Any(ext => files[0].EndsWith(ext, StringComparison.Ordinal)))
                e.Effects = DragDropEffects.Move;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        public static void DropOneByEnd(this DragEventArgs e, string ext, Action<string> onSuccess)
        {
            string[] files = e.GetFilesDrop(ext);

            if (files.Length == 1)
                onSuccess(files[0]);

            e.Handled = true;
        }

        public static void DropManyByEnd(this DragEventArgs e, string ext, Action<string[]> onSuccess)
        {
            string[] files = e.GetFilesDrop(ext);

            if (files.Length > 0)
                onSuccess(files);

            e.Handled = true;
        }
    }
}
