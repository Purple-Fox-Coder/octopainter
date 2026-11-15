using UnityEngine;
using System.Collections.Generic;

[DefaultExecutionOrder(100)]
public class TentaclePhysics : MonoBehaviour
{
	[Header("Chain")]
	
	[Tooltip("Top of the arm")]
	public Transform root_bone;
	[Tooltip("Tip of the arm")]
	public Transform tip_bone;
	[Tooltip("Where to aim to be")]
	public Transform ik_target; // This whole file is practically a custom IK solution

	[Header("Physics Settings")]
	public Vector3 gravity = new Vector3(0F, -9.81F, 0F);
	[Tooltip("How weighty the arm should feel")]
	[Range(0F, 1F)]
	public float damping = 0.08F;
	[Tooltip("How stiff the arm should be")]
	[Range(0F, 1F)]
	public float stiffness = 0.05F;
	[Tooltip("Performance setting, more looks better, less is more performant")]
	[Range(1, 32)]
	public int iterations = 10;
	[Tooltip("How much of the original curl to keep")]
	[Range(0F, 1F)]
	public float curl = 0.1F;

	readonly List<Transform> bones = new List<Transform>();
	Vector3   [] position, previous, target, resting_local;
	Quaternion[] resting_rotation;
	float     [] resting_length;
	float     [] resting_distance;

	Transform frame;
	float     previousDT;
	float     total_length;
	bool      is_ready;

	void OnEnable() => Init();
	[ContextMenu("Re-initialize")]
	public void Init() { // pre-proc
		this.is_ready = false;
		this.bones.Clear();

		if (this.root_bone == null) {
			Debug.LogError("TENT: No root bone assigned!", this);
			return;
		}

		Transform tip = this.root_bone;
		while (tip != null) {
			this.bones.Add(tip);
			if (this.tip_bone != null && tip == this.tip_bone)
				break;
			tip = tip.childCount > 0 ? tip.GetChild(0) : null;
		}

		int num_bones = this.bones.Count;
		if (num_bones < 3) {
			Debug.LogError("TENT: Wrong root bone!", this);
			return;
		}

		this.position         = new Vector3[num_bones];
		this.previous         = new Vector3[num_bones];
		this.target           = new Vector3[num_bones];
		this.resting_local    = new Vector3[num_bones];
		this.resting_rotation = new Quaternion[num_bones];
		this.resting_length   = new float[num_bones - 1];
		this.resting_distance = new float[num_bones];
		this.previousDT       = 1F / 60F;

		// Since the model is bent, we need to do some fuckery to ... compensate :)
		this.frame = this.root_bone.parent;
		for (int i = 0; i < num_bones; ++i) {
			this.position[i] = this.previous[i] = this.bones[i].position;
			this.resting_local[i] = this.frame ? this.frame.InverseTransformPoint(this.position[i]) : this.position[i];
			this.resting_rotation[i] = this.bones[i].localRotation;
		}

		this.total_length = 0F;
		for (int i = 0; i < num_bones - 1; ++i) {
			this.resting_length[i] = (this.resting_local[i + 1] - this.resting_local[i]).magnitude;
			this.total_length += this.resting_length[i];
		}

		float running_length = 0F; // AKA how far are we now
		for (int i = 0; i < num_bones; ++i) {
			this.resting_distance[i] = this.total_length > 0F ? running_length / this.total_length : 0F;
			if (i < num_bones - 1)
				running_length += this.resting_length[i];
		}

		this.is_ready = true;
	}

