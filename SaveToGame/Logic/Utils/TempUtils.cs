using System;
using System.IO;
using System.Linq;
using AndroidHelper.Logic.Interfaces;

namespace SaveToGameWpf.Logic.Utils
{
    public class TempUtils
    {
        private volatile int _lastEntry;
        private readonly object _entryLock = new();
        private readonly string _tempFolder;

        private class TempFolderProvider : ITempFolderProvider
        {
            private readonly TempUtils _tempUtils;

            public TempFolderProvider(TempUtils tempUtils)
            {
                _tempUtils = tempUtils;
            }

            public string CreateTempFolder()
            {
                return _tempUtils.CreateElement(it => Directory.CreateDirectory(it));
            }
        }

        private class TempFileProvider : ITempFileProvider
        {
            private readonly TempUtils _tempUtils;

            public TempFileProvider(TempUtils tempUtils)
            {
                _tempUtils = tempUtils;
            }

            public string CreateTempFile()
            {
                return _tempUtils.CreateElement(filePath => File.Create(filePath).Close());
            }
        }

        public TempUtils(
            GlobalVariables globalVariables
        )
        {
            _tempFolder = globalVariables.TempPath;

            if (Directory.Exists(_tempFolder) && Directory.EnumerateFileSystemEntries(_tempFolder).Any())
                Directory.Delete(_tempFolder, true);
        }

        public ITempFolderProvider CreateTempFolderProvider()
        {
            return new TempFolderProvider(this);
        }

        public ITempFileProvider CreateTempFileProvider()
        {
            return new TempFileProvider(this);
        }

        private string CreateElement(Action<string> elementCreator)
        {
            if (elementCreator == null)
                throw new ArgumentNullException(nameof(elementCreator));

            if (!Directory.Exists(_tempFolder))
                Directory.CreateDirectory(_tempFolder);

            int entryIndex;
            lock (_entryLock)
            {
                if (_lastEntry == int.MaxValue)
                    _lastEntry = 1;
                else
                    _lastEntry++;

                entryIndex = _lastEntry;
            }

            string filePath = Path.Combine(_tempFolder, $"temp_entry_{entryIndex}");
            elementCreator(filePath);
            return filePath;
        }
    }
}
