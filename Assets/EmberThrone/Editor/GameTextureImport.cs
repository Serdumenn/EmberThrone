using UnityEditor;
using UnityEngine;

namespace EmberThrone.EditorTools
{
    sealed class GameTextureImport : AssetPostprocessor
    {
        const string Ui = "Assets/EmberThrone/Resources/UI/";
        const string Board = "Assets/EmberThrone/Resources/Board/";
        const string Brand = "Assets/EmberThrone/Art/Brand/";
        const string AppIcon = "Assets/EmberThrone/Art/AppIcon/";
        const float SplashLogoPixelsPerUnit = 384f;

        void OnPreprocessTexture()
        {
            if (assetPath.StartsWith(Brand)) { ImportSplashLogo(); return; }
            if (assetPath.StartsWith(AppIcon)) { ImportAppIcon(); return; }

            bool board = assetPath.StartsWith(Board);
            if (!board && !assetPath.StartsWith(Ui)) return;

            bool backdrop = board && System.IO.Path.GetFileName(assetPath).StartsWith("bg_");
            int maxSize = board && !backdrop ? 256 : 2048;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = board || assetPath.StartsWith(Ui + "Art/");
            importer.filterMode = importer.mipmapEnabled ? FilterMode.Trilinear : FilterMode.Bilinear;
            importer.maxTextureSize = maxSize;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = maxSize,
                format = TextureImporterFormat.ASTC_4x4,
                compressionQuality = 100,
            });
        }

        void ImportSplashLogo()
        {
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = SplashLogoPixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 1024;
        }

        void ImportAppIcon()
        {
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