	void LateUpdate() {
		if (!this.is_ready)
			return;

		// If framerate is really slow, avoid having crazy movements from it
		float frameDT = Mathf.Min(Time.deltaTime, 0.05F);
		if (frameDT <= 0F)
			return;

		int num_bones = this.bones.Count;
		Transform tip = this.bones[num_bones - 1];

		// Current position, based on the IK:
		Vector3    root_position = this.bones[0].position;
		Vector3    tip_position  = this.ik_target ? this.ik_target.position : tip.position;
		Quaternion tip_rotation  = this.ik_target ? this.ik_target.rotation : tip.rotation;

		float   scale     = frame ? frame.lossyScale.x : 1F;
		float   max_reach = this.total_length * scale;
		Vector3 to_tip    = tip_position - root_position;
		if (to_tip.magnitude > max_reach)
			tip_position = root_position + to_tip.normalized * (max_reach * 0.999F);

		// Since it's pre bent I want to use that to make it seem more "natural"
		// maybe allows for more artistic funsies :3
		// for (int i = 0; i < num_bones; ++i)
		// 	this.target[i] = tip_position - 
		// 		(this.frame ? this.frame.TransformPoint(this.resting_local[i]) : this.resting_local[i]);
		Vector3    resting_root  = this.RestingWorld(0);
		Vector3    resting_chord = this.RestingWorld(num_bones - 1) - resting_root;
		Vector3    actual_chord  = tip_position - root_position;
		Quaternion fit           = (resting_chord.sqrMagnitude > 1e-10F && actual_chord.sqrMagnitude > 1e-10F)
			? Quaternion.FromToRotation(resting_chord, actual_chord)
			: Quaternion.identity;
			
		// Vector3    remaining     = actual_chord - fit * resting_chord;
		// for (int i = 0; i < num_bones; ++i)
		// 	this.target[i] = root_position + fit * (this.RestingWorld(i) - resting_root)
		// 	                + remaining * this.resting_distance[i];
		Vector3 chord_dir = actual_chord.sqrMagnitude > 1e-10F ? actual_chord.normalized : Vector3.up;
		float   rest_len   = resting_chord.magnitude;
		float   actual_len = actual_chord.magnitude;
		float   slack_rest = max_reach - rest_len;
		float   slack_now  = Mathf.Max(max_reach - actual_len, 0F);
		float   bend       = slack_rest > 1e-4F ? Mathf.Min(Mathf.Sqrt(slack_now / slack_rest), 3F) : 0F;
		float   stretch    = rest_len > 1e-6F ? actual_len / rest_len : 1F;
		for (int i = 0; i < num_bones; ++i) {
			Vector3 offset = fit * (this.RestingWorld(i) - resting_root);
			float   along  = Vector3.Dot(offset, chord_dir);
			this.target[i] = root_position + chord_dir * (along * stretch) + (offset - chord_dir * along) * (bend * this.curl);
		}

		int   steps       = Mathf.Clamp(Mathf.CeilToInt(frameDT / (1F / 60F)), 1, 4);
		float delta_time  = frameDT / steps;

		for (int step = 0; step < steps; ++step)
			Simulate(delta_time, scale, root_position, tip_position);

		ApplyToBones();

		// Avoid overriding the IK:
		tip.SetPositionAndRotation(tip_position, tip_rotation);
	}

	void Simulate(float delta_time, float scale, Vector3 root_position, Vector3 tip_position) {
		int num_bones        = this.bones.Count;
		Vector3 curr_gravity = this.gravity * (delta_time * delta_time); // m/s^2 * s^2 = m

		float time_ratio = delta_time / this.previousDT;
		float damp       = 1F - Mathf.Pow(1F - this.damping, delta_time * 60F);
		float stiff      = 1F - Mathf.Pow(1F - this.stiffness, delta_time * 60F);

		for (int i = 1; i < num_bones - 1; ++i) {
			Vector3 velocity  = (this.position[i] - this.previous[i]) * (time_ratio * (1F - damp));
			this.previous[i]  = this.position[i];
			this.position[i] += velocity + curr_gravity;
			this.position[i] += (this.target[i] - this.position[i]) * stiff;
		}

		this.previousDT = delta_time;

		// Avoid overriding IK:
		this.position[0] = this.previous[0] = root_position;
		this.position[num_bones - 1] = this.previous[num_bones - 1] = tip_position;

		for (int i = 0; i < this.iterations; ++i) {
			if ((i & 1) == 0)
				for (int j = 0; j < num_bones - 1; ++j) Constrain(j, scale, num_bones);
			else
				for (int j = num_bones - 2; j >= 0; --j) Constrain(j, scale, num_bones);
		}
	}

	Vector3 RestingWorld(int i) {
		return this.frame ? this.frame.TransformPoint(this.resting_local[i]) : this.resting_local[i];
	}

	// The idea is to minimize strange drifting 
	void Constrain(int i, float scale, int num_bones) {
		Vector3 distance_vec = this.position[i + 1] - this.position[i];
		float   distance     = distance_vec.magnitude;
		if (distance < 1e-6F)
			return;

		float weight_root = i == 0 ? 0F : 1F;
		float weight_tip  = i + 1 == num_bones - 1 ? 0F : 1F;
		float weight_dist = weight_root + weight_tip;
		if (weight_dist <= 0F)
			return;

		float difference      = (distance - this.resting_length[i] * scale) / distance;
		this.position[i]     += distance_vec * (difference * (weight_root / weight_dist));
		this.position[i + 1] -= distance_vec * (difference * (weight_tip / weight_dist));
	}

	// Mainly rotation corrections
	void ApplyToBones() {
		int num_bones = this.bones.Count;
		for (int i = 0; i < num_bones - 1; ++i) {
			Transform bone    = this.bones[i];
			bone.localRotation = this.resting_rotation[i];

			Vector3   current = this.bones[i + 1].position - bone.position;
			Vector3   desired = this.position[i + 1] - bone.position;

			if (current.sqrMagnitude < 1e-10F || desired.sqrMagnitude < 1e-10F)
				continue;

			bone.rotation = Quaternion.FromToRotation(current, desired) * bone.rotation;
		}
	}

	// Something to help visualize this mess :)
	void OnDrawGizmosSelected() {
		if (!this.is_ready)
			return;
		Gizmos.color = Color.cyan;
		for (int i = 0; i < this.position.Length - 1; ++i)
			Gizmos.DrawLine(this.position[i], this.position[i + 1]);
		Gizmos.color = Color.yellow;
		for (int i = 0; i < this.target.Length - 1; ++i)
			Gizmos.DrawLine(this.target[i], this.target[i + 1]);
	}
}
