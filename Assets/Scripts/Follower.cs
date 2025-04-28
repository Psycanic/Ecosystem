using UnityEngine;
using System.Collections.Generic;

public class Follower : MonoBehaviour
{
    public enum FollowerState
    {
        Idle,
        Moving,
        Dancing,
        Fleeing
    }

   
    public float moveSpeed = 8f;
    public float idleSpeed = 2f;
    public float rotationSpeed = 5f;
    public float detectionRange = 10f;
    public float danceSpeed = 3f;
    public float fleeSpeed = 12f;
    public float spawnCooldown = 7f; // 生成后的冷却时间
    public Vector2 pivotOffset = Vector2.zero; // 添加Pivot偏移量设置
    
    public float minMoveTime = 0.5f;
    public float maxMoveTime = 2f;
    public float minIdleTime = 0.3f;
    public float maxIdleTime = 1.5f;
    

    //dance as a circle 
    public float initialDanceRadius = 5f;
    //slowly decrease in radius
    public float minDanceRadius = 1f;
    public float danceRadiusDecreaseRate = 0.1f;
    //chances to join others' dancing action
    public float joinDanceChance = 0.3f;
    public float danceEscapeChance = 0.1f; // chance to flee when see StillOne
    public float danceAmplitude = 0.5f; // dance amount
    
   //spawn setting
    public int minSpawnCount = 1;
    public int maxSpawnCount = 3;
    
    
    public float collisionForce = 5f;
       
    private FollowerState currentState = FollowerState.Idle;
    private Vector3 targetPoint;
    private float stateTimer = 0f;
    private float currentDanceRadius;
    private float danceAngle = 0f;
    private float personalDanceAngle = 0f; // 个人舞蹈角度
    public Transform currentSign;
    private Transform theStillOne;
    private List<Transform> otherFollowers = new List<Transform>();
    private Camera mainCamera;
    private float minX, maxX, minY, maxY;
    private float screenBoundaryOffset = 1f;
    private CreatureManager creatureManager;
    private bool isDancing = false;
    private Rigidbody2D rb;
    private Collider2D followerCollider;
    private float spawnTimer = 0f; // 生成后的计时器
    private bool isInCooldown = true; // 是否在冷却中

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainCamera = Camera.main;
        CalculateScreenBoundaries();
        SetNewRandomPoint();
        FindTheStillOne();
        creatureManager = FindObjectOfType<CreatureManager>();
        
        // 获取组件
        rb = GetComponent<Rigidbody2D>();
        followerCollider = GetComponent<Collider2D>();
        
