using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using Reactor.Utilities;
using System.Collections;
using TouReEdited.Options.Roles.Impostor;
using TownOfUs.Modules.Components;
using TownOfUs.Networking;
using TownOfUs.Roles.Impostor;
using UnityEngine;

namespace TouReEdited.Modules;

// A decoy is a visual corpse only. It must not enter the real DeadBody registry:
// revive, rewind and corpse-cleaning abilities should never operate on a living player.
public static class BodySwapperDecoySystem
{
    private sealed class Decoy
    {
        public int Id;
        public byte OwnerId;
        public GameObject Visual = null!;
        public Vector2 Position;
        public float ExpiresAt;
        public int AppearanceColorId;
    }

    private static readonly List<Decoy> Active = [];
    private static readonly Dictionary<byte, int> PlacementsByCreator = [];
    private static int nextId;

    public static void Clear()
    {
        foreach (var decoy in Active)
        {
            if (decoy.Visual)
            {
                UnityEngine.Object.Destroy(decoy.Visual);
            }
        }
        Active.Clear();
        PlacementsByCreator.Clear();
        nextId = 0;
    }

    public static void Tick()
    {
        for (var i = Active.Count - 1; i >= 0; i--)
        {
            var decoy = Active[i];
            if (decoy.Visual && Time.time < decoy.ExpiresAt)
            {
                continue;
            }

            if (decoy.Visual)
            {
                UnityEngine.Object.Destroy(decoy.Visual);
            }
            Active.RemoveAt(i);
        }
    }

    public static bool TryClosest(Vector2 pos, out int decoyId, out float distance)
    {
        decoyId = -1;
        distance = float.MaxValue;
        foreach (var decoy in Active)
        {
            if (!decoy.Visual)
            {
                continue;
            }

            var current = Vector2.Distance(pos, decoy.Position);
            if (current >= distance)
            {
                continue;
            }

            decoyId = decoy.Id;
            distance = current;
        }
        return decoyId >= 0;
    }

    public static void RpcSpawn(PlayerControl creator, PlayerControl appearance, Vector2 position)
    {
        // Each owner allocates IDs independently. Include the owner so decoys from
        // different clients cannot collide when they place their first body.
        var id = (creator.PlayerId << 24) | (++nextId & 0x00FFFFFF);
        RpcSpawnDecoy(creator, appearance, id, position);
    }

    [MethodRpc((uint)TouReEditedRpc.BodySwapperSpawn)]
    private static void RpcSpawnDecoy(PlayerControl creator, PlayerControl appearance, int id, Vector2 position)
    {
        if (LobbyBehaviour.Instance || MeetingHud.Instance || creator?.Data?.Role is not BodySwapperRole ||
            creator.Data.IsDead || appearance?.Data == null || appearance.Data.IsDead ||
            Vector2.Distance(creator.GetTruePosition(), position) > 3f ||
            GameManager.Instance?.deadBodyPrefab == null || GameManager.Instance.deadBodyPrefab.Length == 0 ||
            Active.Any(d => d.Id == id && d.OwnerId == creator.PlayerId))
        {
            return;
        }

        var options = OptionGroupSingleton<BodySwapperOptions>.Instance;
        if (!options.CanPlaceFirstRound && HudManagerHelper.Instance?.CurrentRound == 1 &&
            !TutorialManager.InstanceExists)
        {
            return;
        }

        var placed = PlacementsByCreator.TryGetValue(creator.PlayerId, out var count) ? count : 0;
        if (placed >= (int)options.MaxFakeBodiesPerRound)
        {
            return;
        }

        var template = GameManager.Instance.deadBodyPrefab[0];
        var body = UnityEngine.Object.Instantiate(template);
        if (!body)
        {
            return;
        }

        body.transform.position = new Vector3(position.x, position.y, position.y / 1000f);
        foreach (var renderer in body.bodyRenderers)
        {
            appearance.SetPlayerMaterialColors(renderer);
        }
        if (body.bloodSplatter)
        {
            appearance.SetPlayerMaterialColors(body.bloodSplatter);
        }

        foreach (var collider in body.GetComponentsInChildren<Collider2D>())
        {
            collider.enabled = false;
        }

        // Destroy only the DeadBody component; keep its sprites as a decoy.
        var visual = body.gameObject;
        UnityEngine.Object.Destroy(body);
        Active.Add(new Decoy
        {
            Id = id,
            OwnerId = creator.PlayerId,
            Visual = visual,
            Position = position,
            ExpiresAt = Time.time + options.FakeBodyDuration,
            AppearanceColorId = appearance.Data.DefaultOutfit.ColorId
        });
        PlacementsByCreator[creator.PlayerId] = placed + 1;

        // Repaint the swallow frames in this decoy's colours now, so springing the trap does not stall.
        BodySwapperSwallowAnimation.PrepareColour(appearance.Data.DefaultOutfit.ColorId);
    }

