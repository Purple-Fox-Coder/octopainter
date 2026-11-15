using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterSpawner : MonoBehaviour {
	public InputAction next_button;

	GameObject[] characters;
	int          index = -1;

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
	}

	void OnNext(InputAction.CallbackContext ctx) {
		if (this.index >= 0)
			this.characters[this.index].SetActive(false);
		// Using modulo as a simple wrapping, doubt it'll come up
		// but you never know :)
		this.index = (this.index + 1) % this.characters.Length;
		this.characters[this.index].SetActive(true);
	}
}
