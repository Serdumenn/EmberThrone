using UnityEditor;
using UnityEngine;

namespace EmberThrone.EditorTools
{
    sealed class GameTextureImport : AssetPostprocessor
    {
        const string Ui = "Assets/EmberThrone/Resources/UI/";
        const string Board = "Assets/EmberThrone/Resources/Board/";

        void OnPreprocessTexture()
        {
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
    }
}
