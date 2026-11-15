// Put per character, pre loads and suchlike :3c

using UnityEngine;
using TMPro;

public class CharacterDialogue : MonoBehaviour {
	public TMP_Text text_box;

	string[] lines;
	int      index;

	void OnEnable() {
		this.lines = DialogueLoader.Load(this.gameObject.name);
		this.index = 0;
		this.Show();
	}

	public void Next() {
		++this.index;
		if (this.index >= this.lines.Length) {
			this.gameObject.SetActive(false);
			return;
		}
		this.Show();
	}

	void Show() => this.text_box.text = this.lines.Length > 0
	                ? this.lines[this.index] : "";
}
