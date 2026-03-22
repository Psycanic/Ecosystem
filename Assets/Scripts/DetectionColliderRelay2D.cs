using UnityEngine;

/// <summary>
/// 挂在「子物体」的 Detection Trigger 上，把 Enter/Exit 转发到父级上的 Follower / TheStillOne / Sign。
/// 主脚本在根物体、Trigger 在子物体时，Unity 不会调用根物体上的 OnTriggerEnter2D。
/// </summary>
public class DetectionColliderRelay2D : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        Follower follower = GetComponentInParent<Follower>();
        if (follower != null)
        {
            follower.OnDetectionTriggerEnter2D(other);
            return;
        }

        TheStillOne still = GetComponentInParent<TheStillOne>();
        if (still != null)
        {
            still.OnDetectionTriggerEnter2D(other);
            return;
        }

        Sign sign = GetComponentInParent<Sign>();
        if (sign != null)
        {
            sign.OnDetectionTriggerEnter2D(other);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Follower follower = GetComponentInParent<Follower>();
        if (follower != null)
        {
            follower.OnDetectionTriggerExit2D(other);
            return;
        }

        TheStillOne still = GetComponentInParent<TheStillOne>();
        if (still != null)
        {
            still.OnDetectionTriggerExit2D(other);
            return;
        }

        Sign sign = GetComponentInParent<Sign>();
        if (sign != null)
        {
            sign.OnDetectionTriggerExit2D(other);
        }
    }
}
