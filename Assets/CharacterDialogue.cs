// Put per character, pre loads and suchlike :3c

using System.Collections;
using UnityEngine;
using TMPro;

public class CharacterDialogue : MonoBehaviour {
	public TMP_Text text_box;
	public float chars_per_second = 40F;
	public float pause_short      = 0.15F;
	public float pause_long       = 0.35F;
	public float page_hold        = 1F / 40F + 0.05F;
	public bool  should_hide      = false;

	string[]  lines;
	int       line_index;
	int       page;
	bool      skip;
	Coroutine routine;

	void OnEnable() {
		this.lines         = DialogueLoader.Load(this.gameObject.name);
		this.line_index    = 0;
		this.skip          = false;
		this.text_box.text = "";
		if (this.lines.Length == 0) {
			return;
		}
		
		this.ShowLine();
	}

	public void Next() {
		this.skip = true;
	}

	void ShowLine() {
		this.text_box.overflowMode = TextOverflowModes.Page;
		this.text_box.text         = this.lines[this.line_index];
		this.text_box.ForceMeshUpdate();
		this.page = 1;
		this.StartPage();
	}

	void StartPage() {
		if (this.routine != null)
			this.StopCoroutine(this.routine);
		this.routine = this.StartCoroutine(this.SpeakPage());
	}

	void Advance() {
		if (this.page < this.text_box.textInfo.pageCount) {
			++this.page;
			this.StartPage();
			return;
		}

		++this.line_index;
		if (this.line_index >= this.lines.Length) {
			this.text_box.text = "";
			this.routine = null;
			if (this.should_hide)
				this.gameObject.SetActive(false);
			return;
		}
		this.ShowLine();
	}

	IEnumerator SpeakPage() {
		var text_info = this.text_box.textInfo;
		var page_info = text_info.pageInfo[this.page - 1];
		int first     = page_info.firstCharacterIndex;
		int last      = page_info.lastCharacterIndex;

		this.text_box.pageToDisplay        = this.page;
		this.text_box.maxVisibleCharacters = first;

		for (int i = first; i <= last; ++i) {
			if (this.skip) {
				this.skip = false;
				break;
			}
			
			this.text_box.maxVisibleCharacters = i + 1;
			char c    = text_info.characterInfo[i].character;
			char next = i < last ? text_info.characterInfo[i + 1].character : '\0';
			yield return new WaitForSeconds(this.Delay(c, next));
		}
		this.text_box.maxVisibleCharacters = last + 1;

		float held = 0F;
		while (held < this.page_hold && !this.skip) {
			held += Time.deltaTime;
			yield return null;
		}
		
		this.skip = false;
		this.Advance();
	}

	float Delay(char c, char next) {
		bool is_next_end = next == '!' || next == '?';

		switch (c) {
			case '.': case '!': case '?':
				return is_next_end ? 1F / this.chars_per_second : this.pause_long;
			case ',': case ';': case ':':
				return this.pause_short;
		}

		return 1F / this.chars_per_second;
	}
}
