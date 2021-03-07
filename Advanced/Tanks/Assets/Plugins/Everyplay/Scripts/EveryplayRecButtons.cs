using UnityEngine;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;

public class EveryplayRecButtons : MonoBehaviour
{
	public enum ButtonsOrigin
	{
		TopLeft = 0,
		TopRight,
		BottomLeft,
		BottomRight
	}

	public Texture2D atlasTexture;
	public ButtonsOrigin origin = ButtonsOrigin.TopLeft;
	public Vector2 containerMargin = new Vector2(16, 16);
	private Vector2 containerOffset = Vector2.zero;
	private float containerScaling = 1.0f;
	private const int atlasWidth = 256;
	private const int atlasHeight = 256;
	private const int atlasPadding = 2;
	private int buttonTitleHorizontalMargin = 16;
	private int buttonTitleVerticalMargin = 20;
	private int buttonMargin = 8;
	private bool faceCamPermissionGranted = false;
	private bool startFaceCamWhenPermissionGranted = false;

	private class TextureAtlasSrc
	{
		public Rect atlasRect;
		public Rect normalizedAtlasRect;

		public TextureAtlasSrc(int width, int height, int x, int y, float scale)
		{
			atlasRect = new Rect(x + atlasPadding, y + atlasPadding, width * scale, height * scale);
			normalizedAtlasRect = new Rect(atlasRect.x / (float)atlasWidth, 1.0f - (atlasRect.y + height) / (float)atlasHeight, width / (float)atlasWidth, height / (float)atlasHeight);
		}
	}

	private delegate void ButtonTapped();

	private class Button
	{
		public bool enabled;
		public Rect screenRect;
		public TextureAtlasSrc bg;
		public TextureAtlasSrc title;
		public ButtonTapped onTap;

		public Button(TextureAtlasSrc bg, TextureAtlasSrc title, ButtonTapped buttonTapped)
		{
			this.enabled = true;
			this.bg = bg;
			this.title = title;
			this.screenRect.width = bg.atlasRect.width;
			this.screenRect.height = bg.atlasRect.height;
			this.onTap = buttonTapped;
		}
	}

	private class ToggleButton : Button
	{
		public TextureAtlasSrc toggleOn;
		public TextureAtlasSrc toggleOff;
		public bool toggled = false;

		public ToggleButton(TextureAtlasSrc bg, TextureAtlasSrc title, ButtonTapped buttonTapped, TextureAtlasSrc toggleOn, TextureAtlasSrc toggleOff)
			: base(bg, title, buttonTapped)
		{
			this.toggleOn = toggleOn;
			this.toggleOff = toggleOff;
		}
	}

	private TextureAtlasSrc editVideoAtlasSrc;
	private TextureAtlasSrc faceCamAtlasSrc;
	private TextureAtlasSrc openEveryplayAtlasSrc;
	private TextureAtlasSrc shareVideoAtlasSrc;
	private TextureAtlasSrc startRecordingAtlasSrc;
	private TextureAtlasSrc stopRecordingAtlasSrc;
	private TextureAtlasSrc facecamToggleOnAtlasSrc;
	private TextureAtlasSrc facecamToggleOffAtlasSrc;
	private TextureAtlasSrc bgHeaderAtlasSrc;
	private TextureAtlasSrc bgFooterAtlasSrc;
	private TextureAtlasSrc bgAtlasSrc;
	private TextureAtlasSrc buttonAtlasSrc;
	private Button shareVideoButton;
	private Button editVideoButton;
	private Button openEveryplayButton;
	private Button startRecordingButton;
	private Button stopRecordingButton;
	private ToggleButton faceCamToggleButton;
	private Button tappedButton = null;
	private List<Button> visibleButtons;

