using UnityEngine;

[DefaultExecutionOrder(50)]
public class LockXRotation : MonoBehaviour
{
    Quaternion rest_offset;
    Vector3    rest_position;
    Quaternion last_yaw = Quaternion.identity;

    Quaternion FlatYaw() {
        Vector3 flat = Vector3.ProjectOnPlane(transform.parent.forward, Vector3.up);
        if (flat.sqrMagnitude > 1e-4F)
            this.last_yaw = Quaternion.LookRotation(flat, Vector3.up);
        return this.last_yaw;
    }

    void Start() {
        if (!this.transform.parent)
            return;
        Quaternion yaw     = this.FlatYaw();
        this.rest_offset   = Quaternion.Inverse(yaw) * this.transform.rotation;
        this.rest_position = Quaternion.Inverse(yaw) * (transform.position - transform.parent.position);
    }

    void LateUpdate() {
        if (!this.transform.parent)
            return;
        Quaternion yaw = this.FlatYaw();
        this.transform.SetPositionAndRotation(this.transform.parent.position + yaw * this.rest_position, yaw * this.rest_offset);
    }
}
