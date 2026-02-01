using UnityEngine;
using Player;

[CreateAssetMenu(fileName = "GameConfigSO", menuName = "Scriptable Objects/Game Config", order = 0)]
public class GameConfigSO : ScriptableObject
{
    [Header("Content")]
    public MapDefinitionSO[] maps;
    public PlayableObjectsSO playableObjects;

    [Header("Defaults")]
    public string defaultMapId;
    public string defaultVehicleId;
}
