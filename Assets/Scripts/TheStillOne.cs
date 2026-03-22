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

    //move and rotation
    public float moveSpeed      = 5f;  
    public float rotationSpeed  = 2f;  
    public float chaseSpeed     = 8f;  

    public float detectionRange = 20f; 
    public Transform target; //from nearby followers

    public float screenBoundaryOffset = 1f; // 相对屏幕世界边界向内收缩的距离，防止贴边

    // ------------------------------------------------------------------------
    // 碰撞生成
    // ------------------------------------------------------------------------
    public float spawnOffset        = 2f;  // 新生成个体相对本物体位置的最大随机偏移半径
    public int   collisionThreshold = 10;  // 与 Follower 碰撞累计次数达到该值时生成新个体并开始死亡

    // ------------------------------------------------------------------------
    // 死亡表现
    // ------------------------------------------------------------------------
    public float deathDuration = 20f;    // 死亡动画/淡出持续时长（秒），超时后销毁物体

    // ------------------------------------------------------------------------
    // 运行时缓存（非 Inspector 主要调参）
    // ------------------------------------------------------------------------
    private Vector3        randomWanderPoint;                   // 当前游荡随机目标点（世界坐标）
    private float          stateTimer       = 0f;                 // 静止/仪式状态下的状态计时（秒）
    private float          ritualDuration   = 10f;                // 仪式状态持续时长（秒）
    private SpriteRenderer spriteRenderer;                        // 缓存 SpriteRenderer，用于死亡淡出
    private Camera         mainCamera;                            // 主摄像机引用，用于计算屏幕世界边界
    private float          minX, maxX, minY, maxY;               // 世界空间可移动矩形边界
    private int            collisionCount   = 0;                 // 与 Follower 的碰撞累计次数
    private bool           isDying          = false;             // 是否处于死亡流程
    private float          deathTimer       = 0f;                 // 死亡流程已进行时间（秒）
    private Vector3        initialScale;                          // 开始死亡前记录的初始缩放
    private Color          initialColor;                          // 开始死亡前记录的初始颜色（含 alpha）
    private float          wanderingTimer    = 0f;
    private float          wanderingDuration = 0f;
    private float          stillTimer        = 0f;
    private float          stillDuration     = 0f;
    private bool           spawnedOnDeath    = false;
    private readonly HashSet<Transform> nearbyFollowers = new HashSet<Transform>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetNewWanderPoint();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
        CalculateScreenBoundaries();
        initialScale = transform.localScale;
        initialColor = spriteRenderer.color;
        SetWanderingDuration();
        SetStillDuration();
    }

    void CalculateScreenBoundaries()
    {
        PhysicsHelper.GetScreenBounds(mainCamera, out minX, out maxX, out minY, out maxY, screenBoundaryOffset);
    }

    void WrapScreenPosition()
    {
        transform.position = PhysicsHelper.WrapToScreenBounds(transform.position, mainCamera, screenBoundaryOffset);
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
        if (TryLockFollowerTarget())
        {
            currentState = MonsterState.Chasing;
            return;
        }
        
        MoveTowards(randomWanderPoint);

        wanderingTimer += Time.deltaTime;
        if (Vector3.Distance(transform.position, randomWanderPoint) < 1f || wanderingTimer >= wanderingDuration)
        {
            currentState = MonsterState.Still;
            stillTimer = 0f;
            SetStillDuration();
        }
    }

    void HandleChasing()
    {
        if (target == null)
        {
            currentState = MonsterState.Wandering;
            wanderingTimer = 0f;
            SetWanderingDuration();
            SetNewWanderPoint();
            return;
        }

        Vector3 direction = (target.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        transform.position += direction * chaseSpeed * Time.deltaTime;
            WrapScreenPosition();

        if (Vector3.Distance(transform.position, target.position) > detectionRange * 1.2f)
        {
            target = null;
            currentState = MonsterState.Wandering;
            wanderingTimer = 0f;
            SetWanderingDuration();
            SetNewWanderPoint();
        }
    }

    void HandleStill()
    {
        stillTimer += Time.deltaTime;
        if (stillTimer >= stillDuration)
        {
            currentState = MonsterState.Wandering;
            wanderingTimer = 0f;
            SetWanderingDuration();
            SetNewWanderPoint();
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

    bool TryLockFollowerTarget()
    {
        float bestDistance = float.MaxValue;
        Transform bestTarget = null;

        foreach (Transform follower in nearbyFollowers)
        {
            if (follower == null)
                continue;

            float distance = Vector3.Distance(transform.position, follower.position);
            if (distance <= detectionRange && distance < bestDistance)
            {
                bestDistance = distance;
                bestTarget = follower;
            }
        }

        if (bestTarget != null)
        {
            target = bestTarget;
            return true;
        }
        return false;
    }

    void SetNewWanderPoint()
    {
        float randomX = Random.Range(minX, maxX);
        float randomY = Random.Range(minY, maxY);
        randomWanderPoint = new Vector3(randomX, randomY, 0f);
    }

    //after setting new wander point, move towards it
    void MoveTowards(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        
        transform.position += direction * moveSpeed * Time.deltaTime;
        
            WrapScreenPosition();
    }
    //fade out shirink and destroy
    void HandleDeath()
    {
        deathTimer += Time.deltaTime;

        float scaleMultiplier = 1f - (deathTimer / deathDuration);
        transform.localScale = initialScale * scaleMultiplier;
        
        Color currentColor = spriteRenderer.color;
        currentColor.a = 1f - (deathTimer / deathDuration);
        spriteRenderer.color = currentColor;
        
        if (deathTimer >= deathDuration)
        {
            if (!spawnedOnDeath)
            {
                SpawnTwoStillOnes();
                spawnedOnDeath = true;
            }
            Destroy(gameObject);
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDying)
            return;

        Follower follower = collision.gameObject.GetComponentInParent<Follower>();
        if (follower != null)
        {
            collisionCount++;
            DestroyFollower(follower.gameObject);

            if (collisionCount >= collisionThreshold)
                StartDeath();
        }
    }

    void DestroyFollower(GameObject followerObject)
    {
        if (followerObject == null)
            return;

        Follower follower = followerObject.GetComponent<Follower>();
        if (follower != null)
        {
            CreatureManager manager = FindObjectOfType<CreatureManager>();
            if (manager != null)
            {
                manager.OnFollowerDestroyed(followerObject);
            }
        }
        Destroy(followerObject);
    }

    void SpawnTwoStillOnes()
    {
        SpawnNewStillOne();
        SpawnNewStillOne();
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
            newStillOneScript.spawnedOnDeath = false;
            newStillOneScript.transform.localScale = initialScale;
            // Instantiate 后新物体的 Start 尚未执行，spriteRenderer 缓存仍为 null，不能直接访问
            SpriteRenderer newSr = newStillOne.GetComponent<SpriteRenderer>();
            if (newSr != null)
                newSr.color = initialColor;
            newStillOneScript.currentState = MonsterState.Wandering;
            newStillOneScript.wanderingTimer = 0f;
            newStillOneScript.SetWanderingDuration();
            newStillOneScript.SetNewWanderPoint();
        }
    }
    
    void StartDeath()
    {
        isDying = true;
        deathTimer = 0f;
        spawnedOnDeath = false;
        target = null;
        nearbyFollowers.Clear();
    }

    void SetWanderingDuration()
    {
        wanderingDuration = Random.Range(1.5f, 3.5f);
    }

    void SetStillDuration()
    {
        stillDuration = Random.Range(1f, 2.5f);
    }

   //add follower to nearby followers if in detection range
    public void OnDetectionTriggerEnter2D(Collider2D other)
    {
        Follower follower = other.GetComponentInParent<Follower>();
        if (follower != null)
            nearbyFollowers.Add(follower.transform);
    }
  // remove follower from nearby followers if out of detection range
    public void OnDetectionTriggerExit2D(Collider2D other)
    {
        Follower follower = other.GetComponentInParent<Follower>();
        if (follower != null)
        {
            nearbyFollowers.Remove(follower.transform);
            if (target == follower.transform)
                target = null;
        }
    }
}
