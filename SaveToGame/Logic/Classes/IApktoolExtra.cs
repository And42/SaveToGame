using AndroidHelper.Logic.Interfaces;

namespace SaveToGameWpf.Logic.Classes;

public interface IApktoolExtra : IApktool
{
    public void ZipAlign(
        string sourceApkPath,
        string alignedApkPath,
        IProcessDataHandler? dataHandler
    );

    public bool TryGetTargetSdkVersion(
        string apkPath,
        out int targetSdkVersion
    );

    public int GetSdkVersion(
        string apkPath
    );
    
    public int GetVersionCode(
        string apkPath
    );
}