using UnityEngine;

public static class PhysicsHelper
{
    
    public static bool IsInRange(Transform object1, Transform object2, float range)
    {
        return Vector3.Distance(object1.position, object2.position) <= range;
    }

    public static Collider2D[] GetObjectsInRange(Vector3 position, float range, string tag)
    {
       

        Collider2D[] allColliders = Physics2D.OverlapCircleAll(position, range);
        return System.Array.FindAll(allColliders, collider => 
        {
            if (collider == null) return false;
            return collider.CompareTag(tag);
        });
    }

    
    public static T[] GetComponentsInRange<T>(Vector3 position, float range) where T : Component
    {
        Collider2D[] allColliders = Physics2D.OverlapCircleAll(position, range);
        return System.Array.ConvertAll(allColliders, collider => collider.GetComponent<T>());
    }

    // 
    public static Vector3 GetFleeDirection(Vector3 currentPosition, Vector3 targetPosition)
    {
        return (currentPosition - targetPosition).normalized;
    }

    // dir towards the target
    public static Vector3 GetMoveDirection(Vector3 currentPosition, Vector3 targetPosition)
    {
        return (targetPosition - currentPosition).normalized;
    }

   

    
    public static void ApplyForce(Rigidbody2D rb, Vector3 direction, float force)
    {
        if (rb != null)
        {
            rb.AddForce(direction * force, ForceMode2D.Impulse);
        }
    }

    // collision check
    public static bool IsColliding(Collider2D collider1, Collider2D collider2)
    {
        return Physics2D.IsTouching(collider1, collider2);
    }

  /*
    public static Vector3 GetCollisionPoint(Collider2D collider1, Collider2D collider2)
    {
        Vector3 center1 = collider1.bounds.center;
        Vector3 center2 = collider2.bounds.center;
        
        // 返回两个中心点的中点
        return Vector3.Lerp(center1, center2, 0.5f);
    }

    */

  
    public static void GetScreenBounds(Camera camera, out float minX, out float maxX, out float minY, out float maxY, float offset = 1f)
    {
        Vector2 screenBounds = camera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, camera.transform.position.z));
        minX = -screenBounds.x + offset;
        maxX = screenBounds.x - offset;
        minY = -screenBounds.y + offset;
        maxY = screenBounds.y - offset;
    }

    // every object is within screen bound
    public static Vector3 ClampToScreenBounds(Vector3 position, Camera camera, float offset = 1f)
    {
        float minX, maxX, minY, maxY;
        GetScreenBounds(camera, out minX, out maxX, out minY, out maxY, offset);
        
        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);
        return position;
    }
} 