using UnityEngine;
using System.Collections.Generic;

public class SkyboxRotator : MonoBehaviour
{
    [SerializeField] private List<GameObject> cloudPrefabs = new List<GameObject>();
    [SerializeField] private int poolSize = 10;
    [SerializeField] private float minMoveSpeed = 2f;
    [SerializeField] private float maxMoveSpeed = 6f;
    [SerializeField] private float rightBoundary = 20f;
    [SerializeField] private float leftBoundary = -20f;
    [SerializeField] private float minY = -5f;
    [SerializeField] private float maxY = 5f;
    [SerializeField] private float minZ = -5f;
    [SerializeField] private float maxZ = 5f;
    [SerializeField] private float minSpawnTime = 2f;
    [SerializeField] private float maxSpawnTime = 5f;
    [SerializeField] private int maxActiveClouds = 2;

    private List<GameObject> cloudPool = new List<GameObject>();
    private List<GameObject> activeClouds = new List<GameObject>();
    private Transform poolContainer;
    private float nextSpawnTime;
    private float spawnTimer;

    void Start()
    {
        // Create pool container
        poolContainer = new GameObject("CloudPool").transform;
        poolContainer.SetParent(transform);

        // Instantiate clouds into pool
        if (cloudPrefabs != null && cloudPrefabs.Count > 0)
        {
            for (int i = 0; i < poolSize; i++)
            {
                GameObject randomPrefab = cloudPrefabs[Random.Range(0, cloudPrefabs.Count)];
                GameObject cloud = Instantiate(randomPrefab, poolContainer);
                cloud.SetActive(false);
                cloudPool.Add(cloud);
            }
        }

        // Set first random spawn time
        nextSpawnTime = Random.Range(minSpawnTime, maxSpawnTime);
        ActivateCloud();
    }

    void Update()
    {
        if (cloudPool == null || cloudPool.Count == 0) return;

        // Update spawn timer
        spawnTimer += Time.deltaTime;

        // Spawn new cloud if we have less than max active clouds and timer is ready
        if (activeClouds.Count < maxActiveClouds && spawnTimer >= nextSpawnTime)
        {
            ActivateCloud();
            spawnTimer = 0f;
            nextSpawnTime = Random.Range(minSpawnTime, maxSpawnTime);
        }

        // Move active clouds and handle recycling
        for (int i = activeClouds.Count - 1; i >= 0; i--)
        {
            GameObject cloud = activeClouds[i];
            if (cloud == null || !cloud.activeSelf)
            {
                activeClouds.RemoveAt(i);
                continue;
            }

            // Move cloud from right to left
            var moveSpeed = Random.Range(minMoveSpeed, maxMoveSpeed);
            cloud.transform.position += Vector3.left * moveSpeed * Time.deltaTime;

            // If cloud goes off screen on the left, recycle it
            if (cloud.transform.position.x < leftBoundary)
            {
                RecycleCloud(cloud);
                activeClouds.RemoveAt(i);
            }
        }
    }

    void ActivateCloud()
    {
        // Find an available cloud from the pool
        GameObject cloudToActivate = null;
        foreach (GameObject cloud in cloudPool)
        {
            if (cloud != null && !cloud.activeSelf && !activeClouds.Contains(cloud))
            {
                cloudToActivate = cloud;
                break;
            }
        }

        if (cloudToActivate != null)
        {
            // Set random Y and Z position
            float randomY = Random.Range(minY, maxY);
            float randomZ = Random.Range(minZ, maxZ);
            cloudToActivate.transform.position = new Vector3(rightBoundary, randomY, randomZ);

            cloudToActivate.SetActive(true);
            activeClouds.Add(cloudToActivate);
        }
    }

    void RecycleCloud(GameObject cloud)
    {
        if (cloud != null)
        {
            cloud.SetActive(false);
            cloud.transform.SetParent(poolContainer);
        }
    }
}
