using GTA;
using GTA.Math;
using StoreRobberyEnhanced.Debug;

namespace StoreRobberyEnhanced.Config
{
    internal static class SafeCrackConfigLoader
    {
        public static SafeCrackSettings Load(IniConfig config)
        {
            SafeCrackSettings settings = new SafeCrackSettings();

            try
            {
                // ------------------------------------------------------------
                // ECONOMY (REUSE EXISTING STORE SETTINGS)
                // ------------------------------------------------------------
                settings.MinCash = config.SafeMinAmount;
                settings.MaxCash = config.SafeMaxAmount;

                // ------------------------------------------------------------
                // SAFECRACK-SPECIFIC SETTINGS (NEW)
                // ------------------------------------------------------------
                SimpleIni ini = new SimpleIni(config.MainIniPath);

                int cooldownDefault = 3000;
                bool padShakeDefault = true;
                bool loadOptionalDefault = false;

                settings.CooldownMs = ini.ReadInt("Store Settings", "SafeCrackCooldownMs", cooldownDefault);
                settings.PadShake = ini.ReadBool("Store Settings", "SafeCrackPadShake", padShakeDefault);
                settings.LoadOptionalSafes = ini.ReadBool("Store Settings", "SafeCrackLoadOptionalSafes", loadOptionalDefault);

                ini.WriteInt("Store Settings", "SafeCrackCooldownMs", settings.CooldownMs);
                ini.WriteBool("Store Settings", "SafeCrackPadShake", settings.PadShake);
                ini.WriteBool("Store Settings", "SafeCrackLoadOptionalSafes", settings.LoadOptionalSafes);

                ini.Save();

                // ------------------------------------------------------------
                // VALIDATED SAFE POSITIONS (21 STORES)
                // ------------------------------------------------------------
                settings.SafeLocations.AddRange(new[]
                {
                    new Vector3(-3047.8590f, 585.6470f, 7.9089f),
                    new Vector3(-3250.0610f, 1004.4640f, 12.8307f),
                    new Vector3(378.2274f, 333.3271f, 103.5664f),
                    new Vector3(2672.7740f, 3286.5960f, 55.2411f),
                    new Vector3(1734.9160f, 6420.7430f, 35.0372f),
                    new Vector3(1734.8350f, 6420.8540f, 35.0372f),
                    new Vector3(546.3298f, 2662.7580f, 42.1565f),
                    new Vector3(2549.2710f, 384.9151f, 108.6229f),
                    new Vector3(1959.2110f, 3748.8450f, 32.3437f),
                    new Vector3(28.3430f, -1339.2300f, 29.4970f),
                    new Vector3(1394.9200f, 3613.8970f, 34.9809f),
                    new Vector3(-43.3559f, -1748.3580f, 29.4210f),
                    new Vector3(1707.9480f, 4920.3650f, 42.0637f),
                    new Vector3(-709.6928f, -904.0578f, 19.2156f),
                    new Vector3(1159.5580f, -314.0486f, 69.2051f),
                    new Vector3(-1829.1980f, 798.8428f, 138.1906f),
                    new Vector3(-2959.6290f, 387.1568f, 14.0433f),
                    new Vector3(1126.8170f, -980.1575f, 45.4158f),
                    new Vector3(-1478.8740f, -375.4337f, 39.1634f),
                    new Vector3(1169.3060f, 2717.8160f, 37.1577f),
                    new Vector3(-1220.8040f, -916.0294f, 11.3263f)
                });

                settings.SafeRotations.AddRange(new[]
                {
                    111.69f, 81.24f, 341.31f, 61.26f, 330.71f,
                    343.00f, 186.56f, 84.84f, 15.66f, 356.39f,
                    15.69f, 50.15f, 315.88f, 90.68f, 97.08f,
                    136.25f, 168.93f, 359.61f, 227.58f, 270.44f,
                    118.63f
                });
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException("SafeCrackConfigLoader.Load", ex);
            }

            return settings;
        }
    }
}
