using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public partial class MissionsManager : MonoBehaviour
{

	private void Awake()
	{
		this.displayBlockList = true;
		this.displayConnectionBox = false;
		this.connectionBoxContent = "";
		this.displayChat = true;
		this.VoscriptWindowData = new Dictionary<int, List<string>>();
		double num = (double)Mathf.Min((float)Screen.height / 1.4f, 750f);
		double num2 = (double)Mathf.Min((float)Screen.height / 1.2f, 600f) / 1.3;
		this.DataEntered = false;
		this.loginRect = new Rect((float)(Screen.width / 2) - (float)(num / 2.0), (float)(Screen.height / 2) - (float)(num2 / 2.0), (float)num, (float)num2);
		this.enteredServer = "play.vopwn55.xyz";
		this.enteredPassword = "Optional";
		this.enteredPort = "8880";
		this.enteredUsername = "Username";
		this.enteredInChat = "Tap here to chat";
		try
		{
			this.enteredServer = PlayerPrefs.GetString("enteredServer", "play.vopwn55.xyz");
			this.enteredPassword = PlayerPrefs.GetString("enteredPassword", "Optional");
			this.enteredPort = PlayerPrefs.GetString("enteredPort", "8880");
			this.enteredUsername = PlayerPrefs.GetString("enteredUsername", "Guest" + new System.Random().Next(1, 99999));
		}
		catch
		{
		}
		Useful.UseMissionsMng = this;
		this.scaleCpHighlight = this.CpHighlight.transform.localScale.x;
		UnlockAcquisition[] unlocks = this.Unlocks;
		for (int i = 0; i < unlocks.Length; i++)
		{
			unlocks[i].Cost += 317692;
		}
		this.SkipMissionFee += 317692;
	}
	
	private void ChatRenderer(int windowId)
	{
		double num = (double)Mathf.Min((float)Screen.height / 1.2f, 600f) / 1.3;
		GUIStyle label = GUI.skin.label;
		label.alignment = TextAnchor.LowerLeft;
		label.fontSize = Convert.ToInt32(num / 20.0);
		GUIStyle textField = GUI.skin.textField;
		textField.fontSize = Convert.ToInt32(num / 20.0);
		GUIStyle button = GUI.skin.button;
		button.fontSize = Convert.ToInt32(num / 20.0);
		float num2 = (float)Screen.height / 3.4f;
		float num3 = num2 * 1.333f;
		float num4 = num2 / 8f;
		float num5 = num4 * 2f;
		this.enteredInChat = GUI.TextField(new Rect(0f, num2 - num4, num3 - num5, num4), this.enteredInChat, textField);
		GUI.Label(new Rect(0f, 0f, num3, num2 - num4), Useful.UseMultiplayer.lastServerChat, label);
		if (GUI.Button(new Rect(num3 - num5, num2 - num4, num5, num4), "Send", button))
		{
			Useful.UseMultiplayer.chatPendingSend = this.enteredInChat;
			this.enteredInChat = "";
		}
	}
	
	public int CreatePopupWindow(string message, string closeButtonText)
	{
		int smallestFreeKey = this.GetSmallestFreeKey(this.VoscriptWindowData);
		this.VoscriptWindowData.Add(smallestFreeKey, new List<string>
		{
			message,
			closeButtonText
		});
		return smallestFreeKey;
	}
	
	private void LoginWindowFunction(int windowID)
	{
		this.guiStyle = new GUIStyle();
		this.labelStyle = GUI.skin.label;
		this.inputStyle = GUI.skin.textField;
		this.buttonStyle = GUI.skin.button;
		this.toggleStyle = GUI.skin.toggle;
		double num = (double)Mathf.Min((float)Screen.height / 1.2f, 600f) / 1.3;
		this.guiStyle.fontSize = Convert.ToInt32(num / 8.0);
		this.labelStyle.fontSize = Convert.ToInt32(num / 14.0);
		this.inputStyle.fontSize = Convert.ToInt32(num / 14.0);
		this.buttonStyle.fontSize = Convert.ToInt32(num / 14.0);
		this.toggleStyle.fontSize = Convert.ToInt32(num / 14.0);
		this.enteredUsername = GUI.TextField(new Rect(10f + this.getTextFieldExplainerWidth(), this.getTextFieldY(1) + this.loginRect.height / 14f, this.getTextFieldWidth(), this.loginRect.height / 8f), Regex.Replace(this.enteredUsername, "[^a-zA-Z0-9]", ""), this.inputStyle);
		GUI.Label(new Rect(10f, this.getTextFieldY(1) + this.loginRect.height / 13f, this.getTextFieldExplainerWidth(), this.loginRect.height / 7f), "Username", this.labelStyle);
		if (GUI.Button(new Rect(10f, this.getTextFieldY(1) + this.loginRect.height / 4.6f, this.loginRect.width / 4f, this.loginRect.height / 12f), "Advanced...", this.buttonStyle))
		{
			this.advancedMode = !this.advancedMode;
		}
		if (this.advancedMode)
		{
			this.enteredServer = GUI.TextField(new Rect(10f + this.getTextFieldExplainerWidth(), this.getTextFieldY(2) + this.loginRect.height / 6.9f, this.getTextFieldWidth(), this.loginRect.height / 8f), this.enteredServer, this.inputStyle);
			this.enteredPort = GUI.TextField(new Rect(10f + this.getTextFieldExplainerWidth(), this.getTextFieldY(3) + this.loginRect.height / 7.9f, this.getTextFieldWidth(), this.loginRect.height / 8f), this.enteredPort, this.inputStyle);
			this.enteredPassword = GUI.TextField(new Rect(10f + this.getTextFieldExplainerWidth(), this.getTextFieldY(4) + this.loginRect.height / 8.8f, this.getTextFieldWidth(), this.loginRect.height / 8f), this.enteredPassword, this.inputStyle);
			GUI.Label(new Rect(10f, this.getTextFieldY(2) + this.loginRect.height / 6.9f, this.getTextFieldExplainerWidth(), this.loginRect.height / 7f), "Server", this.labelStyle);
			GUI.Label(new Rect(10f, this.getTextFieldY(3) + this.loginRect.height / 7.9f, this.getTextFieldExplainerWidth(), this.loginRect.height / 7f), "Port", this.labelStyle);
			GUI.Label(new Rect(10f, this.getTextFieldY(4) + this.loginRect.height / 8.8f, this.getTextFieldExplainerWidth(), this.loginRect.height / 7f), "Password", this.labelStyle);
		}
		else
		{
			this.labelStyle.wordWrap = true;
			GUI.Label(new Rect(10f, this.getTextFieldY(2) + this.loginRect.height / 6.9f, this.loginRect.width - 20f, this.loginRect.height / 2f), "Only Latin letters (a-Z) and numbers (0-9) allowed in username. Username must be 3-20 characters. Do not use spaces or underscores.");
		}
		if (GUI.Button(new Rect(this.loginRect.width - 10f - this.loginRect.width / 2f - 20f, 50f + 5f * (this.loginRect.height / 7f), this.loginRect.width / 2f - 20f, this.loginRect.height / 7f), "Connect", this.buttonStyle))
		{
			try
			{
				PlayerPrefs.SetString("enteredServer", this.enteredServer);
				PlayerPrefs.SetString("enteredPassword", this.enteredPassword);
				PlayerPrefs.SetString("enteredPort", this.enteredPort);
				PlayerPrefs.SetString("enteredUsername", this.enteredUsername);
			}
			catch
			{
			}
			this.DataEntered = true;
			this.multiplayer = new Multiplayer();
			this.multiplayer.broadcastPosition = new Vector3(0f, 5000f, 0f);
			this.multiplayer.planeType = "C-400";
			this.multiplayer.ConnectToServer("192.168.1.194", 49055, this);
		}
	}
	
	private void OnGUI()
	{
		if (!this.DataEntered)
		{
			double num = (double)Mathf.Min((float)Screen.height / 1.2f, 600f) / 1.3;
			this.windowStyle = GUI.skin.window;
			this.windowStyle.fontSize = Convert.ToInt32(num / 14.0);
			this.loginRect = GUI.Window(0, this.loginRect, new GUI.WindowFunction(this.LoginWindowFunction), "Connect to TFSMP", this.windowStyle);
		}
		try
		{
			foreach (KeyValuePair<int, List<string>> keyValuePair in this.VoscriptWindowData)
			{
				Debug.Log("rendering window " + keyValuePair.Key);
				float num2 = this.labelStyle.CalcHeight(new GUIContent(keyValuePair.Value[0]), 370f) + 45f + 75f;
				float width = 400f;
				Debug.Log("calculated size");
				Rect clientRect = new Rect((float)(Screen.width / 2 - 200), (float)(Screen.height / 2) - num2 / 2f, width, num2);
				Debug.Log("spawning window");
				GUI.Window(keyValuePair.Key + 1, clientRect, new GUI.WindowFunction(this.VoscriptWindowRenderer), " ", this.windowStyle);
			}
		}
		catch (Exception ex)
		{
			Debug.Log("failed to create voscript window: " + ex.Message + "\nstack trace:\n" + ex.StackTrace);
		}
		float num3 = (float)Screen.height / 3.4f;
		float num4 = num3 * 1.333f;
		float num5 = num3 / 8f;
		float num6 = (float)(Screen.height / 32);
		float num7 = (float)Screen.width / 2f - num4 / 2f;
		Rect clientRect2 = new Rect(num7, num6, num4, num3);
		if (this.CrtMission != null && this.CrtMission.TypeOfMission == Mission.TypeGameplay.FreeFlight && this.displayChat)
		{
			string text = "Show Blocklist";
			if (this.displayBlockList)
			{
				text = "Hide Blocklist";
			}
			if (GUI.Button(new Rect(num7 + num4 + num4 / 3f, num6, num4 / 2f, num5), text, this.buttonStyle))
			{
				this.displayBlockList = !this.displayBlockList;
			}
			GUI.Window(1, clientRect2, new GUI.WindowFunction(this.ChatRenderer), " ", this.windowStyle);
			int num8 = 1;
			if (this.displayBlockList)
			{
				foreach (KeyValuePair<string, Multiplayer.Pair<Mission.MsObj, GameObject>> keyValuePair2 in Useful.UseMultiplayer.RenderedPrefabs)
				{
					bool flag = Useful.UseMultiplayer.playersToBlock.Contains(keyValuePair2.Key);
					string text2 = "Block " + keyValuePair2.Key;
					if (flag)
					{
						text2 = "Unblock " + keyValuePair2.Key;
					}
					if (GUI.Button(new Rect(num7 + num4, num6 + (float)num8 * num5, num4 / 1.75f, num5), text2, this.buttonStyle))
					{
						if (Useful.UseMultiplayer.playersToBlock.Contains(keyValuePair2.Key))
						{
							Useful.UseMultiplayer.playersToBlock.Remove(keyValuePair2.Key);
						}
						else
						{
							Useful.UseMultiplayer.playersToBlock.Add(keyValuePair2.Key);
						}
					}
					num8++;
				}
			}
		}
		if (this.CrtMission != null && this.CrtMission.TypeOfMission == Mission.TypeGameplay.FreeFlight)
		{
			string text3 = "Show Chat";
			if (this.displayChat)
			{
				text3 = "Hide Chat";
			}
			if (GUI.Button(new Rect(num7 + num4, num6, num4 / 3f, num5), text3, this.buttonStyle))
			{
				this.displayChat = !this.displayChat;
			}
		}
		if (this.displayConnectionBox)
		{
			float num9 = this.labelStyle.CalcHeight(new GUIContent(this.connectionBoxContent), 370f) + 30f;
			float num10 = 400f;
			Rect position = new Rect((float)(Screen.width / 2 - 200), (float)(Screen.height / 2) - num9 / 2f, num10, num9);
			Rect position2 = new Rect((float)(Screen.width / 2 - 200) + 15f, (float)(Screen.height / 2) - num9 / 2f + 15f, num10 - 30f, num9 - 30f);
			GUI.Box(position, "");
			GUI.Label(position2, this.connectionBoxContent, this.labelStyle);
		}
	}
	
	private void VoscriptWindowRenderer(int fakeWindowId)
	{
		int key = fakeWindowId - 1;
		if (!this.VoscriptWindowData.ContainsKey(key))
		{
			Debug.Log("window doesnt exist");
			return;
		}
		List<string> list = this.VoscriptWindowData[key];
		float num = this.labelStyle.CalcHeight(new GUIContent(list[0]), 370f);
		GUI.Label(new Rect(15f, 15f, 370f, num), list[0], this.labelStyle);
		if (GUI.Button(new Rect(15f, 30f + num, 370f, 75f), list[1], this.buttonStyle))
		{
			this.VoscriptWindowData.Remove(key);
		}
	}
}
