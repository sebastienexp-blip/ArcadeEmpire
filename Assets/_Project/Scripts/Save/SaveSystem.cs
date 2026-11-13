using System;
using System.IO;
using UnityEngine;

namespace ArcadeEmpire.Save
{
    /// <summary>
    /// Lecture/écriture JSON de la sauvegarde. Écriture sûre : fichier temporaire,
    /// puis remplacement, avec une copie de secours (.bak) en cas de corruption.
    /// </summary>
    public static class SaveSystem
    {
        private const string FileName = "arcade_empire_save.json";

        private static string MainPath => Path.Combine(Application.persistentDataPath, FileName);
        private static string BackupPath => MainPath + ".bak";
        private static string TempPath => MainPath + ".tmp";

        public static bool HasSave() => File.Exists(MainPath) || File.Exists(BackupPath);

        public static bool Save(SaveData data)
        {
            try
            {
                data.lastSavedUtc = DateTime.UtcNow.ToString("o");
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(TempPath, json);

                if (File.Exists(MainPath))
                    File.Copy(MainPath, BackupPath, true);

                if (File.Exists(MainPath))
                    File.Delete(MainPath);
                File.Move(TempPath, MainPath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Échec de la sauvegarde : {e.Message}");
                return false;
            }
        }

        /// <summary>Charge la sauvegarde, ou la copie de secours, ou une nouvelle partie.</summary>
        public static SaveData Load()
        {
            SaveData data = TryRead(MainPath) ?? TryRead(BackupPath);
            return data ?? new SaveData();
        }

        public static void DeleteSave()
        {
            foreach (string path in new[] { MainPath, BackupPath, TempPath })
                if (File.Exists(path)) File.Delete(path);
        }

        private static SaveData TryRead(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                string text = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(text) || !text.TrimEnd().EndsWith("}"))
                {
                    Debug.LogWarning($"[SaveSystem] {path} est tronqué ou vide, ignoré.");
                    return null;
                }
                var data = JsonUtility.FromJson<SaveData>(text);
                if (data == null || data.version <= 0 || string.IsNullOrEmpty(data.lastSavedUtc))
                {
                    Debug.LogWarning($"[SaveSystem] {path} ne ressemble pas à une sauvegarde valide, ignoré.");
                    return null;
                }
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Lecture impossible de {path} : {e.Message}");
                return null;
            }
        }
    }
}