	void Awake()
	{
		// Scale by resolution
		containerScaling = GetScalingByResolution();

		// Texture atlas sources
		editVideoAtlasSrc = new TextureAtlasSrc(112, 19, 0, 0, containerScaling);
		faceCamAtlasSrc = new TextureAtlasSrc(103, 19, 116, 0, containerScaling);
		openEveryplayAtlasSrc = new TextureAtlasSrc(178, 23, 0, 23, containerScaling);
		shareVideoAtlasSrc = new TextureAtlasSrc(134, 19, 0, 50, containerScaling);
		startRecordingAtlasSrc = new TextureAtlasSrc(171, 23, 0, 73, containerScaling);
		stopRecordingAtlasSrc = new TextureAtlasSrc(169, 23, 0, 100, containerScaling);
		facecamToggleOnAtlasSrc = new TextureAtlasSrc(101, 42, 0, 127, containerScaling);
		facecamToggleOffAtlasSrc = new TextureAtlasSrc(101, 42, 101, 127, containerScaling);
		bgHeaderAtlasSrc = new TextureAtlasSrc(256, 9, 0, 169, containerScaling);
		bgFooterAtlasSrc = new TextureAtlasSrc(256, 9, 0, 169, containerScaling);
		bgAtlasSrc = new TextureAtlasSrc(256, 6, 0, 178, containerScaling);
		buttonAtlasSrc = new TextureAtlasSrc(220, 64, 0, 190, containerScaling);

		buttonTitleHorizontalMargin = Mathf.RoundToInt(buttonTitleHorizontalMargin * containerScaling);
		buttonTitleVerticalMargin = Mathf.RoundToInt(buttonTitleVerticalMargin * containerScaling);
		buttonMargin = Mathf.RoundToInt(buttonMargin * containerScaling);

		// Buttons
		shareVideoButton = new Button(buttonAtlasSrc, shareVideoAtlasSrc, ShareVideo);
		editVideoButton = new Button(buttonAtlasSrc, editVideoAtlasSrc, EditVideo);
		openEveryplayButton = new Button(buttonAtlasSrc, openEveryplayAtlasSrc, OpenEveryplay);
		startRecordingButton = new Button(buttonAtlasSrc, startRecordingAtlasSrc, StartRecording);
		stopRecordingButton = new Button(buttonAtlasSrc, stopRecordingAtlasSrc, StopRecording);
		faceCamToggleButton = new ToggleButton(buttonAtlasSrc, faceCamAtlasSrc, FaceCamToggle, facecamToggleOnAtlasSrc, facecamToggleOffAtlasSrc);

		visibleButtons = new List<Button>();

		// Use header texture for footer by flipping it
		bgFooterAtlasSrc.normalizedAtlasRect.y = bgFooterAtlasSrc.normalizedAtlasRect.y + bgFooterAtlasSrc.normalizedAtlasRect.height;
		bgFooterAtlasSrc.normalizedAtlasRect.height = -bgFooterAtlasSrc.normalizedAtlasRect.height;

		// Set initially visible buttons
		SetButtonVisible(startRecordingButton, true);
		SetButtonVisible(openEveryplayButton, true);
		SetButtonVisible(faceCamToggleButton, true);

		// Set initially
		if (!Everyplay.IsRecordingSupported())
		{
			startRecordingButton.enabled = false;
			stopRecordingButton.enabled = false;
		}

		Everyplay.RecordingStarted += RecordingStarted;
		Everyplay.RecordingStopped += RecordingStopped;
		Everyplay.ReadyForRecording += ReadyForRecording;
		Everyplay.FaceCamRecordingPermission += FaceCamRecordingPermission;
	}

	void Destroy()
	{
		Everyplay.RecordingStarted -= RecordingStarted;
		Everyplay.RecordingStopped -= RecordingStopped;
		Everyplay.ReadyForRecording -= ReadyForRecording;
		Everyplay.FaceCamRecordingPermission -= FaceCamRecordingPermission;
	}

	private void SetButtonVisible(Button button, bool visible)
	{
		if (visibleButtons.Contains(button))
		{
			if (!visible)
			{
				visibleButtons.Remove(button);
			}
		}
		else
		{
			if (visible)
			{
				visibleButtons.Add(button);
			}
		}
	}

	private void ReplaceVisibleButton(Button button, Button replacementButton)
	{
		int index = visibleButtons.IndexOf(button);
		if (index >= 0)
		{
			visibleButtons[index] = replacementButton;
		}
	}

	private void StartRecording()
	{
		Everyplay.StartRecording();
	}

	private void StopRecording()
	{
		Everyplay.StopRecording();
	}

	private void RecordingStarted()
	{
		ReplaceVisibleButton(startRecordingButton, stopRecordingButton);

		SetButtonVisible(shareVideoButton, false);
		SetButtonVisible(editVideoButton, false);
		SetButtonVisible(faceCamToggleButton, false);
	}

	private void RecordingStopped()
	{
		ReplaceVisibleButton(stopRecordingButton, startRecordingButton);

		SetButtonVisible(shareVideoButton, true);
		SetButtonVisible(editVideoButton, true);
		SetButtonVisible(faceCamToggleButton, true);
	}

