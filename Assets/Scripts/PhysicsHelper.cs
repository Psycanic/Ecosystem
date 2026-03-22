using UnityEngine;


public static class PhysicsHelper
{
    public static Vector3 GetFleeDirection(Vector3 currentPosition, Vector3 targetPosition)
    {
        return (currentPosition - targetPosition).normalized;
    }

    public static void GetScreenBounds(Camera camera, out float minX, out float maxX, out float minY, out float maxY, float offset = 1f)
    {
        Vector2 screenBounds = camera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, camera.transform.position.z));
        minX = -screenBounds.x + offset;
        maxX = screenBounds.x - offset;
        minY = -screenBounds.y + offset;
        maxY = screenBounds.y - offset;
    }

    public static Vector3 WrapToScreenBounds(Vector3 position, Camera camera, float offset = 1f)
    {
        float minX, maxX, minY, maxY;
        GetScreenBounds(camera, out minX, out maxX, out minY, out maxY, offset);

        float rangeX = maxX - minX;
        float rangeY = maxY - minY;

        if (rangeX > Mathf.Epsilon)
            position.x = minX + Mathf.Repeat(position.x - minX, rangeX);
        else
            position.x = minX;

        if (rangeY > Mathf.Epsilon)
            position.y = minY + Mathf.Repeat(position.y - minY, rangeY);
        else
            position.y = minY;

        return position;
    }
}
