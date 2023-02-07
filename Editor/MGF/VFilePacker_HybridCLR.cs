using System.IO;
using Saro.MoonAsset.Build;
using UnityEditor;
using UnityEngine;
using Saro.IO;
using HybridCLR.Editor.Commands;
using Saro;
using Saro.Utility;
using System.Text;

namespace HybridCLR.Editor
{
    internal sealed class VFilePacker_HybridCLR : IVFilePacker
    {
        static string vfileName = "hotfix";

        bool IVFilePacker.PackVFile(string dstFolder)
        {
            var dstVFilePath = dstFolder + "/" + vfileName;

            BuildVFile(dstVFilePath, EditorUserBuildSettings.activeBuildTarget);

            return true;
        }

        private static void BuildVFile(string vfilePath, BuildTarget target)
        {
            if (File.Exists(vfilePath))
                File.Delete(vfilePath);

            if (!HybridCLRUtil.IsHotFix)
            {
                Log.INFO("[VFilePacker_HotfixDLL] ENABLE_HOTFIX 未开启");
                return;
            }

            var dllNum = SettingsUtil.HotUpdateAssemblyFilesExcludePreserved.Count + SettingsUtil.AOTAssemblyNames.Count;
            if (dllNum <= 0)
            {
                Log.INFO("[VFilePacker_HotfixDLL] 没有热更相关dll需要打包");
                return;
            }

            CompileDllCommand.CompileDll(target);

            var fileNum = dllNum + 1; // 多了一个 aotDlls json
            using (var vfile = VFileSystem.Open(vfilePath, FileMode.CreateNew, FileAccess.ReadWrite, fileNum, fileNum))
            {
                {
                    string hotfixDllSrcDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
                    foreach (var dll in SettingsUtil.HotUpdateAssemblyFilesExcludePreserved)
                    {
                        string dllPath = $"{hotfixDllSrcDir}/{dll}";
                        var result = vfile.WriteFile($"{dll}", dllPath);
                        if (!result)
                        {
                            Log.ERROR($"[VFilePacker_HotfixDLL] path: {dllPath} 不存在！");
                            continue;
                        }
                    }
                }

                {
                    var aotDlls = JsonHelper.ToJson(SettingsUtil.AOTAssemblyNames);
                    var bytes = Encoding.UTF8.GetBytes(aotDlls);
                    vfile.WriteFile(nameof(aotDlls), bytes, 0, bytes.Length);
                }

                {
                    string aotDllDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
                    foreach (var dll in SettingsUtil.AOTAssemblyNames)
                    {
                        string dllPath = $"{aotDllDir}/{dll}";
                        var result = vfile.WriteFile($"{dll}", dllPath);
                        if (!result)
                        {
                            Log.ERROR($"打包AOT补充元数据dll:{dllPath} 时发生错误，文件不存在。");
                            continue;
                        }
                    }
                }

                Log.INFO("[VFilePacker_HotfixDLL]\n" + string.Join("\n", vfile.GetAllFileInfos()));
            }
        }
    }
}
