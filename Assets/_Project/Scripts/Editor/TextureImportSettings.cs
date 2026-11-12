#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ArcadeEmpire.EditorTools
{
    /// <summary>Réglages d'import automatiques des textures générées (Art/Textures) : *_n.png = carte de normales.</summary>
    public class TextureImportSettings : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/_Project/Art/Textures/")) return;
            var ti = (TextureImporter)assetImporter;
            bool normal = assetPath.EndsWith("_n.png");
            ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.mipmapEnabled = true;
            ti.anisoLevel = 8;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.maxTextureSize = 1024;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
        }
    }
}
#endif
