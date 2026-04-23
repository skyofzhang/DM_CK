// Copyright 2026 极地生存法则
// 根据源字体 Assets/Resources/Fonts/AlibabaPuHuiTi-3-85-Bold.otf
// 生成一个 TMP FontAsset（Dynamic 模式，支持中文全集按需采样），
// 输出到 Assets/Resources/Fonts/AlibabaPuHuiTi-3-85-Bold SDF.asset，
// 并创建关联材质 AlibabaPuHuiTi-3-85-Bold SDF Material.mat。
//
// 菜单：Tools → DrscfZ → Generate Alibaba TMP Font Asset
//
// 设计目标：
//   - Atlas 1024x1024 / Padding 5 / Sample Size 90（与 ChineseFont SDF 对齐）
//   - Dynamic（运行时/编辑器按需生成字形），避免一次性烘焙巨大静态图集
//   - 不使用 EditorUtility.DisplayDialog（阻塞进程铁律）
//
// 参考：Unity 2022.3 TMP 1.x API
//   - TMP_FontAsset.CreateFontAsset(Font font, int sampleSize, int padding,
//         GlyphRenderMode renderMode, int atlasWidth, int atlasHeight,
//         AtlasPopulationMode atlasPopulationMode, bool enableMultiAtlasSupport)

using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using TMPro;

namespace DrscfZ.EditorTools
{
    public static class GenerateAlibabaFontAsset
    {
        private const string SourceOtfPath = "Assets/Resources/Fonts/AlibabaPuHuiTi-3-85-Bold.otf";
        private const string OutputAssetPath = "Assets/Resources/Fonts/AlibabaPuHuiTi-3-85-Bold SDF.asset";
        private const string OutputMaterialPath = "Assets/Resources/Fonts/AlibabaPuHuiTi-3-85-Bold SDF Material.mat";

        // 与 ChineseFont SDF 对齐
        private const int AtlasSize = 1024;
        private const int Padding = 5;
        private const int SampleSize = 90;

        [MenuItem("Tools/DrscfZ/Generate Alibaba TMP Font Asset")]
        public static void Execute()
        {
            Debug.Log("[GenerateAlibabaFontAsset] 开始生成 TMP FontAsset ...");

            // 1. 加载源 OTF 字体
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceOtfPath);
            if (sourceFont == null)
            {
                Debug.LogError($"[GenerateAlibabaFontAsset] 源字体未找到：{SourceOtfPath}");
                return;
            }

            // 2. 若已存在旧 asset,先删除（保持幂等）
            if (File.Exists(OutputAssetPath))
            {
                AssetDatabase.DeleteAsset(OutputAssetPath);
                Debug.Log($"[GenerateAlibabaFontAsset] 已删除旧 asset：{OutputAssetPath}");
            }
            if (File.Exists(OutputMaterialPath))
            {
                AssetDatabase.DeleteAsset(OutputMaterialPath);
                Debug.Log($"[GenerateAlibabaFontAsset] 已删除旧材质：{OutputMaterialPath}");
            }

            // 3. 创建 Dynamic FontAsset（按需采样,支持中文全集）
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                SampleSize,
                Padding,
                GlyphRenderMode.SDFAA,
                AtlasSize,
                AtlasSize,
                AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true
            );

            if (fontAsset == null)
            {
                Debug.LogError("[GenerateAlibabaFontAsset] TMP_FontAsset.CreateFontAsset 返回 null,生成失败");
                return;
            }

            // 设置 hashCode / familyName / styleName 便于识别
            fontAsset.name = "AlibabaPuHuiTi-3-85-Bold SDF";
            fontAsset.hashCode = TMP_TextUtilities.GetHashCode("AlibabaPuHuiTi-3-85-Bold SDF");

            // 4. 写入 asset 文件
            AssetDatabase.CreateAsset(fontAsset, OutputAssetPath);

            // 5. 取出 TMP 自动生成的材质 + atlas texture,作为子资产嵌入同一个 asset 文件
            //    Dynamic 模式下 CreateFontAsset 已经创建了 material + texture,但只是内存对象,
            //    需要 AddObjectToAsset 绑定到 asset 文件,否则重新打开项目后丢失。
            if (fontAsset.material != null)
            {
                // 将材质以子资产方式存入 FontAsset 文件（TMP 默认结构,和 ChineseFont SDF 一致）
                fontAsset.material.name = "AlibabaPuHuiTi-3-85-Bold SDF Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }
            if (fontAsset.atlasTexture != null)
            {
                fontAsset.atlasTexture.name = "AlibabaPuHuiTi-3-85-Bold Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            }

            // 6. 另存一份独立材质(.mat),便于场景 / Prefab 引用(与 ChineseFont SDF 保持一致格式)
            if (fontAsset.material != null)
            {
                var matCopy = new Material(fontAsset.material)
                {
                    name = "AlibabaPuHuiTi-3-85-Bold SDF Material"
                };
                AssetDatabase.CreateAsset(matCopy, OutputMaterialPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[GenerateAlibabaFontAsset] ✅ 已生成 FontAsset：{OutputAssetPath}");
            Debug.Log($"[GenerateAlibabaFontAsset] ✅ 已生成材质副本：{OutputMaterialPath}");
            Debug.Log($"[GenerateAlibabaFontAsset] Atlas={AtlasSize}x{AtlasSize}, " +
                      $"Padding={Padding}, SampleSize={SampleSize}, Mode=Dynamic (按需采样)");
            Debug.Log("[GenerateAlibabaFontAsset] 下一步: 跑 Tools → DrscfZ → Unify All Fonts → AlibabaPuHuiTi");
        }
    }
}
