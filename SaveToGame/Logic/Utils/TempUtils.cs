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
        private readonly object _providerLock = new object();
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
                return _tempUtils.CreateElement(
                     nameProvider: index => $"temp_folder_{index}",
                     elementCreator: LDirectory.CreateDirectory
                );
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
                return _tempUtils.CreateElement(
                    nameProvider: index => $"temp_file_{index}",
                    elementCreator: filePath => LFile.Create(filePath).Close()
                );
            }
        }

        public TempUtils(
            [NotNull] GlobalVariables globalVariables
        )
        {
            _tempFolder = globalVariables.TempPath;
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
        private string CreateElement([NotNull] Func<int, string> nameProvider, [NotNull] Action<string> elementCreator)
        {
            if (nameProvider == null)
                throw new ArgumentNullException(nameof(nameProvider));
            if (elementCreator == null)
                throw new ArgumentNullException(nameof(elementCreator));

            lock (_providerLock)
            {
                if (!LDirectory.Exists(_tempFolder))
                    LDirectory.CreateDirectory(_tempFolder);

                string[] existingEntries = LDirectory.EnumerateFileSystemEntries(_tempFolder).Select(Path.GetFileName).ToArray();

                for (int index = 1; ; index++)
                {
                    string fileName = nameProvider(index);

                    if (existingEntries.Contains(fileName))
                        continue;

                    string filePath = Path.Combine(_tempFolder, fileName);
                    elementCreator(filePath);
                    return filePath;
                }
            }
        }
    }
}
