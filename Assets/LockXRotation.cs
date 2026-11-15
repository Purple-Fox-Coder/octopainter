using UnityEngine;

public class LockXRotation : MonoBehaviour
{
    void LateUpdate() {
        // Need to update on world space
        Vector3 projected = transform.forward;
        projected.y = 0F;
        if (projected.sqrMagnitude > 0.0001F) // Arbitrarially small epsilon 
            this.transform.rotation = Quaternion.LookRotation(projected, Vector3.up);
    }
}