    [MethodRpc((uint)TouReEditedRpc.BodySwapperSpring)]
    public static void RpcSpringDecoy(PlayerControl clicker, int decoyId)
    {
        if (LobbyBehaviour.Instance || MeetingHud.Instance || !AmongUsClient.Instance.AmHost ||
            clicker?.Data == null || clicker.Data.IsDead || clicker.IsImpostorAligned())
        {
            return;
        }

        var decoy = Active.FirstOrDefault(d => d.Id == decoyId && d.Visual);
        if (decoy == null || Vector2.Distance(clicker.GetTruePosition(), decoy.Position) >
            clicker.MaxReportDistance + 0.6f)
        {
            return;
        }

        var owner = MiscUtils.PlayerById(decoy.OwnerId);
        if (owner?.Data?.Role is not BodySwapperRole || owner.Data.IsDead)
        {
            return;
        }

        RpcConsumeDecoy(PlayerControl.LocalPlayer, decoy.Id, clicker.PlayerId);
    }

    [MethodRpc((uint)TouReEditedRpc.BodySwapperConsume)]
    private static void RpcConsumeDecoy(PlayerControl host, int decoyId, byte victimId)
    {
        if (host == null || !host.IsHost())
        {
            return;
        }

        var decoy = Active.FirstOrDefault(d => d.Id == decoyId && d.Visual);
        if (decoy == null)
        {
            return;
        }

        Active.Remove(decoy);
        var victim = MiscUtils.PlayerById(victimId);
        if (victim?.Data == null || victim.Data.IsDead)
        {
            UnityEngine.Object.Destroy(decoy.Visual);
            return;
        }

        // Every client plays the eat animation; the host performs the kill when it ends.
        Coroutines.Start(CoEatSequence(decoy, victim));
    }

    private static IEnumerator CoEatSequence(Decoy decoy, PlayerControl victim)
    {
        // The victim is pulled to where the drawn maw closes, left of the corpse.
        var feet = BodySwapperSwallowAnimation.VictimFeetFor(decoy.Visual);
        var standPos = feet - (victim.GetTruePosition() - (Vector2)victim.transform.position);

        // The victim is pulled to the decoy and frozen (only their own client moves them).
        if (victim.AmOwner)
        {
            victim.moveable = false;
            victim.MyPhysics?.ResetMoveState();
            victim.NetTransform.RpcSnapTo(standPos);
        }

        // Give the snap a frame to land.
        yield return null;

        BodySwapperSwallowAnimation? animation = null;
        try
        {
            animation = new BodySwapperSwallowAnimation(decoy.Visual, victim, decoy.AppearanceColorId);
        }
        catch (Exception ex)
        {
            Error($"Body Swapper: swallow animation failed to start: {ex}");
        }

        if (animation != null)
        {
            while (!MeetingHud.Instance && victim != null && victim.Data != null && !victim.Data.IsDead)
            {
                bool running;
                try
                {
                    running = animation.Step(Time.deltaTime, () => { });
                }
                catch (Exception ex)
                {
                    Error($"Body Swapper: swallow animation failed: {ex}");
                    break;
                }

                if (!running)
                {
                    break;
                }

                yield return null;
            }

            animation.Dispose();
        }
        else
        {
            yield return new WaitForSeconds(BodySwapperSwallowAnimation.SwallowTime);
        }

        if (decoy.Visual)
        {
            UnityEngine.Object.Destroy(decoy.Visual);
        }

        if (victim == null)
        {
            yield break;
        }

        victim.Visible = true;
        if (victim.AmOwner)
        {
            victim.moveable = true;
        }

        // A meeting interrupted the animation: nobody dies.
        if (MeetingHud.Instance || !AmongUsClient.Instance.AmHost || victim.Data == null || victim.Data.IsDead)
        {
            yield break;
        }

        var owner = MiscUtils.PlayerById(decoy.OwnerId);
        if (owner?.Data?.Role is not BodySwapperRole)
        {
            yield break;
        }

        owner.RpcSpecialMurder(victim, isIndirect: true,
            ignoreShield: OptionGroupSingleton<BodySwapperOptions>.Instance.TrapPiercesShields,
            teleportMurderer: false, showKillAnim: false, causeOfDeath: "BodySwapperEaten");
    }
}
