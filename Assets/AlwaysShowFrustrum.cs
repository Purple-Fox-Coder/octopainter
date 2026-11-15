using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AlwaysShowFrustum : MonoBehaviour
{
	public Color color = new Color(1F, 1F, 1F, 0.6F);

	void OnDrawGizmos() {
		Camera cam = GetComponent<Camera>();
		if (!cam)
			return;

		Gizmos.color  = this.color;
		Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

		if (cam.orthographic) {
			float height = cam.orthographicSize * 2F;
			float depth  = cam.farClipPlane - cam.nearClipPlane;
			Gizmos.DrawWireCube(
				new Vector3(0F, 0F, cam.nearClipPlane + depth * 0.5F),
				new Vector3(height * cam.aspect, height, depth));
		} else {
			Gizmos.DrawFrustum(Vector3.zero, cam.fieldOfView, cam.farClipPlane, cam.nearClipPlane, cam.aspect);
		}
	}
}
