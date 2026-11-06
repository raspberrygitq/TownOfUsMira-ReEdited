using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using TouReEdited.Assets;
using TouReEdited.Modules;
using TouReEdited.Options.Roles.Impostor;
using UnityEngine;

namespace TownOfUs.Roles.Impostor;

public sealed class BodySwapperRole(IntPtr cppPtr)
    : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public string IdPart => "BodySwapper";
    public string RoleMedDescriptionLocale => $"TownOfUsMira.Role.{IdPart}.TabDescription";
    public DoomableType DoomHintType => DoomableType.Death;
    public Color RoleColor => TownOfUsColors.Impostor;
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public RoleAlignment RoleAlignment => RoleAlignment.ImpostorKilling;
    public CustomRoleConfiguration Configuration => new(this)
    {
        IconTmp = TmpSpriteUtils.CreateSpriteAsset(ReEditedRoleIcons.BodySwapper.LoadAsset(),
            $"TownOfUsMira.Role.{IdPart}", 1.45f),
        Icon = ReEditedRoleIcons.BodySwapper,
        OptionsScreenshot = TouBanners.ImpostorRoleBanner,
        CanUseVent = OptionGroupSingleton<BodySwapperOptions>.Instance.CanVent,
        MaxRoleCount = 1
    };

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        // Read the decoy's swallow frames now rather than when the first decoy is placed.
        BodySwapperSwallowAnimation.Preload();
    }

    public string GetAdvancedDescription() =>
        MiraLocaleManager.Get($"TownOfUsMira.Role.{IdPart}.WikiDescription") +
        MiscUtils.AppendOptionsText(GetType());

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(MiraLocaleManager.Get($"TownOfUsMira.Role.{IdPart}Decoy", "Decoy"),
            MiraLocaleManager.Get($"TownOfUsMira.Role.{IdPart}Decoy.WikiDescription"),
            ReEditedRoleIcons.BodySwapper)
    ];
}
