using UnityEngine;
using System.Collections.Generic;

public class Follower : MonoBehaviour
{
    private enum FollowerState
    {
        Idle,
        Moving,
        Dancing,
        Fleeing
    }

    private float moveSpeed = 3f;
    private float idleSpeed = 2f;
    private float rotationSpeed = 5f;
    private float detectionRange = 7f;
    private float danceSpeed = 4f;
    private float fleeSpeed = 5f;
    private float spawnCooldown = 7f; 
    private Vector2 pivotOffset = Vector2.zero; 
    
    private float minMoveTime = 0.5f;
    private float maxMoveTime = 2f;
    private float minIdleTime = 0.3f;
    private float maxIdleTime = 1.5f;
    

    //dance as a circle 
    private float initialDanceRadius = 5f;
    //slowly decrease in radius
    private float minDanceRadius = 1f;
    private float danceRadiusDecreaseRate = 0.1f;
    [SerializeField, Range(0f, 1f)] private float stillOneFleeChance = 0.1f; // 每次进入 StillOne 检测范围时触发逃跑的概率
    //spawn setting
    public int minSpawnCount = 1;
    public int maxSpawnCount = 3;
       
    private FollowerState currentState = FollowerState.Idle;
    private Vector3 targetPoint;

    //dance setting
    private float stateTimer = 0f;
    private float currentDanceRadius;
    private float danceAngle = 0f;

    //still one setting
    public Transform currentSign;
    private Transform theStillOne;


    private Camera mainCamera;
    private float minX, maxX, minY, maxY;
    private float screenBoundaryOffset = 1f;
    private CreatureManager creatureManager;
    private bool isDancing = false;
    private Rigidbody2D rb;

    //spawn setting
    private float spawnTimer = 0f; 
    private bool isInCooldown = true; 
    //state setting
    private float currentStateDuration = 0f;
    private float fleeStartDistance = 0f;
    //sign setting
    private readonly HashSet<Transform> nearbySigns = new HashSet<Transform>();
    private bool stillOneInRange = false;
    private bool isRolledThisOverlap = false; // avoid rolling every frame

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainCamera = Camera.main;
        CalculateScreenBoundaries();
        SetNewRandomPoint();
        FindTheStillOne();
        creatureManager = FindObjectOfType<CreatureManager>();
    
        rb = GetComponent<Rigidbody2D>();
        
       
        spawnTimer = 0f;
        isInCooldown = true;
        EnterIdle();
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
        
