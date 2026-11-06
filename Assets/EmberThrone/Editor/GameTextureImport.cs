using UnityEditor;
using UnityEngine;

namespace EmberThrone.EditorTools
{
    /// <summary>
    /// Import rules for the game's images: Resources/UI (interface) and Resources/Board (3D renders of pieces
    /// and obstacles, the game backdrop). The 9-slice chrome keeps its exact pixel size, because USS slice values
    /// are in pixels. Rendered art gets mipmaps, as it is drawn smaller than it is rendered; board pieces are
    /// capped at 256 px, about twice a cell on a phone. Android gets ASTC 4x4 (a quarter of the memory, no
    /// visible loss on gradients); the editor keeps them uncompressed.
    /// </summary>
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
