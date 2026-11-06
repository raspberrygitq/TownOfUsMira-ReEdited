using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TownOfUs.Roles.Impostor;

namespace TouReEdited.Options.Roles.Impostor;

public sealed class BodySwapperOptions : AbstractRoleOptionGroup<BodySwapperRole>
{
    public override string GroupName => MiraLocaleManager.Get("TownOfUsMira.Role.BodySwapper", "Body Swapper");

    [ModdedNumberOption("TouReEditedOptionBodySwapperDecoyCooldown", 5f, 120f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float DecoyCooldown { get; set; } = 30f;

    [ModdedNumberOption("TouReEditedOptionBodySwapperInitialUses", 0f, 10f, 1f, MiraNumberSuffixes.None, "0", zeroInfinity: true)]
    public float InitialUses { get; set; } = 2f;

    [ModdedNumberOption("TouReEditedOptionBodySwapperMaxPerRound", 1f, 5f, 1f, MiraNumberSuffixes.None)]
    public float MaxFakeBodiesPerRound { get; set; } = 1f;

    [ModdedNumberOption("TouReEditedOptionBodySwapperKillsForUse", 0f, 10f, 1f, MiraNumberSuffixes.None)]
    public float KillsForAdditionalDecoy { get; set; } = 2f;

    [ModdedToggleOption("TouReEditedOptionBodySwapperCanVent")]
    public bool CanVent { get; set; } = true;

    [ModdedToggleOption("TouReEditedOptionBodySwapperFirstRound")]
    public bool CanPlaceFirstRound { get; set; }

    [ModdedToggleOption("TouReEditedOptionBodySwapperSyncKill")]
    public bool SyncKillAndDecoy { get; set; } = true;

    [ModdedNumberOption("TouReEditedOptionBodySwapperDuration", 5f, 120f, 5f, MiraNumberSuffixes.Seconds)]
    public float FakeBodyDuration { get; set; } = 45f;

    [ModdedToggleOption("TouReEditedOptionBodySwapperPierceShield")]
    public bool TrapPiercesShields { get; set; }
}