	private void ReadyForRecording(bool enabled)
	{
		startRecordingButton.enabled = enabled;
		stopRecordingButton.enabled = enabled;
	}

	private void FaceCamRecordingPermission(bool granted)
	{
		faceCamPermissionGranted = granted;

		if (startFaceCamWhenPermissionGranted)
		{
			faceCamToggleButton.toggled = granted;
			Everyplay.FaceCamStartSession();
			if (Everyplay.FaceCamIsSessionRunning())
			{
				startFaceCamWhenPermissionGranted = false;
			}
		}
	}

	private void FaceCamToggle()
	{
		if (faceCamPermissionGranted)
		{
			faceCamToggleButton.toggled = !faceCamToggleButton.toggled;

			if (faceCamToggleButton.toggled)
			{
				if (!Everyplay.FaceCamIsSessionRunning())
				{
					Everyplay.FaceCamStartSession();
				}
			}
			else
			{
				if (Everyplay.FaceCamIsSessionRunning())
				{
					Everyplay.FaceCamStopSession();
				}
			}
		}
		else
		{
			Everyplay.FaceCamRequestRecordingPermission();
			startFaceCamWhenPermissionGranted = true;
		}
	}

	private void OpenEveryplay()
	{
		Everyplay.Show();
	}

	private void EditVideo()
	{
		Everyplay.PlayLastRecording();
	}

	private void ShareVideo()
	{
		Everyplay.ShowSharingModal();
	}

	void Update()
	{
		#if UNITY_EDITOR || UNITY_STANDALONE
		if (Input.GetMouseButtonDown(0))
		{
			foreach (Button button in visibleButtons)
			{
				if (button.enabled && button.screenRect.Contains(new Vector2(Input.mousePosition.x - containerOffset.x, Screen.height - Input.mousePosition.y - containerOffset.y)))
				{
					tappedButton = button;
				}
			}
		}
		else if (Input.GetMouseButtonUp(0))
		{
			foreach (Button button in visibleButtons)
			{
				if (button.enabled && button.screenRect.Contains(new Vector2(Input.mousePosition.x - containerOffset.x, Screen.height - Input.mousePosition.y - containerOffset.y)))
				{
					if (button.onTap != null)
					{
						button.onTap();
					}
				}
			}

			tappedButton = null;
		}
		#else
        foreach (Touch touch in Input.touches)
        {
            if (touch.phase == TouchPhase.Began)
            {
                foreach (Button button in visibleButtons)
                {
                    if (button.screenRect.Contains(new Vector2(touch.position.x - containerOffset.x, Screen.height - touch.position.y - containerOffset.y)))
                    {
                        tappedButton = button;
                    }
                }
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                foreach (Button button in visibleButtons)
                {
                    if (button.screenRect.Contains(new Vector2(touch.position.x - containerOffset.x, Screen.height - touch.position.y - containerOffset.y)))
                    {
                        if (button.onTap != null)
                        {
                            button.onTap();
                        }
                    }
                }
                tappedButton = null;
            }
            else if (touch.phase == TouchPhase.Canceled)
            {
                tappedButton = null;
            }
        }
		#endif
	}

	void OnGUI()
	{
		if (Event.current.type.Equals(EventType.Repaint))
		{
			int containerHeight = CalculateContainerHeight();
			UpdateContainerOffset(containerHeight);
			DrawBackround(containerHeight);
			DrawButtons();
		}
	}

	private void DrawTexture(float x, float y, float width, float height, Texture2D texture, Rect uvRect)
	{
		x += containerOffset.x;
		y += containerOffset.y;
		GUI.DrawTextureWithTexCoords(new Rect(x, y, width, height), texture, uvRect, true);
	}

	private void DrawButtons()
	{
		foreach (Button button in visibleButtons)
		{
			if (button.enabled)
			{
				DrawButton(button, (tappedButton == button) ? Color.gray : Color.white);
			}
			else
			{
				DrawButton(button, new Color(0.5f, 0.5f, 0.5f, 0.3f));
			}
		}
	}

