#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ArcadeEmpire.EditorTools
{
    /// <summary>Réglages d'import des sons ElevenLabs : musiques et boucles longues en streaming, effets courts décompressés (latence nulle).</summary>
    public class AudioImport : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            if (!assetPath.Contains("/Resources/Audio/")) return;
            var imp = (AudioImporter)assetImporter;
            string n = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            bool longLoop = n.StartsWith("music_") || n == "ambience" || n == "pusher_motor";
            var s = imp.defaultSampleSettings;
            s.loadType = longLoop ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = longLoop ? 0.6f : 0.8f;
            s.preloadAudioData = !longLoop;
            imp.defaultSampleSettings = s;
            imp.forceToMono = !n.StartsWith("music_");
            imp.loadInBackground = longLoop;
        }
    }
}
#endif
