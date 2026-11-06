using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.Roles;
using TownOfUs.Roles;
using TownOfUs;
using UnityEngine;
using TouReEdited.Assets;

namespace TouReEdited.Roles.Impostor.ImpostorPower;

public sealed class SilencerRole(IntPtr cppPtr)
    : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public string IdPart => "Silencer";
    public string RoleMedDescriptionLocale => $"TownOfUsMira.Role.{IdPart}.TabDescription";
    public DoomableType DoomHintType => DoomableType.Fearmonger;
    public Color RoleColor => TownOfUsColors.Impostor;
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public RoleAlignment RoleAlignment => RoleAlignment.ImpostorPower;

    public CustomRoleConfiguration Configuration => new(this)
    {
        IconTmp = TmpSpriteUtils.CreateSpriteAsset(ReEditedRoleIcons.Silencer.LoadAsset(),
            "TouMira.Role.Impostor.Silencer", 1.45f),
        Icon = ReEditedRoleIcons.Silencer,
        IntroSound = TouAudio.GlitchSound,
        OptionsScreenshot = TouBanners.ImpostorRoleBanner,
        MaxRoleCount = 1
    };

    public string GetAdvancedDescription() =>
        MiraLocaleManager.Get($"TownOfUsMira.Role.{IdPart}.WikiDescription") +
        MiscUtils.AppendOptionsText(GetType());

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities { get; } = [];
}