	private void DrawBackround(int containerHeight)
	{
		DrawTexture(0, 0, bgHeaderAtlasSrc.atlasRect.width, bgHeaderAtlasSrc.atlasRect.height, atlasTexture, bgHeaderAtlasSrc.normalizedAtlasRect);
		DrawTexture(0, bgHeaderAtlasSrc.atlasRect.height, bgAtlasSrc.atlasRect.width, containerHeight - bgHeaderAtlasSrc.atlasRect.height - bgFooterAtlasSrc.atlasRect.height, atlasTexture, bgAtlasSrc.normalizedAtlasRect);
		DrawTexture(0, containerHeight - bgFooterAtlasSrc.atlasRect.height, bgFooterAtlasSrc.atlasRect.width, bgFooterAtlasSrc.atlasRect.height, atlasTexture, bgFooterAtlasSrc.normalizedAtlasRect);
	}

	private void DrawButton(Button button, Color tintColor)
	{
		Color oldColor = GUI.color;
		bool isToggleButton = false;
#if NETFX_CORE
        isToggleButton = typeof(ToggleButton).GetTypeInfo().IsAssignableFrom(button.GetType().GetTypeInfo());
#else
		isToggleButton = typeof(ToggleButton).IsAssignableFrom(button.GetType());
#endif

		if (isToggleButton)
		{
			ToggleButton toggleButton = (ToggleButton)button;

			if (button != null)
			{
				float x = (button.screenRect.x + button.screenRect.width) - toggleButton.toggleOn.atlasRect.width;
				float y = button.screenRect.y + button.screenRect.height / 2 - toggleButton.toggleOn.atlasRect.height / 2;

				TextureAtlasSrc buttonSrc = toggleButton.toggled ? toggleButton.toggleOn : toggleButton.toggleOff;

				GUI.color = tintColor;
				DrawTexture(x, y, buttonSrc.atlasRect.width, buttonSrc.atlasRect.height, atlasTexture, buttonSrc.normalizedAtlasRect);
				GUI.color = oldColor;
			}
		}
		else
		{
			GUI.color = tintColor;
			DrawTexture(button.screenRect.x, button.screenRect.y, button.bg.atlasRect.width, button.bg.atlasRect.height, atlasTexture, button.bg.normalizedAtlasRect);
			GUI.color = oldColor;
		}

		float rightMargin = isToggleButton ? 0 : buttonTitleHorizontalMargin;

		if (!button.enabled)
		{
			GUI.color = tintColor;
		}

		DrawTexture(button.screenRect.x + rightMargin, button.screenRect.y + buttonTitleVerticalMargin, button.title.atlasRect.width, button.title.atlasRect.height, atlasTexture, button.title.normalizedAtlasRect);

		if (!button.enabled)
		{
			GUI.color = oldColor;
		}
	}

	private int CalculateContainerHeight()
	{
		float buttonsHeight = 0;
		float yOffset = bgHeaderAtlasSrc.atlasRect.height + (buttonMargin * 2 - bgHeaderAtlasSrc.atlasRect.height);

		foreach (Button button in visibleButtons)
		{
			button.screenRect.x = (bgAtlasSrc.atlasRect.width - button.screenRect.width) / 2;
			button.screenRect.y = yOffset;
			yOffset += buttonMargin + button.screenRect.height;
			buttonsHeight += buttonMargin + button.screenRect.height;
		}

		int containerHeight = Mathf.RoundToInt(buttonsHeight + bgHeaderAtlasSrc.atlasRect.height + bgFooterAtlasSrc.atlasRect.height);

		return containerHeight;
	}

	private void UpdateContainerOffset(int containerHeight)
	{
		if (origin == ButtonsOrigin.TopRight)
		{
			containerOffset.x = Screen.width - containerMargin.x * containerScaling - bgAtlasSrc.atlasRect.width;
			containerOffset.y = containerMargin.y * containerScaling;
		}
		else if (origin == ButtonsOrigin.BottomLeft)
		{
			containerOffset.x = containerMargin.x * containerScaling;
			containerOffset.y = Screen.height - containerMargin.y * containerScaling - containerHeight;
		}
		else if (origin == ButtonsOrigin.BottomRight)
		{
			containerOffset.x = Screen.width - containerMargin.x * containerScaling - bgAtlasSrc.atlasRect.width;
			containerOffset.y = Screen.height - containerMargin.y * containerScaling - containerHeight;
		}
		else
		{
			containerOffset.x = containerMargin.x * containerScaling;
			containerOffset.y = containerMargin.y * containerScaling;
		}
	}

	private float GetScalingByResolution()
	{
		int high = Mathf.Max(Screen.height, Screen.width);
		int low = Mathf.Min(Screen.height, Screen.width);

		if (high < 640 || (high == 1024 && low == 768))
		{
			return 0.5f;
		}

		return 1.0f;
	}
}
