using System;
using System.IO;

namespace StoreRobberyTrackerMod.Config
{
    internal static class DefaultConfigCreator
    {
        private const string FolderName = "scripts/StoreRobberyTracker";
        private const string FileName = "StoreRobberyTracker.ini";

        public static void EnsureDefaultConfigExists(string gameDir)
        {
            try
            {
                string folderPath = Path.Combine(gameDir, FolderName);
                string filePath = Path.Combine(folderPath, FileName);

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                if (File.Exists(filePath))
                    return;

                File.WriteAllText(filePath, DefaultIniText);
            }
            catch (Exception ex)
            {
                File.AppendAllText("StoreRobberyTracker_Error.log",
                    "[DefaultConfigCreator] " + ex + Environment.NewLine);
            }
        }

        private static readonly string DefaultIniText =
        @"[General]
        EnableCameras=true
        EnableStalkerCall=true
        CooldownMinutes=20
        PayoutMultiplier=1.0

        [Safe]
        MinReward=500
        MaxReward=1500
        CrackTimeSeconds=25
        ";
    }
}
