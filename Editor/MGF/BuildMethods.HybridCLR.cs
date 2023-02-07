using Saro.MoonAsset.Build;
using HybridCLR.Editor.Commands;

namespace HybridCLR.Editor
{
    internal class BuildMethods : IBuildProcessor
    {
        [MoonAssetBuildMethod(49, "<color=red>[HotFix]</color> " + nameof(GenerateAll), tooltip = "HybridCLR 按照必要的顺序，执行所有生成操作，适合打包前操作")]
        public static void GenerateAll()
        {
            PrebuildCommand.GenerateAll();
        }
    }
}
