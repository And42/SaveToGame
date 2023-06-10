using AndroidHelper.Logic.Interfaces;
using JetBrains.Annotations;

namespace SaveToGameWpf.Logic.Classes;

public interface IApktoolExtra : IApktool
{
    public void ZipAlign(
        [NotNull] string sourceApkPath,
        [NotNull] string alignedApkPath,
        [CanBeNull] IProcessDataHandler dataHandler
    );

    public bool TryGetTargetSdkVersion(
        [NotNull] string apkPath,
        out int targetSdkVersion
    );

    public int GetSdkVersion(
        [NotNull] string apkPath
    );
    
    public int GetVersionCode(
        [NotNull] string apkPath
    );
}