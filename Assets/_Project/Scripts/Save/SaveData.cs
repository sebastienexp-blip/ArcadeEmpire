using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArcadeEmpire.Save
{
    [Serializable]
    public class InventoryEntry
    {
        public string rewardId;
        public int count;
    }

    [Serializable]
    public class PlacedMachineData
    {
        public string machineId;
        public Vector3 position;
        public float rotationY;
        public int upgradeLevel;
    }

    [Serializable]
    public class UpgradeEntry
    {
        public string machineId;
        public int level;
    }

    [Serializable]
    public class MissionProgress
    {
        public string missionId;
        public int progress;
        public bool completed;
    }

    /// <summary>
    /// Contenu complet d'une sauvegarde (JSON). Les champs des phases futures
    /// sont déjà présents pour éviter de casser les sauvegardes existantes.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;

        // Joueur
        public int playerLevel = 1;
        public long experience;

        // Monnaies
        public long coins = 100;
        public long tickets;
        public long premiumTokens;

        // Contenu
        public List<InventoryEntry> inventory = new List<InventoryEntry>();
        public List<string> ownedMachineIds = new List<string>();
        public List<PlacedMachineData> placedMachines = new List<PlacedMachineData>();
        public List<MissionProgress> missions = new List<MissionProgress>();
        public List<string> unlockedRoomIds = new List<string>();
        public List<UpgradeEntry> upgrades = new List<UpgradeEntry>();
        public string missionsDate;   // jour (UTC) des missions en cours, pour le renouvellement quotidien

        // Réglages
        public int graphicsQualityOverride = -1; // -1 = automatique selon la plateforme

        public string lastSavedUtc;
    }
}
