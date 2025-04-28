using UnityEngine;
using System.Collections.Generic;

public class CreatureManager : MonoBehaviour
{
   //follower
    public GameObject followerPrefab;
    public int initialFollowerCount = 10;
    public float followerSpawnInterval = 2f;
    public int maxFollowerCount = 50;
    
    //sign
    public GameObject signPrefab;
    public int initialSignCount = 3;
    public float signSpawnInterval = 5f;
    public int maxSignCount = 8;
    
    //spawnrange
    public float spawnRadius = 2f;
    public float minSpawnDistance = 3f;
    
    private Camera mainCamera;
    private float minX, maxX, minY, maxY;
    private float screenBoundaryOffset = 1f;
    private float followerSpawnTimer = 0f;
    private float signSpawnTimer = 0f;
    private List<GameObject> activeFollowers = new List<GameObject>();
    private List<GameObject> activeSigns = new List<GameObject>();

    void Start()
    {
        mainCamera = Camera.main;
        CalculateScreenBoundaries();
        
        SpawnInitialCreatures();
    }

    void Update()
    {
        followerSpawnTimer += Time.deltaTime;
        signSpawnTimer += Time.deltaTime;
        
        // check spawn
        if (followerSpawnTimer >= followerSpawnInterval && activeFollowers.Count < maxFollowerCount)
        {
            SpawnFollower();
            followerSpawnTimer = 0f;
        }
        
        if (signSpawnTimer >= signSpawnInterval && activeSigns.Count < maxSignCount)
        {
            SpawnSign();
            signSpawnTimer = 0f;
        }
        
        CleanupDestroyedObjects();
    }

    void SpawnInitialCreatures()
    {
        for (int i = 0; i < initialSignCount; i++)
        {
            SpawnSign();
        }
        
        for (int i = 0; i < initialFollowerCount; i++)
        {
            SpawnFollower();
        }
    }

    void SpawnFollower()
    {
        //null check
        if (followerPrefab == null)
        {
            
            return;
        }

       
        if (activeFollowers.Count >= maxFollowerCount)
        {
            return;
        }

        
        Vector3 spawnPos = GetValidSpawnPosition();
        
        GameObject newFollower = Instantiate(followerPrefab, spawnPos, Quaternion.identity);
        if (newFollower != null)
        {
            activeFollowers.Add(newFollower);
            Debug.Log($"Spawned new follower. Total count: {activeFollowers.Count}");
        }
        else
        {
            Debug.LogError("CreatureManager: Failed to instantiate follower!");
        }
    }

    void SpawnSign()
    {
        Vector3 spawnPos = GetValidSpawnPosition();
        GameObject sign = Instantiate(signPrefab, spawnPos, Quaternion.identity);
        activeSigns.Add(sign);
    }

    Vector3 GetValidSpawnPosition()
    {
        Vector3 spawnPos;
        bool isValidPosition;
        int attempts = 0;
        const int maxAttempts = 10;
        
        do
        {
            float randomX = Random.Range(minX, maxX);
            float randomY = Random.Range(minY, maxY);
            spawnPos = new Vector3(randomX, randomY, 0f);
            
            isValidPosition = true;
            foreach (GameObject follower in activeFollowers)
            {
                if (follower != null && Vector3.Distance(spawnPos, follower.transform.position) < minSpawnDistance)
                {
                    isValidPosition = false;
                    break;
                }
            }
            
            foreach (GameObject sign in activeSigns)
            {
                if (sign != null && Vector3.Distance(spawnPos, sign.transform.position) < minSpawnDistance)
                {
                    isValidPosition = false;
                    break;
                }
            }
            
            attempts++;
        } while (!isValidPosition && attempts < maxAttempts);
        
        return spawnPos;
    }

    void CleanupDestroyedObjects()
    {
        activeFollowers.RemoveAll(follower => follower == null);
        
       //clear destroyed signs
        activeSigns.RemoveAll(sign => sign == null);
    }

    void CalculateScreenBoundaries()
    {
        Vector2 screenBounds = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, mainCamera.transform.position.z));
        minX = -screenBounds.x + screenBoundaryOffset;
        maxX = screenBounds.x - screenBoundaryOffset;
        minY = -screenBounds.y + screenBoundaryOffset;
        maxY = screenBounds.y - screenBoundaryOffset;
    }

    //access messages for other objects
    public void OnFollowerDestroyed(GameObject follower)
    {
        activeFollowers.Remove(follower);
    }

    public void OnSignDestroyed(GameObject sign)
    {
        activeSigns.Remove(sign);
    }


    /*
    public int GetActiveFollowerCount()
    {
        return activeFollowers.Count;
    }

    public int GetActiveSignCount()
    {
        return activeSigns.Count;
    
}*/
}
