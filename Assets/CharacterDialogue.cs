// Put per character, pre loads and suchlike :3c

using System.Collections;
using UnityEngine;
using TMPro;

public class CharacterDialogue : MonoBehaviour {
	public TMP_Text text_box;

	[Header("Acting")]
	public float start_delay      = 0.5F;
	public float chars_per_second = 40F;
	public float pause_short      = 0.15F;
	public float pause_long       = 0.35F;
	public float page_hold        = 1F / 40F + 0.05F;

	[Header("Mumble")]
	public float       blip_volume = 1.0F;
	public float       pitch_min   = 0.9F;
	public float       pitch_max   = 1.1F;
	public int         blip_every  = 1; // if it's too blippy, de-blippify

	[Header("Voice")]
	public float voice_pitch   = 140F;
	public float wobble        = 0.05F;
	public float wobble_rate   = 6F;
	[Tooltip("Squeekiness")]
	public float formant_shift = 1F;
	public float breathiness   = 0.1F;
	public float blip_length   = 0.09F;

	[Header("Inflections")]
	public float question_rise  = 0.25F;
	public float exclaim_boost  = 0.15F;
	public float exclaim_volume = 1.3F;

	static readonly float[][] FORMANTS = {
		new[] { 800F, 1150F, 2900F },  // a
		new[] { 530F, 1840F, 2480F },  // e
		new[] { 270F, 2290F, 3010F },  // i
		new[] { 570F,  840F, 2410F },  // o
		new[] { 300F,  870F, 2240F },  // u
	};
	static readonly float[] BANDWIDTHS = { 80F, 90F,  120F };
	static readonly float[] GAINS      = { 1F,  0.6F, 0.3F };

	AudioSource voice;
	AudioClip[] vowel_clips;

	string[]  lines;
	int       line_index;
	int       page;
	Coroutine routine;
	int       vowel_count;
	char      prev_char;
	int       sentence_start;
	int       sentence_end;
	char      sentence_mark;

	void Awake() {
		if (!this.TryGetComponent(out this.voice))
			this.voice = this.gameObject.AddComponent<AudioSource>();
		this.voice.playOnAwake = false;
		this.voice.spatialBlend = 1F;
	
		this.vowel_clips = new AudioClip[5];
		for (int v = 0; v < 5; ++v)
			this.vowel_clips[v] = this.MakeVowel(v);
	}

	void OnEnable() {
		this.lines         = DialogueLoader.Load(this.gameObject.name);
		this.line_index    = 0;
		this.text_box.text = "";
		if (this.lines.Length == 0) {
			return;
		}
		
		this.ShowLine();
	}

