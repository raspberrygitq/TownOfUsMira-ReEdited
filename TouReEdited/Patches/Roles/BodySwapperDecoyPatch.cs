using HarmonyLib;
using TouReEdited.Modules;
using UnityEngine;

namespace TouReEdited.Patches.Roles;

[HarmonyPatch]
public static class BodySwapperDecoyPatch
{
    [HarmonyPatch(typeof(ReportButton), nameof(ReportButton.DoClick))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.High)]
    public static bool OnReport()
    {
        var local = PlayerControl.LocalPlayer;
        if (local?.Data == null || local.Data.IsDead || MeetingHud.Instance || ExileController.Instance ||
            !BodySwapperDecoySystem.TryClosest(local.GetTruePosition(), out var decoyId, out var distance) ||
            distance >= local.MaxReportDistance)
        {
            return true;
        }

        foreach (var realBody in UnityEngine.Object.FindObjectsOfType<DeadBody>())
        {
            if (realBody && Vector2.Distance(realBody.TruePosition, local.GetTruePosition()) <= distance)
            {
                return true;
            }
        }

        if (!local.IsImpostorAligned())
        {
            BodySwapperDecoySystem.RpcSpringDecoy(local, decoyId);
        }
        return false;
    }

    // Priority.Last: the extension's Charlatan patch rewrites the report button every frame from real bodies
    // only, so the decoy check must run after it or the button never lights up for a fake body.
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void OnHudUpdate(HudManager __instance)
    {
        BodySwapperDecoySystem.Tick();
        var local = PlayerControl.LocalPlayer;
        // `!local` also catches a destroyed player (end of game, leaving), which `?.` lets through.
        if (!local || local.Data == null || local.Data.IsDead || !local.CanMove || MeetingHud.Instance ||
            ExileController.Instance || Minigame.Instance || __instance?.ReportButton == null ||
            local.IsImpostorAligned())
        {
            return;
        }

        if (BodySwapperDecoySystem.TryClosest(local.GetTruePosition(), out _, out var distance) &&
            distance < local.MaxReportDistance)
        {
            __instance.ReportButton.SetActive(true);
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPrefix]
    public static void OnMeetingStart() => BodySwapperDecoySystem.Clear();

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
    [HarmonyPostfix]
    public static void OnGameEnd() => BodySwapperDecoySystem.Clear();
}
