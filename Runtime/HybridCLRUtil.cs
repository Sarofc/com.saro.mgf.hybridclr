using System;
using System.Collections.Generic;
using System.Text;
using Saro;
using Saro.Core;
using Saro.Utility;

namespace HybridCLR
{
    public static class HybridCLRUtil
    {
        public static bool IsHotFix
        {
            get
            {
#if ENABLE_HOTFIX
                return true;
#else
                return false;
#endif
            }
        }

        public static string s_HotFixDLL = "HotFix.dll";

        /// <summary>
        /// 为aot assembly加载原始metadata， 这个代码放aot或者热更新都行。
        /// 一旦加载后，如果AOT泛型函数对应native实现不存在，则自动替换为解释模式执行
        public static void LoadMetadataForAOTAssembly()
        {
#if !UNITY_EDITOR
            LoadMetadataForAOTAssembly_Internal();
#endif
        }

        private static async void LoadMetadataForAOTAssembly_Internal()
        {
            var assetManager = IAssetManager.Current;

#if ENABLE_LOG
            var sb = new StringBuilder(1024);
            sb.AppendLine("LoadMetadataForAOTAssembly:");
#endif

            using var vfile = await assetManager.OpenVFileAsync("Assets/ResRaw/hotfix");
            var bytes = vfile.ReadFile("aotDlls");
            var json = Encoding.UTF8.GetString(bytes);
            var aotDlls = JsonHelper.FromJson<List<string>>(json);

            foreach (var aotDllName in aotDlls)
            {
                byte[] dllBytes = vfile.ReadFile(aotDllName);
                if (dllBytes == null)
                {
                    Log.ERROR($"LoadMetadataForAOTAssembly failed. file not found: {aotDllName}.");
                    continue;
                }

                unsafe
                {
                    HomologousImageMode mode = HomologousImageMode.SuperSet;
                    LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, mode);
#if ENABLE_LOG
                    sb.AppendLine($"dll : {aotDllName}. mode:{mode} ret : {err}");
#endif
                }
            }

#if ENABLE_LOG
            Log.ERROR(sb.ToString());
#endif
        }
    }
}
