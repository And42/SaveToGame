using Microsoft.WindowsAPICodePack.Dialogs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace SaveToGameWpf.Logic.Utils
{
    public static class PickerUtils
    {
        public static (bool success, string filePath) PickFile(
            string title = null,
            string filter = null,
            bool addExtension = true,
            bool checkFileExists = true,
            bool checkPathExists = true
        )
        {
            var openDialog = new OpenFileDialog
            {
                Title = title ?? string.Empty,
                Filter = filter ?? string.Empty,
                AddExtension = addExtension,
                CheckFileExists = checkFileExists,
                CheckPathExists = checkPathExists,
                Multiselect = false
            };

            return openDialog.ShowDialog() == true ? (true, openDialog.FileName) : (false, null);
        }

        public static (bool success, string[] filePaths) PickFiles(
            string title = null,
            string filter = null,
            bool addExtension = true,
            bool checkFileExists = true,
            bool checkPathExists = true
        )
        {
            var openDialog = new OpenFileDialog
            {
                Title = title ?? string.Empty,
                Filter = filter ?? string.Empty,
                AddExtension = addExtension,
                CheckFileExists = checkFileExists,
                CheckPathExists = checkPathExists,
                Multiselect = true
            };

            return openDialog.ShowDialog() == true ? (true, openDialog.FileNames) : (false, null);
        }

        public static (bool success, string folderPath) PickFolder(string title = null)
        {
            var dialog = new CommonOpenFileDialog
            {
                Title = title,
                IsFolderPicker = true,
                Multiselect = false
            };

            return dialog.ShowDialog() == CommonFileDialogResult.Ok ? (true, dialog.FileName) : (false, null);
        }
    }
}
