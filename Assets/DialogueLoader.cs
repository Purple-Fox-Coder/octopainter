// pre load dialogue

using UnityEngine;

public static class DialogueLoader
{
	public static string[] Load(string filename) {
		var asset = Resources.Load<TextAsset>($"Dialogue/{filename}");

		if (asset == null) {
			Debug.LogWarning($"Could not load: Dialogue/{filename}");
			return new string[0];
		}

		return asset.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
	}
}
