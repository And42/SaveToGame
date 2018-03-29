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
using File = Alphaleonis.Win32.Filesystem.File;

namespace SaveToGameWpf.Logic.Utils
{
    public static class LicensingUtils
    {
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
            FileStream fileStream = File.Open(path, FileMode.Create, FileAccess.Write);
            var binStream = new BinaryWriter(fileStream);

            var sha = new SHA1Managed();

            foreach (var (systemObjects, propertyName) in Utils.GetQueries())
            {
                binStream.Write(systemObjects.Count);
                foreach (ManagementObject systemObject in systemObjects)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(systemObject[propertyName].ToString());
                    bytes = sha.ComputeHash(bytes);

                    binStream.Write(bytes.Length);
                    binStream.Write(bytes);
                }
            }

            binStream.Write(1);

            // key block
            binStream.Write(1);             // key block id
            binStream.Write(sizeof(int));   // block size
            binStream.Write(2);             // version
            // ---

            binStream.Close();
            fileStream.Close();
        }

        public static bool VerifySignature(byte[] hash, byte[] signedHash)
        {
            return RsaSignatureDeformatter.VerifySignature(hash, signedHash);
        }

        public static bool IsLicenseValid(byte[] license)
        {
            var memStream = new MemoryStream(license);
            var binReader = new BinaryReader(memStream);

            var cdrom = new List<byte[]>();
            var card = new List<byte[]>();
            var diskDrive = new List<byte[]>();
            var processor = new List<byte[]>();
            var systemProduct = new List<byte[]>();

            List<byte[]>[] array = { cdrom, card, diskDrive, processor, systemProduct };
            string[] itemsArr = { "DeviceID", "SerialNumber", "SerialNumber", "ProcessorId", "UUID" };

            foreach (List<byte[]> oneList in array)
            {
                int len = binReader.ReadInt32();

                for (var j = 0; j < len; j++)
                {
                    int oneLen = binReader.ReadInt32();

                    byte[] bytes = binReader.ReadBytes(oneLen);

                    oneList.Add(bytes);
                }
            }

            var queries = Utils.GetQueries();

            int found = 0;

            var sha = new SHA1Managed();
            // ReSharper disable once LoopCanBeConvertedToQuery
            for (int i = 0; i < array.Length; i++)
                found += array[i].Count(str => queries[i].systemObjects.Any(query => VerifySignature(sha.ComputeHash(Encoding.UTF8.GetBytes(query[itemsArr[i]].ToString())), str)));

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
    }
}
