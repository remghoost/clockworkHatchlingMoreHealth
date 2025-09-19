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
    internal static ConfigEntry<int> HpMultiplier;
    internal static ConfigEntry<int> BaseHpOverride;

    void Awake()
    {
        Log = Logger;

        HpMultiplier = Config.Bind(
            "General",
            "HpMultiplier",
            2,
            "Multiplier applied to Clockwork Hatchling (Cogfly) HP. Set to 1 if you want to only use BaseHpOverride."
        );

        BaseHpOverride = Config.Bind(
            "General",
            "BaseHpOverride",
            0,
            "Optional fixed base HP for Clockwork Hatchlings. If > 0, this value is used instead of the prefab’s default (6). Final HP = BaseHp * Multiplier."
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

        // Guard so we only modify if it's at the prefab default (6) OR someone has provided an override.
        if (oldValue == 6 || clockworkHatchlingMoreHealth.BaseHpOverride.Value > 0)
        {
            int safeMultiplier = clockworkHatchlingMoreHealth.HpMultiplier.Value;
            if (safeMultiplier <= 0) safeMultiplier = 1; // safety fallback

            int baseHp = clockworkHatchlingMoreHealth.BaseHpOverride.Value > 0
                ? clockworkHatchlingMoreHealth.BaseHpOverride.Value
                : oldValue; // fallback to prefab’s 6 if no override

            int newValue = baseHp * safeMultiplier;
            hpField.SetValue(__instance, newValue);

            clockworkHatchlingMoreHealth.Log.LogInfo(
                $"[ClockworkHatchling DoEnableReset] hp before: {oldValue}, override: {baseHp}, multiplier: {safeMultiplier}, final: {newValue}"
            );
        }
    }
}
