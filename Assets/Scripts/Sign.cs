using UnityEngine;
using System.Collections.Generic;

public class Sign : MonoBehaviour
{
    private enum SignState
    {
        Still,
        Wandering,
        Dying,
        Respawn
    }

    //fading n shirink overtime
    public float rotationSpeed = 180f;
    public float shrinkSpeed = 1f;
    public float fadeSpeed = 1f;

    public bool isDisappearing = false;
    private SpriteRenderer spriteRenderer;
    private CreatureManager creatureManager;
    private Camera mainCamera;
    private float screenBoundaryOffset = 1f;
    private SignState currentState = SignState.Still;
    private float stillRotationSpeed = 0f;
    private Vector3 wanderDirection = Vector3.right;
    private float wanderDirectionTimer = 0f;
    private int dancingHitCount = 0;
    private bool collidersDisabled = false;
    private readonly HashSet<Follower> nearbyFollowers = new HashSet<Follower>();
    private const int DANCING_HIT_THRESHOLD = 5;
    private const float WANDER_MOVE_SPEED = 0.3f;
    private const float WANDER_ROTATE_SPEED = 30f;

    void Start()
    {
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
        creatureManager = FindObjectOfType<CreatureManager>();
        stillRotationSpeed = Random.Range(-45f, 45f);
        SetRandomWanderDirection();
    }

    void Update()
    {
        switch (currentState)
        {
            case SignState.Still:
                HandleStill();
                break;
            case SignState.Wandering:
                HandleWandering();
                break;
            case SignState.Respawn:
                HandleRespawn();
                break;
            case SignState.Dying:
                HandleDying();
                break;
        }

        if (mainCamera != null)
            transform.position = PhysicsHelper.WrapToScreenBounds(transform.position, mainCamera, screenBoundaryOffset);
    }

    void HandleStill()
    {
        transform.Rotate(0, 0, stillRotationSpeed * Time.deltaTime);

        if (HasDancingFollowerTargetingMe())
        {
            currentState = SignState.Wandering;
        }
    }

    void HandleWandering()
    {
        wanderDirectionTimer += Time.deltaTime;
        if (wanderDirectionTimer >= 1.5f)
        {
            SetRandomWanderDirection();
        }

        transform.position += wanderDirection * WANDER_MOVE_SPEED * Time.deltaTime;
        transform.Rotate(0, 0, WANDER_ROTATE_SPEED * Time.deltaTime);

        if (dancingHitCount >= DANCING_HIT_THRESHOLD)
        {
            StartDying();
        }
    }

    void HandleRespawn()
    {
        int spawnCount = Random.Range(1, 3);
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPos = transform.position + Random.insideUnitSphere * 2f;
            spawnPos.z = 0f;
            if (creatureManager != null)
            {
                creatureManager.SpawnSignAt(spawnPos);
            }
        }

        StartDying();
    }

    void HandleDying()
    {
        if (!collidersDisabled)
        {
            DisableAllColliders();
            collidersDisabled = true;
        }

        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

        Vector3 newScale = transform.localScale - Vector3.one * shrinkSpeed * Time.deltaTime;
        transform.localScale = newScale;

        Color currentColor = spriteRenderer.color;
        currentColor.a -= fadeSpeed * Time.deltaTime;
        spriteRenderer.color = currentColor;

        if (currentColor.a <= 0 || transform.localScale.x <= 0)
        {
            if (creatureManager != null)
            {
                creatureManager.OnSignDestroyed(gameObject);
            }
            Destroy(gameObject);
        }
    }

    void StartDying()
    {
        if (currentState == SignState.Dying)
            return;

        currentState = SignState.Dying;
        isDisappearing = true;
    }

    bool HasDancingFollowerTargetingMe()
    {
        foreach (Follower follower in nearbyFollowers)
        {
            if (follower == null)
                continue;

            if (follower.IsDancing() && follower.currentSign == transform)
            {
                return true;
            }
        }
        return false;
    }

    void SetRandomWanderDirection()
    {
        wanderDirection = Random.insideUnitCircle.normalized;
        if (wanderDirection.sqrMagnitude < 0.0001f)
        {
            wanderDirection = Vector3.right;
        }
        wanderDirectionTimer = 0f;
    }

    void DisableAllColliders()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.enabled = false;
        }
    }
    //add follower to nearby followers if in detection range
    public void OnDetectionTriggerEnter2D(Collider2D other)
    {
        Follower follower = other.GetComponentInParent<Follower>();
        if (follower != null)
            nearbyFollowers.Add(follower);
    }

    public void OnDetectionTriggerExit2D(Collider2D other)
    {
        Follower follower = other.GetComponentInParent<Follower>();
        if (follower != null)
            nearbyFollowers.Remove(follower);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (currentState == SignState.Dying)
            return;

        if (collision.gameObject.GetComponentInParent<TheStillOne>() != null)
        {
            currentState = SignState.Respawn;
            return;
        }

        Follower follower = collision.gameObject.GetComponentInParent<Follower>();
        if (follower != null && follower.IsDancing() && follower.currentSign == transform)
        {
            dancingHitCount++;
            if (dancingHitCount >= DANCING_HIT_THRESHOLD)
            {
                StartDying();
            }
        }
    }
} 