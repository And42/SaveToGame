using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using AndroidHelper.Logic;
using AndroidHelper.Logic.Interfaces;
using AndroidHelper.Logic.Utils;
using JetBrains.Annotations;

namespace SaveToGameWpf.Logic.Classes;

public class ApkSignerApktool : IApktoolExtra
{
    [NotNull]
    private readonly IApktool _apktool;
    
    public string JavaPath => _apktool.JavaPath;
    public string ApktoolPath => _apktool.ApktoolPath;
    public string SignApkPath => _apktool.SignApkPath;
    public string BaksmaliPath => _apktool.BaksmaliPath;
    public string SmaliPath => _apktool.SmaliPath;
    public string DefaultKeyPemPath => _apktool.DefaultKeyPemPath;
    public string DefaultKeyPkPath => _apktool.DefaultKeyPkPath;
    [NotNull]
    public string ApkSignerPath { get; }
    [NotNull]
    public string ZipalignPath { get; }
    [NotNull]
    public string Aapt2Path { get; }
    
    public ApkSignerApktool(
        [NotNull] IApktool apktool,
        [NotNull] string apkSignerPath,
        [NotNull] string zipalignPath,
        [NotNull] string aapt2Path
    )
    {
        _apktool = apktool;
        ApkSignerPath = apkSignerPath;
        ZipalignPath = zipalignPath;
        Aapt2Path = aapt2Path;
    }
    
    public void Decompile(string apkPath, string destinationFolder, IProcessDataHandler dataHandler)
    {
        _apktool.Decompile(apkPath, destinationFolder, dataHandler);
    }

    public void Compile(string projectFolderPath, string destinationApkPath, IProcessDataHandler dataHandler, out List<Error> errors)
    {
        _apktool.Compile(projectFolderPath, destinationApkPath, dataHandler, out errors);
    }

    public void Sign(
        [NotNull] string sourceApkPath,
        [NotNull] string signedApkPath,
        [NotNull] ITempFileProvider tempFileProvider,
        [CanBeNull] IProcessDataHandler dataHandler,
        bool deleteMetaInf
    )
    {
        if (sourceApkPath == null)
            throw new ArgumentNullException(nameof(sourceApkPath));
        if (signedApkPath == null)
            throw new ArgumentNullException(nameof(signedApkPath));
        if (tempFileProvider == null)
            throw new ArgumentNullException(nameof(tempFileProvider));
        if (DefaultKeyPemPath == null)
            throw new InvalidOperationException($"`{nameof(DefaultKeyPemPath)}` has to be set");
        if (DefaultKeyPkPath == null)
            throw new InvalidOperationException($"`{nameof(DefaultKeyPkPath)}` has to be set");
        if (ApkSignerPath == null)
            throw new InvalidOperationException($"`{nameof(ApkSignerPath)}` has to be set");

        using (TempUtils.TempFileDisposable tempFileDisposable = TempUtils.UseTempFile(tempFileProvider))
        {
            File.Copy(sourceApkPath, tempFileDisposable.TempFile, true);
            if (deleteMetaInf)
                RemoveMetaInf(tempFileDisposable.TempFile);
           
            RunJava(
                fileName: ApkSignerPath,
                arguments: $"sign --verbose --key \"{DefaultKeyPkPath}\" --cert \"{DefaultKeyPemPath}\" \"{tempFileDisposable.TempFile}\"",
                dataHandler: dataHandler
            );
            
            File.Copy(sourceFileName: tempFileDisposable.TempFile, destFileName: signedApkPath, overwrite: true);
        }
    }

    public void ZipAlign(
        [NotNull] string sourceApkPath,
        [NotNull] string alignedApkPath,
        [CanBeNull] IProcessDataHandler dataHandler
    )
    {
        if (sourceApkPath == null)
            throw new ArgumentNullException(nameof(sourceApkPath));
        if (alignedApkPath == null)
            throw new ArgumentNullException(nameof(alignedApkPath));
        if (ZipalignPath == null)
            throw new InvalidOperationException($"`{nameof(ZipalignPath)}` has to be set");

        RunProc(
            fileName: ZipalignPath,
            arguments: $"-p -f 4 \"{sourceApkPath}\" \"{alignedApkPath}\"",
            dataHandler: dataHandler
        );
    }

