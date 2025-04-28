using UnityEngine;
using System.Collections.Generic;

public class TheStillOne : MonoBehaviour
{
    public enum MonsterState
    {
        Wandering,
        Chasing,
        Still,
        Ritual
    }

    public MonsterState currentState = MonsterState.Wandering;
    
    public float moveSpeed = 5f;
    public float rotationSpeed = 2f;
    public float chaseSpeed = 8f;
    //detect surrounding followers
    public float detectionRange = 20f;
    public float attackRange = 3f;
    
    //chase followers
    public Transform target;
    public List<Transform> followers = new List<Transform>();
    
    
    public float screenBoundaryOffset = 1f; 
    
    public float spawnOffset = 2f; //new stillone distance
    public int collisionThreshold = 10; // collide number to spawnnew

    public float deathDuration = 20f; 
    
    
 
    public float shrinkSpeed = 0.05f; 
    public float fadeSpeed = 0.05f; 
    
    private Vector3 randomWanderPoint;
    private float stateTimer = 0f;
    private float ritualDuration = 10f;
    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;
    private float minX, maxX, minY, maxY;
    private int collisionCount = 0;
    private bool isDying = false;
    private float deathTimer = 0f;
    private Vector3 initialScale;
    private Color initialColor;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       
        SetNewWanderPoint();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
        CalculateScreenBoundaries();
        initialScale = transform.localScale;
        initialColor = spriteRenderer.color;
    }

    void CalculateScreenBoundaries()
    {
        Vector2 screenBounds = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, mainCamera.transform.position.z));
        minX = -screenBounds.x + screenBoundaryOffset;
        maxX = screenBounds.x - screenBoundaryOffset;
        minY = -screenBounds.y + screenBoundaryOffset;
        maxY = screenBounds.y - screenBoundaryOffset;
    }

    void ClampPosition()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        transform.position = pos;
    }

    // Update is called once per frame
    void Update()
    {
        if (isDying)
        {
            HandleDeath();
            return;
        }
        
        switch (currentState)
        {
            case MonsterState.Wandering:
                HandleWandering();
                break;
            case MonsterState.Chasing:
                HandleChasing();
                break;
            case MonsterState.Still:
                HandleStill();
                break;
            case MonsterState.Ritual:
                HandleRitual();
                break;
        }
    }

    void HandleWandering()
    {
        CheckForFollowers();
        
        MoveTowards(randomWanderPoint);
        
        if (Vector3.Distance(transform.position, randomWanderPoint) < 1f)
        {
            SetNewWanderPoint();
        }
    }

    void HandleChasing()
    {
        if (target != null)
        {
            //truningf
            Vector3 direction = (target.position - transform.position).normalized;
            
            
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            // turnsmooth
            Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            
            transform.position += direction * chaseSpeed * Time.deltaTime;
            
            ClampPosition();
            
            if (Vector3.Distance(transform.position, target.position) > detectionRange)
            {
                currentState = MonsterState.Wandering;
                target = null;
            }
        }
    }

    void HandleStill()
    {
        stateTimer += Time.deltaTime;
        if (stateTimer >= 5f) 
        {
            currentState = MonsterState.Wandering;
            stateTimer = 0f;
        }
    }

    void HandleRitual()
    {
        stateTimer += Time.deltaTime;
        if (stateTimer >= ritualDuration)
        {
            currentState = MonsterState.Wandering;
            stateTimer = 0f;
        }
    }

    void CheckForFollowers()
    {
        foreach (Transform follower in followers)
        {
            if (follower != null && Vector3.Distance(transform.position, follower.position) <= detectionRange)
            {
                target = follower;
                currentState = MonsterState.Chasing;
                return;
            }
        }
    }

    void SetNewWanderPoint()
    {
        float randomX = Random.Range(minX, maxX);
        float randomY = Random.Range(minY, maxY);
        randomWanderPoint = new Vector3(randomX, randomY, 0f);
    }

    void MoveTowards(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        
        transform.position += direction * moveSpeed * Time.deltaTime;
        
        ClampPosition();
    }

    void HandleDeath()
    {
        deathTimer += Time.deltaTime;
        //淡入淡出
        float scaleMultiplier = 1f - (deathTimer / deathDuration);
        transform.localScale = initialScale * scaleMultiplier;
        
        Color currentColor = spriteRenderer.color;
        currentColor.a = 1f - (deathTimer / deathDuration);
        spriteRenderer.color = currentColor;
        
        if (deathTimer >= deathDuration)
        {
            Destroy(gameObject);
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Follower"))
        {
            collisionCount++;
            
            // 达到阈值时生成新的Still One
            if (collisionCount >= collisionThreshold)
            {
                SpawnNewStillOne();
                StartDeath();
            }
        }
    }
    
    void SpawnNewStillOne()
    {
        Vector3 spawnPosition = transform.position + Random.insideUnitSphere * spawnOffset;
        spawnPosition.z = 0f;
        
        GameObject newStillOne = Instantiate(gameObject, spawnPosition, Quaternion.identity);
        
        // collision numvber reset
        TheStillOne newStillOneScript = newStillOne.GetComponent<TheStillOne>();
        if (newStillOneScript != null)
        {
            newStillOneScript.collisionCount = 0;
            newStillOneScript.isDying = false;
            newStillOneScript.deathTimer = 0f;
            newStillOneScript.transform.localScale = initialScale;
            newStillOneScript.spriteRenderer.color = initialColor;
        }
    }
    
    void StartDeath()
    {
        isDying = true;
        deathTimer = 0f;
    }
}
