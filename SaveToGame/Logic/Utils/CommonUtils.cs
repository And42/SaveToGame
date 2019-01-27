using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using JetBrains.Annotations;
using SaveToGameWpf.Logic.Classes;
using SaveToGameWpf.Resources;
using SmaliParser;

namespace SaveToGameWpf.Logic.Utils
{
    internal static class CommonUtils
    {
        [NotNull]
        private static readonly Encoding SmaliEncoding = new UTF8Encoding(false);
        [NotNull]
        private static readonly string NewLine = Environment.NewLine;

        [NotNull]
        private static string GenByteString(byte input)
        {
            if (input <= 127)
                return "0x" + input.ToString("x");
            return "-0x" + (256 - input).ToString("x");
        }

        public static void EncryptFile(
            [NotNull] string filePath,
            [NotNull] string outputPath,
            [NotNull] byte[] iv,
            [NotNull] byte[] key
        )
        {
            Guard.NotNullArgument(filePath, nameof(filePath));
            Guard.NotNullArgument(outputPath, nameof(outputPath));
            Guard.NotNullArgument(iv, nameof(iv));
            Guard.NotNullArgument(key, nameof(key));

            if (filePath == outputPath)
                throw new ArgumentException($"`{nameof(filePath)}` must differ from `{nameof(outputPath)}`");

            using (Aes aesAlg = Aes.Create())
            {
                if (aesAlg == null)
                    throw new Exception("AES encryption not found");

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(key, iv);

                using (var reader = IOUtils.FileOpenRead(filePath))
                using (var msEncrypt = IOUtils.FileCreate(outputPath))
                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    int read;
                    byte[] buffer = new byte[1024];

                    while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                        csEncrypt.Write(buffer, 0, read);
                }
            }
        }

        public static void GenerateAndSaveSmali(
            [NotNull] string filePath,
            [NotNull] byte[] iv,
            [NotNull] byte[] key,
            bool addSave,
            [NotNull] string message,
            int messagesCount
        )
        {
            Guard.NotNullArgument(filePath, nameof(filePath));
            Guard.NotNullArgument(iv, nameof(iv));
            Guard.NotNullArgument(key, nameof(key));
            Guard.NotNullArgument(message, nameof(message));

            if (messagesCount < 0)
                throw new ArgumentOutOfRangeException(nameof(messagesCount));

            message = message.Replace("\r", "").Replace("\n", "\\n");

            string text = FileResources.SavesRestoringPortable;

            var toAdd = new StringBuilder();

            var resourceMessage = FileResources.MessageShow;

            for (int i = 0; i < messagesCount; i++)
                toAdd.Append(resourceMessage.Replace("[(message)]", PadBoth(message, i)));

            text = text.Replace("[(message)]", toAdd.ToString());

            toAdd.Clear();

            string nl = NewLine;

            for (byte i = 0; i < iv.Length; i++)
            {
                string temp = $"    const/16 v12, {GenByteString(i)}{nl}" +
                              $"{nl}" +
                              $"    const/16 v13, {GenByteString(iv[i])}{nl}" +
                              $"{nl}" +
                              $"    aput-byte v13, v1, v12{nl}" +
                              $"{nl}";

                toAdd.Append(temp);
            }

            text = text.Replace("[(iv_bytes_init)]", toAdd.ToString());

            toAdd.Clear();

            for (byte i = 0; i < iv.Length; i++)
            {
                string temp = $"    const/16 v12, {GenByteString(i)}{nl}" +
                              $"{nl}" +
                              $"    const/16 v13, {GenByteString(key[i])}{nl}" +
                              $"{nl}" +
                              $"    aput-byte v13, v7, v12{nl}" +
                              $"{nl}";

                toAdd.Append(temp);
            }

            text = text.Replace("[(key_bytes_init)]", toAdd.ToString());

            using (var reader = new StringReader(text))
            {
                var cls = SmaliClass.ParseStream(reader);

                var dict = new Dictionary<string, SmaliMethod>();

                int messagesLength = cls.Methods.Sum(method => SmaliStringEncryptor.EncryptMethod(method, cls.Name, dict));

                foreach (var elem in dict)
                    cls.Methods.Add(elem.Value);

                using (var writer = new StringWriter())
                {
                    cls.Save(writer);

                    text = writer.ToString().Replace("[(message_length)]", messagesLength.ToString("X"));
                    text = text.Replace("[(data_restore_call)]", addSave ? FileResources.DataRestoreCall : "");

                    IOUtils.FileWriteAllText(filePath, text, SmaliEncoding);
                }
            }
        }

        private static string PadBoth(
            [CanBeNull] string source,
            int padding,
            char paddingChar = ' '
        )
        {
            if (padding < 0)
                throw new ArgumentOutOfRangeException(nameof(padding));

            if (source == null)
                return new string(paddingChar, padding * 2);

            return source.PadLeft(source.Length + padding, paddingChar).PadRight(source.Length + padding + padding, paddingChar);
        }
    }
}
