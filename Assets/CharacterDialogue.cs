// Put per character, pre loads and suchlike :3c

using System.Collections;
using UnityEngine;
using TMPro;

public class CharacterDialogue : MonoBehaviour {
	public TMP_Text text_box;

	[Header("Acting")]
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
	bool      skip;
	Coroutine routine;
	int       vowel_count;
	char      prev_char;

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
			if (this.IsVowel(c) && !this.IsVowel(this.prev_char) && ++this.vowel_count % this.blip_every == 0)
				this.Blip(c);
			this.prev_char = c;
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

	bool IsVowel(char c) {
		switch (char.ToLowerInvariant(c)) {
			case 'a': case 'e': case 'i': case 'o': case 'u': case 'y':
				return true;
		}
		return false;
	}

	void Blip(char c) {
		if (this.voice == null)
			return;

		this.voice.pitch = Random.Range(this.pitch_min, this.pitch_max);
		this.voice.PlayOneShot(this.vowel_clips[this.VowelIndex(c)], this.blip_volume);
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
