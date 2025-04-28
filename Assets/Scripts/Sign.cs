using UnityEngine;

public class Sign : MonoBehaviour
{
    //fading n shirink overtime
    public float rotationSpeed = 180f; 
    public float shrinkSpeed = 1f; 
    public float fadeSpeed = 1f; 
    
    public bool isDisappearing = false;
    private SpriteRenderer spriteRenderer;
    private CreatureManager creatureManager;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        creatureManager = FindObjectOfType<CreatureManager>();
    }

    void Update()
    {
        if (isDisappearing)
        {
           
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
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("TheStillOne") && !isDisappearing)
        {
            StartDisappearing();
        }
    }
    //tell surrounding followers dissaperar
    void StartDisappearing()
    {
        isDisappearing = true;
        
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 10f);
        foreach (Collider2D collider in colliders)
        {
            Follower follower = collider.GetComponent<Follower>();
            if (follower != null && follower.currentSign == transform)
            {
                follower.OnSignDisappearing();
            }
        }
    }
} 