	void ShowLine() {
		this.text_box.overflowMode = TextOverflowModes.Page;
		this.text_box.text         = this.lines[this.line_index];
		this.text_box.ForceMeshUpdate();
		this.sentence_end = -1;
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

		if (this.line_index == 0 && this.page == 1)
			yield return new WaitForSeconds(this.start_delay);

		for (int i = first; i <= last; ++i) {
			if (i > this.sentence_end)
				this.FindSentence(text_info, i);
			
			this.text_box.maxVisibleCharacters = i + 1;
			char c    = text_info.characterInfo[i].character;
			char next = i < last ? text_info.characterInfo[i + 1].character : '\0';
			if (this.IsVowel(c) && !this.IsVowel(this.prev_char) && ++this.vowel_count % this.blip_every == 0)
				this.Blip(c, i);
			this.prev_char = c;
			yield return new WaitForSeconds(this.Delay(c, next));
		}
		this.text_box.maxVisibleCharacters = last + 1;

		yield return new WaitForSeconds(this.page_hold);
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

	bool IsVowel(char c) {
		switch (char.ToLowerInvariant(c)) {
			case 'a': case 'e': case 'i': case 'o': case 'u': case 'y':
				return true;
		}
		return false;
	}

	void Blip(char c, int i) {
		if (this.voice == null)
			return;

		// this.voice.pitch = Random.Range(this.pitch_min, this.pitch_max);
		float pitch  = Random.Range(this.pitch_min, this.pitch_max);
		float volume = this.blip_volume;

		float part = (float)(i - this.sentence_start) / Mathf.Max(1, this.sentence_end - this.sentence_start);

		if (this.sentence_mark == '?') {
			pitch *= 1F + this.question_rise * part * part;
		} else if (this.sentence_mark == '!') {
			pitch  *= 1F + this.exclaim_boost;
			volume *= this.exclaim_volume;
		}

		this.voice.pitch = pitch;
		this.voice.PlayOneShot(this.vowel_clips[this.VowelIndex(c)], volume);
	}

	int VowelIndex(char c) {
		switch (char.ToLowerInvariant(c)) {
			case 'a': return 0;
			case 'e': return 1;
			case 'o': return 3;
			case 'u': return 4;
		}
		// i and y
		return 2; 
	}

	void FindSentence(TMP_TextInfo info, int i) {
		this.sentence_start = i;
		this.sentence_mark  = '\0';

		int j = i;
		for (; j < info.characterCount; ++j) {
			char ch = info.characterInfo[j].character;
			if (ch == '.' || ch == '!' || ch == '?') {
				this.sentence_mark = ch;
				break;
			}
		}

		this.sentence_end = Mathf.Min(j, info.characterCount - 1);
	}

	AudioClip MakeVowel(int vowel) {
		const int rate = 44100;
		int       len  = Mathf.Max(1, (int)(rate * this.blip_length));
		var       data = new float[len];

		var input_coef            = new float[3];
		var prev_sample_coef      = new float[3];
		var prev_prev_sample_coef = new float[3];
		var prev_samples          = new float[3];
		var prev_prev_samples     = new float[3];
		// y = a * x + b * y1 + c * y2

		for (int k = 0; k < 3; ++k) {
			float formant            = FORMANTS[vowel][k] * this.formant_shift;
			prev_prev_sample_coef[k] = -Mathf.Exp(-2F * Mathf.PI * BANDWIDTHS[k] / rate);
			prev_sample_coef[k]      = 2F * Mathf.Exp(-Mathf.PI * BANDWIDTHS[k] / rate)
									   * Mathf.Cos(2F * Mathf.PI * formant / rate);
			input_coef[k]            = 1F - prev_sample_coef[k] - prev_prev_sample_coef[k];
		}

		float phase = 0F, peak = 0.0001F;
		for (int i = 0; i < len; ++i) {
			float time             = (float)i / rate;
			float fundamental_freq = this.voice_pitch * (1F
						+ this.wobble * Mathf.Sin(2F * Mathf.PI * this.wobble_rate * time));
			phase += fundamental_freq / rate;
			phase -= Mathf.Floor(phase);

			float excitation = (2F * phase - 1F) * (1F - this.breathiness)
			           + (Random.value * 2F - 1F) * this.breathiness;

			float sample = 0F;
			for (int k = 0; k < 3; ++k) {
				float filtered_sample = input_coef[k] * excitation
				          + prev_sample_coef[k] * prev_samples[k]
						  + prev_prev_sample_coef[k] * prev_prev_samples[k];
				prev_prev_samples[k] = prev_samples[k];
				prev_samples[k] = filtered_sample;
				sample += filtered_sample * GAINS[k];
			}

			// Follow the shape of a vocal wave
			sample  *= Mathf.Min(time / 0.008F, 1F) * Mathf.Pow(1F - (float)i / len, 1.5F);
			data[i]  = sample;
			peak     = Mathf.Max(peak, Mathf.Abs(sample));
		}

		for (int i = 0; i < len; ++i)
			data[i] *= 0.9F / peak;

		var clip = AudioClip.Create($"{this.gameObject.name}_vowel{vowel}", len, 1, rate, false);
		clip.SetData(data, 0);
		return clip;
	}
}
