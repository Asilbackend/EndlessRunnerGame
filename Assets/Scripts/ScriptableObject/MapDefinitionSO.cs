using UnityEngine;
using World;

[CreateAssetMenu(fileName = "MapDefinition", menuName = "World/Map Definition", order = 2)]
public class MapDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;

    [Header("Menu Visuals")]
    public Sprite thumbnail;
    public bool lockedByDefault;

    [Header("Gameplay")]
    [Tooltip("Scene to load for this map (e.g. MainGame)")]
    public string sceneName;
    [Tooltip("TODO: when multiple decorations per map, wire up in RunBootstrap + ChunkSpawner. Only decoration selection uses this; chunks/road unchanged.")]
    public ChunkLayoutSO decorationLayout;
}
