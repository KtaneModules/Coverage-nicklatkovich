using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class CoverageModule : MonoBehaviour {
	private static int moduleIdCounter = 1;

	public readonly string TwitchHelpMessage = "\"!{0} submit 01\" - submit answer. Should be 2-digits answer. Word \"submit\" is optional";

	public TextMesh ScreenText;
	public Renderer BackgroundRenderer;
	public KMNeedyModule Needy;
	public KMSelectable[] Numbers;
	public KMSelectable ClearButton;
	public KMSelectable SubmitButton;
	public KMAudio Audio;

	private bool onceActivated = false;
	private bool activated = false;
	private int moduleId;
	private int expectedAnswer;
	private float activationTime;
	private string question;
	private string answer = "";

	private void Start() {
		moduleId = moduleIdCounter++;
		Needy.OnActivate += OnActivate;
	}

	private void Update() {
		Color warningColor = Color.green;
		if (!onceActivated) warningColor = new Color(1f, Mathf.Sin(Time.time * Mathf.PI) * 0.5f + 0.5f, 0f);
		else if (activated) warningColor = new Color(1f, Mathf.Sin(Mathf.PI * (activationTime + Mathf.Pow(Time.time - activationTime, 1.2f))) * 0.5f + 0.5f, 0f);
		BackgroundRenderer.material.SetColor("_UnlitTint", warningColor);
	}

	private void OnActivate() {
		Needy.OnNeedyActivation += OnNeedyActivation;
		Needy.OnNeedyDeactivation += OnNeedyDeactivation;
		Needy.OnTimerExpired += OnTimerExpired;
		for (int i = 0; i < 10; i++) {
			int j = i;
			Numbers[i].OnInteract += () => { PressDigit(j); return false; };
		}
		ClearButton.OnInteract += () => { RemoveDigit(); return false; };
		SubmitButton.OnInteract += () => { Submit(); return false; };
	}

	private void OnNeedyActivation() {
		activationTime = Time.time;
		onceActivated = true;
		activated = true;
		answer = "";
		int ww = Random.Range(1, 10);
		int hh = Random.Range(1, 10);
		expectedAnswer = ww * hh;
		int a = Random.Range(2, 10);
		int w = Random.Range(a * (ww - 1) + 1, a * ww);
		int h = Random.Range(a * (hh - 1) + 1, a * hh);
		question = string.Format("w:{0} | a:{1}\nh:{2} | x:", w.ToString().PadRight(2, ' '), a.ToString().PadRight(2, ' '), h.ToString().PadRight(2, ' '));
		Debug.LogFormat("[Coverage #{0}] w:{1}, h:{2}, a:{3}", moduleId, w, h, a);
		Debug.LogFormat("[Coverage #{0}] Expected answer: {1}", moduleId, expectedAnswer.ToString().PadLeft(2, '0'));
		UpdateText();
	}

	private void OnNeedyDeactivation() {
		activated = false;
		ScreenText.text = "";
		answer = "";
	}

	private void OnTimerExpired() {
		Needy.HandleStrike();
		OnNeedyDeactivation();
	}

	private void PressDigit(int digit) {
		if (answer.Length >= 2) return;
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		answer = answer + digit.ToString();
		UpdateText();
	}

	private void RemoveDigit() {
		if (answer.Length == 0) return;
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		answer = answer.Substring(0, answer.Length - 1);
		UpdateText();
	}

	private void Submit() {
		if (!activated) return;
		if (answer.Length < 2) return;
		if (answer != expectedAnswer.ToString().PadLeft(2, '0')) {
			Debug.LogFormat("[Coverage #{0}] Submitted: {1}. Strike", moduleId, answer);
			Needy.HandleStrike();
		} else {
			Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
			Debug.LogFormat("[Coverage #{0}] Submitted: {1} ({2}s remaining). Solved", moduleId, answer, Needy.GetNeedyTimeRemaining().ToString("n2"));
			Needy.HandlePass();
			ScreenText.text = "";
			OnNeedyDeactivation();
		}
	}

	public IEnumerator ProcessTwitchCommand(string command) {
		command = command.Trim().ToLower();
		if (command.StartsWith("submit ")) command = command.Split(' ').Skip(1).Join(" ");
		if (Regex.IsMatch(command, @"^\d+$")) {
			if (command.Length != 2) {
				yield return "sendtochat {0}, !{1} should be 2-digits answer";
				yield break;
			}
			yield return null;
			if (!activated) {
				yield return "sendtochat {0}, !{1} not activated";
				yield break;
			}
			while (answer != command.Substring(0, answer.Length)) yield return new[] { ClearButton };
			if (answer.Length == 0) yield return new[] { Numbers[command[0] - '0'] };
			if (answer.Length == 1) yield return new[] { Numbers[command[1] - '0'] };
			yield return new[] { SubmitButton };
			yield break;
		}
	}

	private IEnumerator TwitchHandleForcedSolve() {
		yield return null;
		if (!activated) yield break;
		IEnumerator action = ProcessTwitchCommand(expectedAnswer.ToString().PadLeft(2, '0'));
		while (action.MoveNext()) {
			KMSelectable[] selectables = action.Current as KMSelectable[];
			if (selectables == null) continue;
			foreach (KMSelectable selectable in selectables) {
				selectable.OnInteract();
				yield return new WaitForSeconds(.1f);
			}
		}
	}

	private void UpdateText() {
		ScreenText.text = activated ? question + answer.PadRight(2, '_') : "";
	}
}
