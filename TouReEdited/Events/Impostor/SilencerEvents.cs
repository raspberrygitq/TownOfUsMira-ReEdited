using HarmonyLib;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.GameOptions;
using TouReEdited.Options.Roles.Impostor;
using TouReEdited.Roles.Impostor.ImpostorPower;
using UnityEngine;

namespace TouReEdited.Events.Impostor;

[HarmonyPatch]
public static class SilencerEvents
{
    private static float totalReductionSeconds;
    private static float pendingReductionSeconds;
    private static bool appliedToMeeting;

    [RegisterEvent]
    public static void OnAfterMurder(AfterMurderEvent @event)
    {
        if (@event.Source?.Data?.Role is SilencerRole)
        {
            totalReductionSeconds += OptionGroupSingleton<SilencerOptions>.Instance.SecondsPerKill;
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
    [HarmonyPostfix]
    public static void OnGameEnd()
    {
        totalReductionSeconds = 0f;
        pendingReductionSeconds = 0f;
        appliedToMeeting = false;
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPostfix]
    public static void OnMeetingStart()
    {
        appliedToMeeting = false;
        var options = OptionGroupSingleton<SilencerOptions>.Instance;
        if (options.NormalVotingTimeWhenDead &&
            !PlayerControl.AllPlayerControls.ToArray().Any(player =>
                player?.Data?.Role is SilencerRole && !player.Data.IsDead))
        {
            totalReductionSeconds = 0f;
            pendingReductionSeconds = 0f;
            return;
        }

        var votingTime = GameOptionsManager.Instance?.currentNormalGameOptions?.VotingTime ?? 0;
        pendingReductionSeconds = Mathf.Clamp(totalReductionSeconds, 0f,
            Mathf.Max(0f, votingTime - options.MinimumVotingTime));
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    [HarmonyPostfix]
    public static void OnMeetingUpdate(MeetingHud __instance)
    {
        if (appliedToMeeting || pendingReductionSeconds <= 0f || __instance == null ||
            __instance.state is not (MeetingHud.MeetingStates.NotVoted or MeetingHud.MeetingStates.Voted))
        {
            return;
        }

        __instance.discussionTimer += pendingReductionSeconds;
        appliedToMeeting = true;
    }
}
