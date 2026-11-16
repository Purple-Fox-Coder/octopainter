using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterSpawner : MonoBehaviour {
	public InputAction next_button;
	public CanvasPainter canvas;

	[Header("Fade")]
	public float fade_out_time = 0.5F;
	public float hold_time     = 0.3F;
	public float fade_in_time  = 0.5F;

	GameObject[] characters;
	int          index = -1;
	bool         is_busy;
	Material     fade_material;
	Transform    fade_quad;

	void Awake() {
		this.characters = new GameObject[this.transform.childCount];
		for (int i = 0; i < this.characters.Length; ++i) {
			this.characters[i] = this.transform.GetChild(i).gameObject;
			this.characters[i].SetActive(false);
		}
	}

	void OnEnable() {
		this.next_button.performed += this.OnNext;
		this.next_button.Enable();
	}

	void OnDisable() {
		this.next_button.performed -= this.OnNext;
		this.is_busy = false;
		if (this.fade_quad != null)
			this.SetFade(0F);
	}

	void OnNext(InputAction.CallbackContext ctx) {
		// if (this.index >= 0)
		// 	this.characters[this.index].SetActive(false);
		if (this.is_busy || this.characters.Length == 0)
			return;
		this.StartCoroutine(this.Transition());
	}

	IEnumerator Transition() {
		this.is_busy = true;
		this.EnsureFade();

		yield return this.Fade(0F, 1F, this.fade_out_time);

		if (this.index >= 0)
			this.characters[this.index].SetActive(false);
		
		// Using modulo as a simple wrapping, doubt it'll come up
		// but you never know :)
		this.index = (this.index + 1) % this.characters.Length;
		this.characters[this.index].SetActive(true);

		canvas.ClearCanvas();

		yield return new WaitForSeconds(this.hold_time);
		yield return this.Fade(1F, 0F, this.fade_in_time);

		this.is_busy = false;
	}

	IEnumerator Fade(float from, float to, float duration) {
		float time = 0F;
		while (time < duration) {
			time += Time.deltaTime;
			this.SetFade(Mathf.Lerp(from, to, time / duration));
			yield return null;
		}
		this.SetFade(to);
	}

	void SetFade(float alpha) {
		this.fade_material.color = new Color(0F, 0F, 0F, alpha);
		this.fade_quad.gameObject.SetActive(alpha > 0.001F);
	}

	void EnsureFade() {
		if (this.fade_quad != null)
			return;

		var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
		Destroy(quad.GetComponent<Collider>());
		quad.name = "__player_FadeQuad";

		this.fade_material = new Material(Shader.Find("Sprites/Default"));
		this.fade_material.renderQueue = 5000;
		quad.GetComponent<MeshRenderer>().material = this.fade_material;

		this.fade_quad = quad.transform;
		this.fade_quad.SetParent(Camera.main.transform, false);
		this.fade_quad.localPosition = new Vector3(0F, 0F, 0.2F);
		this.fade_quad.localScale    = new Vector3(4F, 4F, 1F);

		this.SetFade(0F);
	}
}
