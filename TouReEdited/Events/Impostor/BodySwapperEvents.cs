using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using TouReEdited.Buttons.Classic.Impostor.ImpostorKilling;
using TouReEdited.Options.Roles.Impostor;
using TownOfUs.Roles.Impostor;

namespace TouReEdited.Events.Impostor;

public static class BodySwapperEvents
{
    [RegisterEvent]
    public static void OnRoundStart(RoundStartEvent @event)
    {
        if (PlayerControl.LocalPlayer?.Data?.Role is not BodySwapperRole)
        {
            return;
        }

        var button = CustomButtonSingleton<BodySwapperDecoyButton>.Instance;
        if (@event.TriggeredByIntro)
        {
            button.ResetGameState();
        }
        else
        {
            button.ResetRoundState();
        }
    }

    [RegisterEvent]
    public static void OnAfterMurder(AfterMurderEvent @event)
    {
        var source = @event.Source;
        if (source?.Data?.Role is not BodySwapperRole || !source.AmOwner ||
            @event.Target == null || @event.Target.PlayerId == source.PlayerId)
        {
            return;
        }

        var button = CustomButtonSingleton<BodySwapperDecoyButton>.Instance;
        button.RegisterKill();
        if (OptionGroupSingleton<BodySwapperOptions>.Instance.SyncKillAndDecoy)
        {
            button.SetTimer(button.Cooldown);
        }
    }
}
