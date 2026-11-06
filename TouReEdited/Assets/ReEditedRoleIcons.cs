using UnityEngine;

namespace TouReEdited.Assets;

public static class ReEditedRoleIcons
{
    // THIS FILE SHOULD ONLY HOLD ROLE ICONS

    private const string ShortPath = "TouReEdited.Resources";

    // Neutrals
    public static LoadableAsset<Sprite> BodySwapper { get; } =
        new LoadableResourceAsset($"{ShortPath}.RoleIcons.BodySwapper.png", 200f);
    public static LoadableAsset<Sprite> Silencer { get; } =
        new LoadableResourceAsset($"{ShortPath}.RoleIcons.Silencer.png", 200f);
}