        // 初始化冷却状态
        spawnTimer = 0f;
        isInCooldown = true;
    }

    void FindTheStillOne()
    {
        theStillOne = GameObject.FindGameObjectWithTag("TheStillOne")?.transform;
    }

    void CalculateScreenBoundaries()
    {
        PhysicsHelper.GetScreenBounds(mainCamera, out minX, out maxX, out minY, out maxY, screenBoundaryOffset);
    }

    // Update is called once per frame
    void Update()
    {
        // 更新冷却计时器
        if (isInCooldown)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnCooldown)
            {
                isInCooldown = false;
            }
        }

        switch (currentState)
        {
            case FollowerState.Idle:
                HandleIdle();
                break;
            case FollowerState.Moving:
                HandleMoving();
                break;
            case FollowerState.Dancing:
                HandleDancing();
                break;
            case FollowerState.Fleeing:
                HandleFleeing();
                break;
        }

        ClampPosition();
    }

    void HandleIdle()
    {
        stateTimer += Time.deltaTime;
        
        // 在静止状态下检查周围环境
        if (stateTimer >= Random.Range(minIdleTime, maxIdleTime))
        {
            CheckEnvironment();
            if (currentState == FollowerState.Idle) // 如果没有进入其他状态
            {
                SetNewRandomPoint();
                currentState = FollowerState.Moving;
                stateTimer = 0f;
            }
        }
    }

    void HandleMoving()
    {
        MoveTowards(targetPoint);
        
        if (Vector3.Distance(transform.position, targetPoint) < 0.1f)
        {
            currentState = FollowerState.Idle;
            stateTimer = 0f;
        }
    }

    void HandleDancing()
    {
        if (currentSign == null)
        {
            currentState = FollowerState.Idle;
            isDancing = false;
            return;
        }

        // 检查Sign是否正在消失
        Sign sign = currentSign.GetComponent<Sign>();
        if (sign != null && sign.isDisappearing)
        {
            currentState = FollowerState.Idle;
            isDancing = false;
            currentSign = null;
            return;
        }

        // 检查是否看到The Still One
        if (theStillOne != null && Vector3.Distance(transform.position, theStillOne.position) <= detectionRange)
        {
            if (Random.value < danceEscapeChance)
            {
                currentState = FollowerState.Fleeing;
                isDancing = false;
                return;
            }
        }

        // 更新舞蹈半径
        currentDanceRadius = Mathf.Max(minDanceRadius, currentDanceRadius - danceRadiusDecreaseRate * Time.deltaTime);
        
        // 计算舞蹈位置（围绕Sign做圆周运动）
        danceAngle += danceSpeed * Time.deltaTime;
        Vector3 targetPosition = currentSign.position + new Vector3(
            Mathf.Cos(danceAngle) * currentDanceRadius,
            Mathf.Sin(danceAngle) * currentDanceRadius,
            0f
        );
        
        // 应用Pivot偏移量
        targetPosition += new Vector3(pivotOffset.x, pivotOffset.y, 0f);
        
        // 移动到目标位置
        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
        
        // 更新朝向
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, angle), rotationSpeed * Time.deltaTime);
        
        // 检查是否碰到sign
        if (Vector3.Distance(transform.position, currentSign.position) < 0.5f)
        {
            HandleSignCollision();
        }
    }

    void HandleFleeing()
    {
        if (theStillOne == null)
        {
            currentState = FollowerState.Idle;
            return;
        }

        // 计算远离The Still One的方向
        Vector3 fleeDirection = PhysicsHelper.GetFleeDirection(transform.position, theStillOne.position);
        
        // 应用力
        if (rb != null)
        {
            PhysicsHelper.ApplyForce(rb, fleeDirection, fleeSpeed);
        }
        else
        {
            transform.position += fleeDirection * fleeSpeed * Time.deltaTime;
        }
        
        // 检查是否已经逃离足够远
        if (PhysicsHelper.IsInRange(transform, theStillOne, detectionRange * 1.5f))
        {
            currentState = FollowerState.Idle;
            stateTimer = 0f;
        }
    }

    void CheckEnvironment()
    {
        // 如果在冷却中，只允许Idle和Moving状态
        if (isInCooldown)
        {
            return;
        }

        // 检查是否有The Still One在范围内
        if (theStillOne != null && PhysicsHelper.IsInRange(transform, theStillOne, detectionRange))
        {
            currentState = FollowerState.Fleeing;
            return;
        }

        // 检查是否有Signs在范围内
        const string SIGN_TAG = "Sign";
        Collider2D[] signs = PhysicsHelper.GetObjectsInRange(transform.position, detectionRange, SIGN_TAG);
        
        if (signs == null || signs.Length == 0)
        {
            return;
        }

        foreach (Collider2D signCollider in signs)
        {
            if (signCollider == null) continue;

            // 检查这个Sign是否已经有Follower在跳舞
            Follower[] nearbyFollowers = PhysicsHelper.GetComponentsInRange<Follower>(signCollider.transform.position, detectionRange);
            bool hasDancingFollower = false;
            
            foreach (Follower otherFollower in nearbyFollowers)
            {
                if (otherFollower != null && otherFollower.isDancing && otherFollower.currentSign == signCollider.transform)
                {
                    hasDancingFollower = true;
                    break;
                }
            }

            // 如果已经有Follower在跳舞，加入他们
            if (hasDancingFollower)
            {
                currentSign = signCollider.transform;
                currentDanceRadius = initialDanceRadius;
                currentState = FollowerState.Dancing;
                isDancing = true;
                return;
            }
            // 如果是新的Sign，开始新的舞蹈
            else if (!isDancing)
            {
                currentSign = signCollider.transform;
                currentDanceRadius = initialDanceRadius;
                currentState = FollowerState.Dancing;
                isDancing = true;
                return;
            }
        }
    }

    void HandleSignCollision()
    {
        // 生成新的Followers
        int spawnCount = Random.Range(minSpawnCount, maxSpawnCount);
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnNewFollower();
        }
        
        // 销毁当前Follower
        if (creatureManager != null)
        {
            creatureManager.OnFollowerDestroyed(gameObject);
        }
        Destroy(gameObject);
    }

    void SpawnNewFollower()
    {
        Vector3 spawnPos = transform.position + Random.insideUnitSphere * 2f;
        spawnPos.z = 0f;
        GameObject newFollower = Instantiate(gameObject, spawnPos, Quaternion.identity);
        // 新生成的Follower会继承相同的冷却时间设置
    }

    void SetNewRandomPoint()
    {
        float randomX = Random.Range(minX, maxX);
        float randomY = Random.Range(minY, maxY);
        targetPoint = new Vector3(randomX, randomY, 0f);
    }

    void MoveTowards(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        
        float currentSpeed = currentState == FollowerState.Moving ? moveSpeed : idleSpeed;
        transform.position += direction * currentSpeed * Time.deltaTime;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 处理与The Still One的碰撞
        if (collision.gameObject.CompareTag("TheStillOne"))
        {
            // 通知CreatureManager
            if (creatureManager != null)
            {
                creatureManager.OnFollowerDestroyed(gameObject);
            }
            // 销毁当前Follower
            Destroy(gameObject);
            return;
        }

        // 处理与其他Follower的碰撞
        Follower otherFollower = collision.gameObject.GetComponent<Follower>();
        if (otherFollower != null)
        {
            // 计算碰撞方向并应用力
            Vector3 collisionDirection = PhysicsHelper.GetMoveDirection(transform.position, collision.transform.position);
            if (rb != null)
            {
                PhysicsHelper.ApplyForce(rb, collisionDirection, collisionForce);
            }
        }
    }

    void ClampPosition()
    {
        transform.position = PhysicsHelper.ClampToScreenBounds(transform.position, mainCamera, screenBoundaryOffset);
    }

    // 添加新方法
    public void OnSignDisappearing()
    {
        if (currentState == FollowerState.Dancing)
        {
            currentState = FollowerState.Idle;
            isDancing = false;
            currentSign = null;
            SetNewRandomPoint();
        }
    }
}
