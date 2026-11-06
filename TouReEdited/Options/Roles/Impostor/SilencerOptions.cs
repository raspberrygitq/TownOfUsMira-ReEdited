using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TouReEdited.Roles.Impostor.ImpostorPower;

namespace TouReEdited.Options.Roles.Impostor;

public sealed class SilencerOptions : AbstractRoleOptionGroup<SilencerRole>
{
    public override string GroupName => MiraLocaleManager.Get("TownOfUsMira.Role.Silencer", "Silencer");

    [ModdedNumberOption("TouOptionSilencerSecondsPerKill", 10f, 40f, 5f, MiraNumberSuffixes.Seconds)]
    public float SecondsPerKill { get; set; } = 25f;

    [ModdedNumberOption("TouOptionSilencerMinimumVotingTime", 5f, 25f, 5f, MiraNumberSuffixes.Seconds)]
    public float MinimumVotingTime { get; set; } = 10f;

    [ModdedToggleOption("TouOptionSilencerNormalVotingTimeWhenDead")]
    public bool NormalVotingTimeWhenDead { get; set; } = true;
}
