using MiraAPI.GameOptions;
using MiraAPI.Hud;
using TouReEdited.Assets;
using TouReEdited.Modules;
using TouReEdited.Options.Roles.Impostor;
using TownOfUs;
using TownOfUs.Buttons;
using TownOfUs.Roles.Impostor;
using UnityEngine;

namespace TouReEdited.Buttons.Classic.Impostor.ImpostorKilling;

public sealed class BodySwapperDecoyButton : TownOfUsRoleButton<BodySwapperRole>
{
    private int placedThisRound;
    private int killsTowardsExtra;

    public override string Name => MiraLocaleManager.Get("TouReEdited.Role.BodySwapperDecoy", "Decoy");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TownOfUsColors.Impostor;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<BodySwapperOptions>.Instance.DecoyCooldown + MapCooldown, 5f, 120f);
    public override int MaxUses => (int)OptionGroupSingleton<BodySwapperOptions>.Instance.InitialUses;
    public override bool ZeroIsInfinite { get; set; } = true;
    public override bool UsableFirstRound => OptionGroupSingleton<BodySwapperOptions>.Instance.CanPlaceFirstRound;
    public override LoadableAsset<Sprite> Sprite => ReEditedRoleIcons.BodySwapper;

    public override bool CanUse() =>
        base.CanUse() && placedThisRound < OptionGroupSingleton<BodySwapperOptions>.Instance.MaxFakeBodiesPerRound &&
        !PlayerControl.LocalPlayer.inVent && !Minigame.Instance;

    public override void ClickHandler()
    {
        if (CanClick())
        {
            OnClick();
        }
    }

    protected override void OnClick()
    {
        var menu = CustomPlayerMenu.Create();
        if (menu == null)
        {
            Timer = 0.01f;
            return;
        }

        menu.transform.FindChild("PhoneUI").GetChild(0).GetComponent<SpriteRenderer>().material =
            PlayerControl.LocalPlayer.cosmetics.currentBodySprite.BodySprite.material;
        menu.transform.FindChild("PhoneUI").GetChild(1).GetComponent<SpriteRenderer>().material =
            PlayerControl.LocalPlayer.cosmetics.currentBodySprite.BodySprite.material;

        menu.Begin(player => player != null && !player.HasDied(), selected =>
        {
            menu.ForceClose();
            if (selected == null || PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.HasDied() ||
                MeetingHud.Instance || placedThisRound >= OptionGroupSingleton<BodySwapperOptions>.Instance.MaxFakeBodiesPerRound ||
                LimitedUses && MaxUses > 0 && UsesLeft <= 0)
            {
                Timer = 0.01f;
                return;
            }

            var local = PlayerControl.LocalPlayer;
            BodySwapperDecoySystem.RpcSpawn(local, selected, local.transform.position);
            placedThisRound++;
            Timer = Cooldown;
            if (LimitedUses && (!ZeroIsInfinite || MaxUses != 0))
            {
                SetUses(UsesLeft - 1);
            }

            if (OptionGroupSingleton<BodySwapperOptions>.Instance.SyncKillAndDecoy)
            {
                local.SetKillTimer(local.GetKillCooldown());
            }
        });
    }

    public void ResetRoundState() => placedThisRound = 0;

    public void ResetGameState()
    {
        placedThisRound = 0;
        killsTowardsExtra = 0;
    }

    public void RegisterKill()
    {
        var threshold = (int)OptionGroupSingleton<BodySwapperOptions>.Instance.KillsForAdditionalDecoy;
        if (threshold <= 0 || MaxUses == 0)
        {
            return;
        }

        if (++killsTowardsExtra >= threshold)
        {
            killsTowardsExtra = 0;
            SetUses(UsesLeft + 1);
        }
    }
}