    public bool TryGetTargetSdkVersion(
        [NotNull] string apkPath,
        out int targetSdkVersion
    )
    {
        targetSdkVersion = 0;
        
        string output = GetAapt2BadgingOutput(apkPath);
        Match match = Regex.Match(input: output, pattern: @"targetSdkVersion:'(?<targetSdkVersion>\d+)'");
        if (!match.Success)
        {
            Debug.WriteLine($"Can't find `targetSdkVersion` in `{output}`");
            return false;
        }

        string targetSdkVersionString = match.Groups["targetSdkVersion"].Value;
        if (!int.TryParse(targetSdkVersionString, out targetSdkVersion))
        {
            Debug.WriteLine($"Can't parse `targetSdkVersion` from `{targetSdkVersionString}`");
            return false;
        }

        return true;
    }

    public int GetSdkVersion(
        [NotNull] string apkPath
    )
    {
        string output = GetAapt2BadgingOutput(apkPath);
        Match match = Regex.Match(input: output, pattern: @"sdkVersion:'(?<sdkVersion>\d+)'");
        if (!match.Success)
            throw new InvalidOperationException($"Can't find `sdkVersion` in `{output}`");
        
        string sdkVersionString = match.Groups["sdkVersion"].Value;
        int sdkVersion;
        if (!int.TryParse(sdkVersionString, out sdkVersion))
            throw new InvalidOperationException($"Can't parse `sdkVersion` from `{sdkVersionString}`");
        
        return sdkVersion;
    }

    public int GetVersionCode(
        [NotNull] string apkPath
    )
    {
        string output = GetAapt2BadgingOutput(apkPath);
        Match match = Regex.Match(input: output, pattern: @"versionCode='(?<versionCode>\d+)'");
        if (!match.Success)
            throw new InvalidOperationException($"Can't find `versionCode` in `{output}`");
        
        string versionCodeString = match.Groups["versionCode"].Value;
        int versionCode;
        if (!int.TryParse(versionCodeString, out versionCode))
            throw new InvalidOperationException($"Can't parse `versionCode` from `{versionCodeString}`");
        
        return versionCode;
    }

    private string GetAapt2BadgingOutput([NotNull] string apkPath)
    {
        if (Aapt2Path == null)
            throw new InvalidOperationException($"`{nameof(Aapt2Path)}` has to be set");

        var output = new StringBuilder();
        RunProc(
            fileName: Aapt2Path,
            arguments: $"dump badging \"{apkPath}\"",
            dataHandler: new ProcessDataCombinedHandler(data => output.AppendLine(data))
        );

        return output.ToString();
    }

    public void InstallFramework(string pathToFramework, IProcessDataHandler dataHandler)
    {
        _apktool.InstallFramework(pathToFramework, dataHandler);
    }

    public void Baksmali(string apkPath, string resultFolder, ITempFolderProvider tempFolderProvider, IProcessDataHandler dataHandler)
    {
        _apktool.Baksmali(apkPath, resultFolder, tempFolderProvider, dataHandler);
    }

    public void Smali(string folderWithSmali, string resultFolder, IProcessDataHandler dataHandler)
    {
        _apktool.Smali(folderWithSmali, resultFolder, dataHandler);
    }

    public void ExtractSimpleManifest(string apkPath, string resultManifestPath, ITempFolderProvider tempFolderProvider)
    {
        _apktool.ExtractSimpleManifest(apkPath, resultManifestPath, tempFolderProvider);
    }

    public void FixErrors(IEnumerable<Error> errors)
    {
        _apktool.FixErrors(errors);
    }

    public void RemoveMetaInf(string fileName)
    {
        _apktool.RemoveMetaInf(fileName);
    }

    public string GetApktoolVersion()
    {
        return _apktool.GetApktoolVersion();
    }

    private void RunJava(string fileName, string arguments, IProcessDataHandler dataHandler)
    {
        MethodInfo runJava = _apktool.GetType().GetMethod(
            name: "RunJava",
            bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance,
            types: new []
            {
                typeof(string),
                typeof(string),
                typeof(IProcessDataHandler)
            }
        );
        if (runJava == null)
            throw new InvalidOperationException($"`{nameof(runJava)}` is null");
        
        runJava.Invoke(_apktool, parameters: new object[] { fileName, arguments, dataHandler });
    }

    private static void RunProc(string fileName, string arguments, IProcessDataHandler dataHandler)
    {
        MethodInfo runProc = typeof(Apktool).GetMethod(
            name: "RunProc",
            bindingAttr: BindingFlags.NonPublic | BindingFlags.Static,
            types: new []
            {
                typeof(string),
                typeof(string),
                typeof(IProcessDataHandler)
            }
        );
        if (runProc == null)
            throw new InvalidOperationException($"`{nameof(runProc)}` is null");
        
        runProc.Invoke(null, parameters: new object[] { fileName, arguments, dataHandler });
    }
}