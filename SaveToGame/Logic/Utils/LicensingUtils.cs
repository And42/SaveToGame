using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using SaveToGameWpf.Logic.Classes;
using SaveToGameWpf.Resources.Localizations;
using SaveToGameWpf.Windows;

namespace SaveToGameWpf.Logic.Utils
{
    public static class LicensingUtils
    {
        private static class ComputerInfo
        {
            public const string DiskDrive = "Win32_DiskDrive";
            public const string Processor = "Win32_Processor";
            public const string SystemProduct = "Win32_ComputerSystemProduct";
            // ReSharper disable once InconsistentNaming
            public const string CDROM = "Win32_CDROMDrive";
            public const string Card = "CIM_Card";

            /// <summary>
            /// Returns all management objects from the provided place
            /// </summary>
            /// <param name="from">The place to selectfrom</param>
            public static List<ManagementObject> GetQueryList(string from)
            {
                var winQuery = new ObjectQuery("SELECT * FROM " + from);
                var searcher = new ManagementObjectSearcher(winQuery);

                return searcher.Get().Cast<ManagementObject>().ToList();
            }
        }

        private const string RsaParamsInXml =
            "<RSAKeyValue><Modulus>smneh/6PWonzZSD4sPwd76llm6Qow0UhcG3vIVMqxhSBRC3QcvC47ej/XqsXc2h7Le60VO3n8UIR4DYaBwbDli2unfTrwnAq6sAINd0HathMce6njEuVSwWwhF8ioN9xDiwqs3iDq59hghHicaveAvgwkEo8v0tbnsLPt7l+FS6bjmwK9b9yMslJVw4Nu1Ar2F3pqOCCUfRwCU7PvGvfloLmcuG9dIVVaJEtNHDispQUMwprWN3hzKFVxvNTKPwJEouQ8MFOT0ycxzsZWJibeapQew8fz/An2FGi6hDQp0mcGN42HnPrPrYUmZnSbG6FYAvFoImBEpOTS0WxZgfkwQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        private static readonly RSAPKCS1SignatureDeformatter RsaSignatureDeformatter;

        static LicensingUtils()
        {
            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(RsaParamsInXml);

            RsaSignatureDeformatter = new RSAPKCS1SignatureDeformatter(rsa);
            RsaSignatureDeformatter.SetHashAlgorithm("SHA1");
        }

        public static void GenerateNotActivatedLicense(string path)
        {
            using (FileStream fileStream = File.Open(path, FileMode.Create, FileAccess.Write))
            {
                var binStream = new BinaryWriter(fileStream);

                var sha = new SHA1Managed();

                foreach (var (_, systemObjects) in GetQueries())
                {
                    binStream.Write(systemObjects.Count);
                    foreach (string systemObject in systemObjects)
                    {
                        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(systemObject));

                        binStream.Write(bytes.Length);
                        binStream.Write(bytes);
                    }
                }

                binStream.Write(1);

                // key block
                binStream.Write(1); // key block id
                binStream.Write(sizeof(int)); // block size
                binStream.Write(2); // version
                // ---

                binStream.Close();
            }
        }

        public static bool VerifySignature(byte[] hash, byte[] signedHash)
        {
            return RsaSignatureDeformatter.VerifySignature(hash, signedHash);
        }

        public static bool IsLicenseValid(byte[] license)
        {
            var memStream = new MemoryStream(license);
            var binReader = new BinaryReader(memStream);

            var fileHashes = new Dictionary<string, List<byte[]>>();

            void AddItem(string key)
            {
                var typeElements = new List<byte[]>();

                int itemsCount = binReader.ReadInt32();
  
                for (var j = 0; j < itemsCount; j++)
                {
                    int itemBytesSize = binReader.ReadInt32();
                    byte[] bytes = binReader.ReadBytes(itemBytesSize);
                    typeElements.Add(bytes);
                }

                fileHashes.Add(key, typeElements);
            }

            AddItem(ComputerInfo.CDROM);
            AddItem(ComputerInfo.Card);
            AddItem(ComputerInfo.DiskDrive);
            AddItem(ComputerInfo.Processor);
            AddItem(ComputerInfo.SystemProduct);

            var queries = GetQueries();

            int found = 0;

            var sha = new SHA1Managed();

            foreach ((string elementSystemName, List<string> systemObjects) in queries)
            {
                List<byte[]> fileElementHashes = fileHashes[elementSystemName];
                List<byte[]> systemElementHashes = systemObjects.ConvertAll(it => sha.ComputeHash(Encoding.UTF8.GetBytes(it)));

                foreach (byte[] fileElementHash in fileElementHashes)
                    if (systemElementHashes.Any(systemHash => VerifySignature(systemHash, fileElementHash)))
                        found++;
            }

            var keyVersion = 1;

            if (memStream.Position < memStream.Length)
            {
                int numberOfBlocks = binReader.ReadInt32();

                for (int i = 0; i < numberOfBlocks; i++)
                {
                    var blockId = binReader.ReadInt32();
                    // ReSharper disable once UnusedVariable
                    var blockSize = binReader.ReadInt32();

                    switch (blockId)
                    {
                        case BlockIds.KeyVersionId:
                            keyVersion = binReader.ReadInt32();
                            break;
                        case BlockIds.AdditionalFileKey:
                            var base64BytesLen = binReader.ReadInt32();
                            var base64StringBytes = binReader.ReadBytes(base64BytesLen);

                            var base64String = Encoding.ASCII.GetString(base64StringBytes);

                            GlobalVariables.AdditionalFilePassword =
                                Encoding.UTF8.GetString(Convert.FromBase64String(base64String));
                            break;
                    }
                }
            }

            binReader.Close();
            memStream.Close();

            if (keyVersion != 2)
            {
                Application.Current.Dispatcher.InvokeAction(() =>
                    MessBox.ShowDial(MainResources.LicenseFormatUpdated_Text, MainResources.Information_Title)
                );

                return false;
            }

            return found >= 2;
        }

        public static bool IsLicenseValid(ArrayList license)
        {
            if (license == null || license.Count == 0)
                return false;

            return IsLicenseValid(license.Cast<byte>().ToArray());
        }

        private static (string sourceType, List<string> systemObjects)[] GetQueries()
        {
            (string from, string propName, Predicate<ManagementObject> checker)[] items =
            {
                (ComputerInfo.CDROM, "DeviceID", _ => true),
                (ComputerInfo.Card, "SerialNumber", _ => true),
                (ComputerInfo.DiskDrive, "SerialNumber", it => it["MediaType"]?.ToString() == "Fixed hard disk media"),
                (ComputerInfo.Processor, "ProcessorId", _ => true),
                (ComputerInfo.SystemProduct, "UUID", _ => true)
            };

            return Array.ConvertAll(items, item =>
            {
                var queries = ComputerInfo.GetQueryList(item.from);
                return
                (
                    item.from,
                    queries
                        .Where(it => item.checker(it))
                        .Select(it => it[item.propName]?.ToString())
                        .Where(it => !string.IsNullOrEmpty(it))
                        .ToList()
                );
            });
        }
    }
}
