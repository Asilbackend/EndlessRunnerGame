using UnityEngine;
using World;

/// <summary>
/// Runs in the gameplay scene. Applies saved map/vehicle selection to GameManager and ChunkSpawner.
/// </summary>
public class RunBootstrap : MonoBehaviour
{
    [SerializeField] private GameConfigSO config;

    private void Awake()
    {
        if (config == null) return;

        string vehicleId = PlayerPrefsManager.GetString(PlayerPrefsKeys.SelectedVehicleId, config.defaultVehicleId);

        if (GameManager.Instance != null)
            GameManager.Instance.SetSelectedVehicleId(vehicleId);

        // TODO: when multiple decorations per map, get selected map (PlayerPrefsKeys.SelectedMapId + config.GetMapById) and call spawner.SetOverrideDecorationSource(selectedMap.decorationLayout)
    }
}