        if (isInCooldown)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnCooldown)
            {
                isInCooldown = false;
            }
        }

        if (!stillOneInRange)
     = false;
        else if (currentState != FollowerState.Fleeing &&)
        {
     = true;
            if (Random.value < stillOneFleeChance)
                EnterFleeing();
        }

        if ((currentState == FollowerState.Idle || currentState == FollowerState.Moving) && !isInCooldown && nearbySigns.Count > 0)
        {
            Transform signTarget = SelectNearestSign();
            if (signTarget != null)
            {
                EnterDancing(signTarget);
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

        WrapScreenPosition();
    }

    void HandleIdle()
    {
        stateTimer += Time.deltaTime;

        if (stateTimer >= currentStateDuration)
        {
            EnterMoving();
        }
    }

    void HandleMoving()
    {
        MoveTowards(targetPoint);

        stateTimer += Time.deltaTime;
        if (stateTimer >= currentStateDuration || Vector3.Distance(transform.position, targetPoint) < 0.1f)
        {
            EnterIdle();
        }
    }

    void HandleDancing()
    {
        if (currentSign == null)
        {
            ExitDancing();
            EnterIdle();
            return;
        }

        Sign sign = currentSign.GetComponent<Sign>();
        if (sign != null && sign.isDisappearing)
        {
            ExitDancing();
            EnterIdle();
            return;
        }

        UpdateDanceSpinning();
    }

    void UpdateDanceSpinning()
    {
        currentDanceRadius = Mathf.Max(minDanceRadius, currentDanceRadius - danceRadiusDecreaseRate * Time.deltaTime);
        danceAngle += danceSpeed * Time.deltaTime;

        Vector3 center = currentSign.position + new Vector3(pivotOffset.x, pivotOffset.y, 0f);

        Vector3 targetPosition = center + new Vector3(
            Mathf.Cos(danceAngle) * currentDanceRadius,
            Mathf.Sin(danceAngle) * currentDanceRadius,
            0f
        );

        Vector3 toTarget = targetPosition - transform.position;
        Vector3 direction = toTarget.normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;

        Vector3 radial = targetPosition - center;
        Vector3 tangent = new Vector3(-radial.y, radial.x, 0f);
        if (tangent.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, angle), rotationSpeed * Time.deltaTime);
        }
    }

    void HandleFleeing()
    {
        if (theStillOne == null)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            EnterIdle();
            return;
        }

        Vector3 fleeDirection = PhysicsHelper.GetFleeDirection(transform.position, theStillOne.position);

        if (rb != null)
        {
            rb.linearVelocity = (Vector2)(fleeDirection * fleeSpeed);
        }
        else
        {
            transform.position += fleeDirection * fleeSpeed * Time.deltaTime;
        }

        float movedDistance = Vector3.Distance(transform.position, theStillOne.position);
        if (!stillOneInRange && movedDistance > fleeStartDistance + detectionRange)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            EnterIdle();
        }
    }

    void EnterIdle()
    {
        currentState = FollowerState.Idle;
        stateTimer = 0f;
        currentStateDuration = Random.Range(minIdleTime, maxIdleTime);
        SetNewRandomPoint();
        ExitDancing();
    }

    void EnterMoving()
    {
        currentState = FollowerState.Moving;
        stateTimer = 0f;
        currentStateDuration = Random.Range(minMoveTime, maxMoveTime);
        SetNewRandomPoint();
    }

    void EnterDancing(Transform signTarget)
    {
        currentSign = signTarget;
        currentDanceRadius = initialDanceRadius;
        danceAngle = Random.Range(0f, Mathf.PI * 2f);
        currentState = FollowerState.Dancing;
        isDancing = true;
    }

    void ExitDancing()
    {
        isDancing = false;
        currentSign = null;
    }

    void EnterFleeing()
    {
        if (theStillOne == null)
            return;

        ExitDancing();
        currentState = FollowerState.Fleeing;
        fleeStartDistance = Vector3.Distance(transform.position, theStillOne.position);
    }

    Transform SelectNearestSign()
    {
        float bestDistance = float.MaxValue;
        Transform best = null;
        foreach (Transform sign in nearbySigns)
        {
            if (sign == null)
                continue;

            float distance = Vector3.Distance(transform.position, sign.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = sign;
            }
        }
        return best;
    }

    void HandleSignCollision()
    {
        // new follower spawn
        int spawnCount = Random.Range(minSpawnCount, maxSpawnCount + 1);
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnNewFollower();
        }

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
        if (creatureManager != null)
        {
            creatureManager.SpawnFollowerAt(spawnPos);
        }
        else
        {
            Instantiate(gameObject, spawnPos, Quaternion.identity);
        }
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
        if (currentState != FollowerState.Dancing || currentSign == null)
            return;

        Sign hitSign = collision.gameObject.GetComponentInParent<Sign>();
        if (hitSign != null && hitSign.transform == currentSign)
            HandleSignCollision();
    }

    void WrapScreenPosition()
    {
        transform.position = PhysicsHelper.WrapToScreenBounds(transform.position, mainCamera, screenBoundaryOffset);
    }

    public bool IsDancing()
    {
        return currentState == FollowerState.Dancing;
    }

    
    public void OnDetectionTriggerEnter2D(Collider2D other)
    {
        // 碰撞体常在子物体上且为 Untagged,,用parent组件判断
        Sign sign = other.GetComponentInParent<Sign>();
        if (sign != null)
        {
            nearbySigns.Add(sign.transform);
            return;
        }

        if (other.GetComponentInParent<TheStillOne>() != null)
            stillOneInRange = true;
    }

    public void OnDetectionTriggerExit2D(Collider2D other)
    {
        Sign sign = other.GetComponentInParent<Sign>();
        if (sign != null)
        {
            nearbySigns.Remove(sign.transform);
            return;
        }

        if (other.GetComponentInParent<TheStillOne>() != null)
            stillOneInRange = false;
    }
}
