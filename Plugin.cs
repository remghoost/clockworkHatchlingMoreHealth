using GlobalSettings;
using HarmonyLib;
using System.Reflection;
using BepInEx;
using TeamCherry.SharedUtils;
using UnityEngine;
using BepInEx.Logging;
using BepInEx.Configuration;

namespace clockworkHatchlingMoreHealth;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class clockworkHatchlingMoreHealth : BaseUnityPlugin
{
    internal static ManualLogSource Log;
    internal static ConfigEntry<int> HpMultiplier; // config entry

    void Awake()
    {
        Log = Logger;

        // Create config entry
        HpMultiplier = Config.Bind(
            "General",                // Section
            "HpMultiplier",           // Key
            2,                        // Default value
            "Multiplier applied to Clockwork Hatchling (Cogfly) HP." // Description
        );

        var harmony = new Harmony("com.remghoost.clockworkhatchlingmorehealth");
        harmony.PatchAll();
    }
}

[HarmonyPatch(typeof(ClockworkHatchling))]
[HarmonyPatch("DoEnableReset")]
class ClockworkHatchling_DoEnableReset_Patch
{
    static void Prefix(ClockworkHatchling __instance)
    {
        var type = typeof(ClockworkHatchling);
        var hpField = type.GetField("hp", BindingFlags.NonPublic | BindingFlags.Instance);

        if (hpField == null)
        {
            clockworkHatchlingMoreHealth.Log.LogWarning("[ClockworkHatchling_DoEnableReset_Patch] Could not find field: hp");
            return;
        }

        int oldValue = (int)hpField.GetValue(__instance);

        // Only modify if it's the prefab's default value (6)
        if (oldValue == 6)
        {
            int newValue = oldValue * clockworkHatchlingMoreHealth.HpMultiplier.Value;
            hpField.SetValue(__instance, newValue);
            clockworkHatchlingMoreHealth.Log.LogInfo($"[ClockworkHatchling DoEnableReset] hp before: {oldValue}, after: {newValue}");
        }
    }
}
