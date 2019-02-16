using System;
using System.IO;
using System.Linq;
using AndroidHelper.Logic.Interfaces;
using JetBrains.Annotations;
using LongPaths.Logic;

namespace SaveToGameWpf.Logic.Utils
{
    public class TempUtils
    {
        private volatile int _lastEntry;
        private readonly object _entryLock = new object();

        private readonly string _tempFolder;

        private class TempFolderProvider : ITempFolderProvider
        {
            [NotNull] private readonly TempUtils _tempUtils;

            public TempFolderProvider([NotNull] TempUtils tempUtils)
            {
                _tempUtils = tempUtils;
            }

            public string CreateTempFolder()
            {
                return _tempUtils.CreateElement(LDirectory.CreateDirectory);
            }
        }

        private class TempFileProvider : ITempFileProvider
        {
            [NotNull] private readonly TempUtils _tempUtils;

            public TempFileProvider([NotNull] TempUtils tempUtils)
            {
                _tempUtils = tempUtils;
            }

            public string CreateTempFile()
            {
                return _tempUtils.CreateElement(filePath => LFile.Create(filePath).Close());
            }
        }

        public TempUtils(
            [NotNull] GlobalVariables globalVariables
        )
        {
            _tempFolder = globalVariables.TempPath;

            if (LDirectory.Exists(_tempFolder) && LDirectory.EnumerateFileSystemEntries(_tempFolder).Any())
                LDirectory.Delete(_tempFolder, true);
        }

        [NotNull]
        public ITempFolderProvider CreateTempFolderProvider()
        {
            return new TempFolderProvider(this);
        }

        [NotNull]
        public ITempFileProvider CreateTempFileProvider()
        {
            return new TempFileProvider(this);
        }

        [NotNull]
        private string CreateElement([NotNull] Action<string> elementCreator)
        {
            if (elementCreator == null)
                throw new ArgumentNullException(nameof(elementCreator));

            if (!LDirectory.Exists(_tempFolder))
                LDirectory.CreateDirectory(_tempFolder);

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
