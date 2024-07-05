using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using SimpleJSON;
using UnityEngine;
public class Multiplayer
{
	public void Initialize()
	{
		this.PlayerMarkers = new Dictionary<string, int>();
		this.PlayersBeingRendered = new List<string>();
		this.currentState = new Dictionary<string, object>();
		this.broadcastPosition = new Vector3(0f, 1000f, 0f);
		this.planeType = "C-400";
		this.broadcastRotation = new Vector3(0f, 0f, 0f);
		this.InProcessing = new Dictionary<string, JSONNode>();
		this.ToDespawn = new List<GameObject>();
		this.RenderedPrefabs = new Dictionary<string, Multiplayer.Pair<Mission.MsObj, GameObject>>();
	}
	public string Vector3ToString(Vector3 inputVar)
	{
		return string.Format("{0},{1},{2}", inputVar.x, inputVar.y, inputVar.z);
	}
	public void ConnectToServer(string ServerIP, int ServerPort, MissionsManager missionsManager)
	{
		this.bracketBuffer = new Dictionary<double, JSONNode>();
		this.playersToBlock = new List<string>();
		this.States = new Dictionary<string, Dictionary<string, object>>();
		this.PersonModels = new Dictionary<string, GameObject>();
		this.lastServerChat = "Connecting to chat...";
		try
		{
			GameObject.Find("PlayMissionsBttn").SetActive(false);
			GameObject.Find("AircraftViewerBttn").SetActive(false);
			GameObject.Find("NextBttn").SetActive(false);
			GameObject.Find("QuitBttn").SetActive(false);
			GameObject.Find("QuitBttn").SetActive(false);
		}
		catch
		{
		}
		this.SizeSuffixes = new string[]
		{
			"bytes",
			"KB",
			"MB",
			"GB",
			"TB",
			"PB",
			"EB",
			"ZB",
			"YB"
		};
		this.InSpeedRaw = 0;
		this.OutSpeedRaw = 0;
		this.BytesInOverTehInterwebz = 0;
		this.BytesOutOverTehInterwebz = 0;
		this.SendMessageToast("Initializing");
		this.stopwatch = new Stopwatch();
		this.stopwatch.Start();
		this.lastServerTime = -9999990.0;
		Useful.UseMultiplayer = this;
		List<object> prefs = this.GetPrefs(ServerIP, ServerPort);
		UnityEngine.Debug.Log("got prefs!!");
		string s = prefs[3].ToString();
		byte[] array = SHA384.Create().ComputeHash(Encoding.UTF8.GetBytes(s));
		StringBuilder stringBuilder = new StringBuilder();
		foreach (byte b in array)
		{
			stringBuilder.Append(b.ToString("x2"));
		}
		stringBuilder.ToString();
		this.currentState["Eng1"] = false;
		this.currentState["Eng2"] = false;
		this.currentState["Eng3"] = false;
		this.currentState["Eng4"] = false;
		this.currentState["GearDown"] = false;
		this.currentState["SigL"] = false;
		this.currentState["MainL"] = false;
		this.currentState["VTOLAngle"] = 10;
		this.currentState["PV40Color"] = "0,0,0";
		this.currentState["LiveryId"] = -1;
		this.missionsManager = missionsManager;
		BackgroundWorker recvWorker = new BackgroundWorker();
		BackgroundWorker backgroundWorker = new BackgroundWorker();
		UnityEngine.Debug.Log("got past security check");
		recvWorker.DoWork += delegate(object sender, DoWorkEventArgs e)
		{
			UnityEngine.Debug.Log("recieve worker invoked");
			for (;;)
			{
				try
				{
					if (this.socket == null || !this.socket.Connected)
					{
						UnityEngine.Debug.Log("connecting");
						while (this.missionsManager.CrtMission == null || this.missionsManager.CrtMission.TypeOfMission != Mission.TypeGameplay.FreeFlight)
						{
							Thread.Sleep(100);
						}
						missionsManager.displayConnectionBox = true;
						missionsManager.connectionBoxContent = string.Concat(new object[]
						{
							"Connecting to server:
",
							ServerIP,
							ServerPort,
							"
Please wait..."
						});
						int num = new System.Random().Next(1000, 9999);
						this.MyUsername = "Guest" + num;
						this.MyUsername = Convert.ToString(prefs[0]);
						ServerIP = Convert.ToString(prefs[1]);
						ServerPort = Convert.ToInt32(prefs[2]);
						UnityEngine.Debug.Log(this.MyUsername);
						UnityEngine.Debug.Log(ServerIP);
						UnityEngine.Debug.Log(ServerPort);
						Controllable crtControllable = missionsManager.CrtMission.CrtControllable;
						if (crtControllable != null)
						{
							this.planeType = this.PrefabNameToTFSMPName(crtControllable.PhysSystem.InfoPrefabName);
						}
						string s2 = string.Concat(new string[]
						{
							"{"NCVersion":3,"NCClient":true,"Username":"",
							this.MyUsername,
							"","PlaneType":"",
							this.planeType,
							""}"
						});
						int num2 = Encoding.UTF8.GetBytes(s2).Length;
						string s3 = string.Concat(new string[]
						{
							"{"NCVersion":3,"NCClient":true,"Username":"",
							this.MyUsername,
							"","PlaneType":"",
							this.planeType,
							""}"
						});
						byte[] bytes = Encoding.UTF8.GetBytes(s3);
						this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
						this.TargetServer = ServerIP + ":" + ServerPort;
						this.socket.Connect(ServerIP, ServerPort);
						this.socket.Send(bytes);
						UnityEngine.Debug.Log("Connected to server");
						missionsManager.displayConnectionBox = false;
						backgroundWorker.RunWorkerAsync();
					}
					UnityEngine.Debug.Log("recieve worker ready");
					while (this.socket.Connected && this.missionsManager.CrtMission.TypeOfMission == Mission.TypeGameplay.FreeFlight)
					{
						UnityEngine.Debug.Log("recv'ing");
						StringBuilder stringBuilder2 = new StringBuilder();
						byte[] array3 = new byte[1024];
						do
						{
							int num3 = this.socket.Receive(array3);
							this.BytesInOverTehInterwebz += num3;
							stringBuilder2.Append(Encoding.UTF8.GetString(array3, 0, num3));
						}
						while (!stringBuilder2.ToString().Contains(""));
						UnityEngine.Debug.Log("recv'ed");
						int num4 = 0;
						int num5 = 0;
						while ((num5 = stringBuilder2.ToString().IndexOf("", num5)) != -1)
						{
							num4++;
							num5 += "".Length;
						}
						if (num4 == 0)
						{
							this.underreportedPacket = stringBuilder2.ToString();
							UnityEngine.Debug.Log("Error: unfinished packet");
						}
						else
						{
							string[] array4 = stringBuilder2.ToString().Split(new char[]
							{
								''
							});
							string text = array4[0];
							if (this.underreportedPacket != null)
							{
								text = this.underreportedPacket + text;
								this.underreportedPacket = "";
							}
							string text2 = array4[array4.Length - 1];
							if (text2.Length > 0)
							{
								UnityEngine.Debug.Log("lost packet in transit");
							}
							this.underreportedPacket = text2;
							if (text.Contains("!!VoscriptPluginData"))
							{
								UnityEngine.Debug.Log("detected voscript data in bracket");
								JSONNode jsonnode = JSON.Parse(text.Replace("", ""));
								if (!jsonnode.HasKey("!!VoscriptPluginData"))
								{
									UnityEngine.Debug.Log("undetected voscript data in bracket! wtf?");
									continue;
								}
								UnityEngine.Debug.Log("executing detected voscripts");
								using (IEnumerator<JSONNode> enumerator = jsonnode["!!VoscriptPluginData"].Children.GetEnumerator())
								{
									while (enumerator.MoveNext())
									{
										JSONNode jsonnode2 = enumerator.Current;
										UnityEngine.Debug.Log(jsonnode2.ToString());
										this.ParseVoscript(jsonnode2.ToString());
									}
									continue;
								}
							}
							recvWorker.ReportProgress(0, text);
							this.pingMS = this.GetCurrentEpoch() - this.lastRecvTime;
							this.lastRecvTime = this.GetCurrentEpoch();
						}
					}
					this.InProcessing.Clear();
					this.RenderedPrefabs.Clear();
				}
				catch (Exception ex)
				{
					missionsManager.displayConnectionBox = true;
					UnityEngine.Debug.Log("Recv connection lost. Reconnecting... Cause: " + ex.Message + "
stack trace:
" + ex.StackTrace);
					this.LastError = "Recv connection lost. Reconnecting... Cause: " + ex.Message + "
stack trace:
" + ex.StackTrace;
					for (int j = 3; j <= 0; j--)
					{
						missionsManager.displayConnectionBox = true;
						missionsManager.connectionBoxContent = string.Concat(new object[]
						{
							"Lost connection to server:
",
							ServerIP,
							ServerPort,
							"
Reconnecting automatically in ",
							j,
							" seconds...
If this error keeps appearing, please check your Internet connection or try changing your username."
						});
						Thread.Sleep(1000);
					}
				}
			}
		};
		backgroundWorker.DoWork += delegate(object sender, DoWorkEventArgs e)
		{
			for (;;)
			{
				try
				{
					if (this.socket.Connected && missionsManager.CrtMission != null && missionsManager.CrtMission.TypeOfMission == Mission.TypeGameplay.FreeFlight)
					{
						Controllable crtControllable = missionsManager.CrtMission.CrtControllable;
						if (crtControllable != null)
						{
							this.broadcastPosition = -Useful.UseWorldContainer.Origin + crtControllable.transform.position;
							this.broadcastRotation = this.normalizeRotationVector(crtControllable.transform.eulerAngles);
							this.planeType = this.PrefabNameToTFSMPName(crtControllable.PhysSystem.InfoPrefabName);
							UnityEngine.Debug.Log("current controllable: " + crtControllable.PhysSystem.InfoPrefabName);
							try
							{
								PersonUserInput component = crtControllable.gameObject.GetComponent<PersonUserInput>();
								if (component != null)
								{
									this.InPersonMode = true;
									if (component.InSeatedMode)
									{
										this.broadcastPosition = Useful.UseFollowCams.MainCamera.transform.position - Useful.UseWorldContainer.Origin - new Vector3(0f, 1f, 0f);
										this.broadcastRotation = component.CamPivotTransform.eulerAngles;
										Vector3 eulerAngles = crtControllable.transform.eulerAngles;
										eulerAngles.y = Useful.UseFollowCams.MainCamera.transform.eulerAngles.y;
										crtControllable.transform.eulerAngles = eulerAngles;
										if (component.SeatedInPhysSystem.gameObject.GetComponent<VehiclePhysics>() == null)
										{
											crtControllable.transform.position = Useful.UseFollowCams.MainCamera.transform.position;
											this.broadcastPosition -= new Vector3(0f, 0f, 0f);
										}
									}
								}
								else
								{
									this.InPersonMode = false;
								}
							}
							catch (Exception ex)
							{
								UnityEngine.Debug.Log("failed to determine where person is seating: " + ex.Message + "
stack trace:
" + ex.StackTrace);
							}
						}
						try
						{
							foreach (Mission.MsObj msObj in missionsManager.CrtMission.MissionObjects)
							{
								if (msObj.FindParam(Mission.MsTypeParam.UserControlled) >= 0 && msObj.GameObjReference == crtControllable.gameObject)
								{
									AircraftPhysics component2 = crtControllable.gameObject.GetComponent<AircraftPhysics>();
									if (component2 != null)
									{
										int num = 1;
										this.currentState["Eng1"] = false;
										this.currentState["Eng2"] = false;
										this.currentState["Eng3"] = false;
										this.currentState["Eng4"] = false;
										foreach (AircraftPhysics.EnginePropFan enginePropFan in component2.Engines)
										{
											bool isShutDown = component2.Engines[num - 1].IsShutDown;
											this.currentState["Eng" + num] = !isShutDown;
											num++;
										}
										this.currentState["GearDown"] = component2.GearDown;
										this.currentState["SigL"] = true;
										this.currentState["MainL"] = false;
										TiltWingPhysics tiltWingPhysics = crtControllable.gameObject.GetComponent<AircraftPhysics>() as TiltWingPhysics;
										if (tiltWingPhysics)
										{
											try
											{
												UnityEngine.Debug.Log("vtol angle: " + tiltWingPhysics.CrtThrustAngle);
												this.currentState["VTOLAngle"] = 360f + tiltWingPhysics.CrtThrustAngle;
											}
											catch (Exception ex2)
											{
												UnityEngine.Debug.Log("failed to get vtol angle for whatever reason: " + ex2.ToString());
											}
										}
										this.currentState["LiveryId"] = -1;
										foreach (Mission.MsParam msParam in msObj.Params)
										{
											if (msParam.TypeParam == Mission.MsTypeParam.Livery)
											{
												UnityEngine.Debug.Log("Livery detected: " + msParam.ParseIntValue());
												this.currentState["LiveryId"] = msParam.ParseIntValue();
											}
											else if (msParam.TypeParam == Mission.MsTypeParam.CustomColor)
											{
												UnityEngine.Debug.Log("custom color detecte");
												string[] array3 = new string[msParam.ParseMultipleFloatValues(null).Length];
												int num2 = 0;
												foreach (float num3 in msParam.ParseMultipleFloatValues(null))
												{
													array3[num2] = num3.ToString();
													num2++;
												}
												this.planeType = "PV-40";
												UnityEngine.Debug.Log(array3.ToString());
												this.currentState["PV40Color"] = string.Join(",", array3);
											}
										}
									}
								}
							}
						}
						catch (Exception ex3)
						{
							UnityEngine.Debug.Log("failed to get state: " + ex3.Message + "
stack trace:
" + ex3.StackTrace);
							this.LastError = "failed to get state: " + ex3.Message + "
stack trace:
" + ex3.StackTrace;
						}
						string format = "{{"PositionService":{{"Position":"{0}","PlaneType":"{1}", "Rotation":"{2}", "State":{3}{4}}}, "ChatService":{{"Pending":"{5}"}}}}";
						string text = "";
						string text2 = string.Format(format, new object[]
						{
							this.Vector3ToString(this.broadcastPosition),
							this.planeType,
							this.Vector3ToString(this.normalizeRotationVector(this.broadcastRotation)),
							this.StateToString(),
							text,
							this.chatPendingSend
						});
						this.chatPendingSend = "";
						int num4 = Encoding.UTF8.GetBytes(text2).Length;
						string s2 = string.Concat(new object[]
						{
							text2
						});
						this.socket.Send(Encoding.UTF8.GetBytes(s2));
						this.BytesOutOverTehInterwebz += Encoding.UTF8.GetBytes(s2).Length;
						Thread.Sleep(50);
					}
				}
				catch (Exception ex4)
				{
					UnityEngine.Debug.Log("Send connection lost. Reconnecting... Cause: " + ex4.Message + "
stack trace:
" + ex4.StackTrace);
					this.LastError = "Send connection lost. Reconnecting... Cause: " + ex4.Message + "
stack trace:
" + ex4.StackTrace;
				}
			}
		};
		recvWorker.ProgressChanged += delegate(object sender, ProgressChangedEventArgs e)
		{
			try
			{
				string[] array3 = e.UserState.ToString().Replace("", "").Split(new string[]
				{
					"
"
				}, StringSplitOptions.None);
				string text = array3[array3.Length - 1];
				UnityEngine.Debug.Log(text);
				this.HandleNewBracket(JSONNode.Parse(text), missionsManager);
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.Log("Failed to process incoming server bracket: " + ex.Message + "
stack trace: " + ex.StackTrace);
				this.LastError = "Failed to process incoming server bracket: " + ex.Message + "
stack trace: " + ex.StackTrace;
			}
		};
		recvWorker.WorkerReportsProgress = true;
		recvWorker.RunWorkerAsync();
		UnityEngine.Debug.Log("initialized");
		this.SendMessageToast("Initialization successful");
	}
	public Vector3 StringToVector3(string StrInputUnfiltered)
	{
		UnityEngine.Debug.Log(StrInputUnfiltered);
		string[] array = StrInputUnfiltered.Trim(new char[]
		{
			'(',
			')'
		}).Replace(" ", "").Split(new char[]
		{
			','
		});
		return new Vector3(float.Parse(array[0]), float.Parse(array[1]), float.Parse(array[2]));
	}
	public void HandleNewBracket(JSONNode bracket, MissionsManager thisManager)
	{
		double totalSeconds = this.stopwatch.Elapsed.TotalSeconds;
		if (bracket.HasKey("ChatService"))
		{
			UnityEngine.Debug.Log(bracket["ChatService"]["Chat"].ToString());
			this.lastServerChat = Regex.Replace(bracket["ChatService"]["Chat"].ToString().Trim(new char[]
			{
				'"'
			}).Replace("\n", Environment.NewLine), "\[" + string.Join("|", this.playersToBlock.ToArray()) + "\].*?\n", "");
		}
		else
		{
			UnityEngine.Debug.Log("No chat! Wth!");
		}
		if (bracket["PositionService"].HasKey("CurrentServerTime") && this.lastServerTime < 0.0)
		{
			this.lastServerTime = bracket["PositionService"]["CurrentServerTime"].AsDouble;
		}
		this.serverDelayTime = Math.Abs(this.lastServerTime - bracket["PositionService"]["CurrentServerTime"].AsDouble);
		UnityEngine.Debug.Log("updating buffer");
		if (this.bracketBuffer.ContainsKey(bracket["PositionService"]["CurrentServerTime"].AsDouble))
		{
			UnityEngine.Debug.Log("buffer key exists");
			this.bracketBuffer[bracket["PositionService"]["CurrentServerTime"].AsDouble] = bracket;
		}
		else
		{
			UnityEngine.Debug.Log("buffer key doesnt exist");
			this.bracketBuffer.Add(bracket["PositionService"]["CurrentServerTime"].AsDouble, bracket);
		}
		this.NewestBracket = bracket["PositionService"]["CurrentServerTime"].AsDouble;
		UnityEngine.Debug.Log("clearing buffer from old entries");
		if (this.bracketBuffer.Count > 300)
		{
			double num = double.MaxValue;
			foreach (double num2 in this.bracketBuffer.Keys)
			{
				if (num2 < num)
				{
					num = num2;
				}
			}
			this.bracketBuffer.Remove(num);
		}
		UnityEngine.Debug.Log("doing that old update thing");
		foreach (KeyValuePair<string, JSONNode> keyValuePair in bracket["PositionService"]["Positions"])
		{
			if (this.MyUsername == null || !(this.MyUsername == keyValuePair.Key))
			{
				UnityEngine.Debug.Log("trying to render (or create new) " + keyValuePair.Key);
				if (this.RenderedPrefabs.ContainsKey(keyValuePair.Key))
				{
					Multiplayer.Pair<Mission.MsObj, GameObject> pair = this.RenderedPrefabs[keyValuePair.Key];
					UnityEngine.Debug.Log("got to rendering (moving) existing " + keyValuePair.Key);
					if (pair.First != null)
					{
						if (pair.Second != null)
						{
							UnityEngine.Debug.Log("about to tween existing " + keyValuePair.Key);
							try
							{
								this.States[keyValuePair.Key] = this.JSONNodeToState(keyValuePair.Value[3]);
							}
							catch
							{
							}
							if (this.GetPlaneType(pair.First.PrefabIdx) != keyValuePair.Value[1])
							{
								UnityEngine.Debug.Log(string.Concat(new string[]
								{
									"plane type changed from ",
									this.GetPlaneType(pair.First.PrefabIdx),
									" to ",
									keyValuePair.Value[1],
									", subject to rerender"
								}));
								this.Destroy(pair.Second);
								this.RenderedPrefabs.Remove(keyValuePair.Key);
								if (this.PlayerMarkers.ContainsKey(keyValuePair.Key))
								{
									Useful.UseMarkDisplay.RemoveMarker(this.PlayerMarkers[keyValuePair.Key]);
									this.PlayerMarkers.Remove(keyValuePair.Key);
								}
								if (this.PersonModels.ContainsKey(keyValuePair.Key))
								{
									try
									{
										this.Destroy(this.PersonModels[keyValuePair.Key]);
										this.PersonModels.Remove(keyValuePair.Key);
									}
									catch
									{
									}
								}
								this.InProcessing.Add(keyValuePair.Key, keyValuePair.Value);
							}
						}
						else
						{
							this.RenderedPrefabs.Remove(keyValuePair.Key);
							if (this.PlayerMarkers.ContainsKey(keyValuePair.Key))
							{
								Useful.UseMarkDisplay.RemoveMarker(this.PlayerMarkers[keyValuePair.Key]);
								this.PlayerMarkers.Remove(keyValuePair.Key);
							}
							if (!this.InProcessing.ContainsKey(keyValuePair.Key))
							{
								this.InProcessing.Add(keyValuePair.Key, keyValuePair.Value);
							}
						}
					}
				}
				else if (!this.InProcessing.ContainsKey(keyValuePair.Key))
				{
					UnityEngine.Debug.Log("tryna spawn new Modele " + keyValuePair.Key + " but not exists");
					this.InProcessing.Add(keyValuePair.Key, keyValuePair.Value);
				}
				else
				{
					UnityEngine.Debug.Log("tryna spawn new Modele " + keyValuePair.Key + " but it already exists in processing");
				}
				if (this.InProcessing.ContainsKey(keyValuePair.Key) && this.RenderedPrefabs.ContainsKey(keyValuePair.Key))
				{
					UnityEngine.Debug.Log("spawned new model done");
					this.InProcessing.Remove(keyValuePair.Key);
				}
			}
		}
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, Multiplayer.Pair<Mission.MsObj, GameObject>> keyValuePair2 in this.RenderedPrefabs)
		{
			if (!bracket["PositionService"]["Positions"].HasKey(keyValuePair2.Key))
			{
				UnityEngine.Debug.Log("despawning due to player left");
				this.Destroy(keyValuePair2.Value.Second);
				if (this.PlayerMarkers.ContainsKey(keyValuePair2.Key))
				{
					Useful.UseMarkDisplay.RemoveMarker(this.PlayerMarkers[keyValuePair2.Key]);
					this.PlayerMarkers.Remove(keyValuePair2.Key);
				}
				if (this.PersonModels.ContainsKey(keyValuePair2.Key))
				{
					UnityEngine.Object.Destroy(this.PersonModels[keyValuePair2.Key]);
					this.PersonModels.Remove(keyValuePair2.Key);
				}
				list.Add(keyValuePair2.Key);
			}
		}
		foreach (string key in list)
		{
			this.RenderedPrefabs.Remove(key);
		}
		double totalSeconds2 = this.stopwatch.Elapsed.TotalSeconds;
		UnityEngine.Debug.Log("time taken to handle bracket: " + (totalSeconds2 - totalSeconds));
	}
	public string PrefabNameToTFSMPName(string PrefabName)
	{
		if (PrefabName == string.Empty)
		{
			return "MC-400";
		}
		MissionsManager.PrefabData[] prefabs = this.missionsManager.Prefabs;
		for (int i = 0; i < prefabs.Length; i++)
		{
			if (prefabs[i].Name.Replace(" ", "") == PrefabName.Replace(" ", ""))
			{
				return this.GetPlaneType(i);
			}
		}
		return "C-400";
	}
	public string GetPlaneType(int prefabIdx)
	{
		if (prefabIdx <= 27)
		{
			if (prefabIdx <= 17)
			{
				switch (prefabIdx)
				{
				case 0:
					return "C-400";
				case 1:
					return "C-400";
				case 2:
					return "HC-400";
				case 3:
					return "InPerson";
				case 4:
					return "HC-400";
				default:
					if (prefabIdx == 17)
					{
						return "MC-400";
					}
					break;
				}
			}
			else
			{
				if (prefabIdx == 23)
				{
					return "4x4";
				}
				if (prefabIdx == 27)
				{
					return "Flatbed";
				}
			}
		}
		else if (prefabIdx <= 41)
		{
			if (prefabIdx == 40)
			{
				return "RL-42";
			}
			if (prefabIdx == 41)
			{
				return "RL-72";
			}
		}
		else
		{
			switch (prefabIdx)
			{
			case 47:
				return "APC";
			case 48:
				return "E-42";
			case 49:
			case 50:
				break;
			case 51:
				return "8x8";
			default:
				switch (prefabIdx)
				{
				case 57:
					return "XV-40";
				case 59:
					return "FuelTruck";
				case 61:
					return "PV-40";
				}
				break;
			}
		}
		return "HC-400";
	}
	private void TweenGameObject(GameObject gameObject, Vector3 targetPosition, Vector3 targetEulerAngle, float timeToTween)
	{
		float num = 0f;
		Vector3 position = gameObject.transform.position;
		Vector3 eulerAngles = gameObject.transform.eulerAngles;
		while (num < timeToTween)
		{
			float t = num / timeToTween;
			gameObject.transform.position = Vector3.Lerp(position, targetPosition, t);
			gameObject.transform.eulerAngles = Vector3.Lerp(eulerAngles, targetEulerAngle, t);
			num += 0f;
			Thread.Sleep(3);
			if (gameObject.transform.position != Vector3.Lerp(position, targetPosition, t))
			{
				break;
			}
		}
		gameObject.transform.position = targetPosition;
		gameObject.transform.eulerAngles = targetEulerAngle;
	}
	public void CoolTweenGameObject(GameObject gameObject, Vector3 targetPosition, Vector3 targetEulerAngle, float timeToTween)
	{
		Vector3 position = gameObject.transform.position;
		Vector3 eulerAngles = gameObject.transform.eulerAngles;
		try
		{
			gameObject.GetComponent<PhysSystem>().OwnRigid.isKinematic = false;
			gameObject.GetComponent<PhysSystem>().OwnRigid.position = targetPosition;
			gameObject.GetComponent<PhysSystem>().OwnRigid.transform.eulerAngles = targetEulerAngle;
		}
		catch (Exception ex)
		{
			UnityEngine.Debug.Log("failed to move plane's rigid: " + ex.Message + "
stack trace:
" + ex.StackTrace);
			gameObject.transform.position = targetPosition;
			gameObject.transform.eulerAngles = targetEulerAngle;
		}
	}
	public void Destroy(GameObject gameObject)
	{
		this.ToDespawn.Add(gameObject);
	}
	public List<object> GetPrefs(string ServerIP, int ServerPort)
	{
		List<object> result;
		try
		{
			if (Useful.UseMissionsMng != null)
			{
				result = new List<object>
				{
					Useful.UseMissionsMng.enteredUsername,
					Useful.UseMissionsMng.enteredServer,
					int.Parse(Useful.UseMissionsMng.enteredPort),
					Useful.UseMissionsMng.enteredPassword
				};
				return result;
			}
			string text = "/storage/emulated/0/tfsmp/data.dat";
			if (File.Exists(text))
			{
				string str = File.ReadAllText(text);
				UnityEngine.Debug.Log("data.dat contents: " + str);
				string[] array = File.ReadAllLines(text);
				string text2 = this.MyUsername;
				string text3 = "play.vopwn55.xyz";
				int num = 8880;
				string item = "None";
				string[] array2 = array;
				for (int i = 0; i < array2.Length; i++)
				{
					string[] array3 = array2[i].Split(new char[]
					{
						':'
					});
					if (array3.Length == 2)
					{
						string a = array3[0].Trim();
						string text4 = array3[1].Trim();
						if (!(a == "Username"))
						{
							if (!(a == "Password"))
							{
								if (!(a == "Server"))
								{
									if (a == "Port" && text4 != "")
									{
										int.TryParse(text4, out num);
									}
								}
								else if (text4 != "")
								{
									text3 = text4;
								}
							}
							else if (text4 != "")
							{
								item = text4;
							}
						}
						else if (text4 != "")
						{
							text2 = text4;
						}
					}
				}
				ServerIP = text3;
				ServerPort = num;
				this.MyUsername = text2;
				result = new List<object>
				{
					text2,
					ServerIP,
					num,
					item
				};
				return result;
			}
			UnityEngine.Debug.LogError("File not found: " + text);
			string @string = PlayerPrefs.GetString("TFSMPUsername");
			string string2 = PlayerPrefs.GetString("TFSMPServerAddress");
			int @int = PlayerPrefs.GetInt("TFSMPServerPort");
			if (@string != null)
			{
				this.MyUsername = @string;
				UnityEngine.Debug.Log(this.MyUsername);
			}
			if (string2 != null)
			{
				ServerIP = string2;
				UnityEngine.Debug.Log(ServerIP);
			}
			ServerPort = @int;
			UnityEngine.Debug.Log(ServerPort);
			result = new List<object>
			{
				@string,
				string2,
				@int,
				"None"
			};
		}
		catch (Exception ex)
		{
			UnityEngine.Debug.Log("failed to retrieve playerprefs for tfsmp: 
" + ex.Message + "
stack trace: " + ex.StackTrace);
			result = new List<object>
			{
				"Guest1337",
				"play.vopwn55.xyz",
				8880,
				"None"
			};
		}
		return result;
	}
	private AndroidJavaObject GetExtras(AndroidJavaObject intent)
	{
		AndroidJavaObject result = null;
		try
		{
			result = intent.Call<AndroidJavaObject>("getExtras", new object[0]);
		}
		catch (Exception ex)
		{
			UnityEngine.Debug.Log(ex.Message);
		}
		return result;
	}
	private string GetProperty(AndroidJavaObject extras, string name)
	{
		string result = string.Empty;
		try
		{
			result = extras.Call<string>("getString", new object[]
			{
				name
			});
		}
		catch (Exception ex)
		{
			UnityEngine.Debug.Log(ex.Message);
		}
		return result;
	}
	public string StateToString()
	{
		string text = "{";
		foreach (KeyValuePair<string, object> keyValuePair in this.currentState)
		{
			string text2 = "";
			if (keyValuePair.Value is string)
			{
				text2 = """;
			}
			text = string.Concat(new string[]
			{
				text,
				""",
				keyValuePair.Key,
				"":",
				text2,
				keyValuePair.Value.ToString().ToLower(),
				text2,
				", "
			});
		}
		text = text.TrimEnd(new char[]
		{
			',',
			' '
		});
		return text + "}";
	}
	public Dictionary<string, bool> StringToState(string state)
	{
		string[] array = state.Split(new string[]
		{
			","
		}, StringSplitOptions.RemoveEmptyEntries);
		Dictionary<string, bool> dictionary = new Dictionary<string, bool>();
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			string[] array3 = array2[i].Split(new char[]
			{
				':'
			});
			string key = array3[0].Trim(new char[]
			{
				'"',
				' '
			});
			string text = array3[1].Trim(new char[]
			{
				'"',
				' '
			});
			bool value = false;
			if (text.ToLower() == "true")
			{
				value = true;
			}
			dictionary[key] = value;
		}
		return dictionary;
	}
	private object getIntentData()
	{
		return this.CreatePushClass(new AndroidJavaClass("com.unity3d.player.UnityPlayer"));
	}
	public object CreatePushClass(AndroidJavaClass UnityPlayer)
	{
		AndroidJavaObject intent = UnityPlayer.GetStatic<AndroidJavaObject>("currentActivity").Call<AndroidJavaObject>("getIntent", new object[0]);
		AndroidJavaObject extras = this.GetExtras(intent);
		if (extras != null)
		{
			return this.GetProperty(extras, "MultiplayerData");
		}
		return false;
	}
	public void Update(float dt)
	{
		double totalSeconds = this.stopwatch.Elapsed.TotalSeconds;
		this.CollisionsEnabled = Useful.TerrainBump;
		this.TimeSinceLaunch += Time.deltaTime;
		this.lastServerTime += this.getPreciseDelta();
		UnityEngine.Debug.Log("hi im Update()");
		try
		{
			UnityEngine.Debug.Log("moving");
			List<double> list = new List<double>(this.bracketBuffer.Keys);
			list.Sort();
			foreach (KeyValuePair<string, Multiplayer.Pair<Mission.MsObj, GameObject>> keyValuePair in this.RenderedPrefabs)
			{
				try
				{
					double num = (double)Useful.CollisionWarnTime * 0.1 + 0.2;
					double num2 = this.lastServerTime - num;
					double num3 = 0.0;
					foreach (double num4 in this.bracketBuffer.Keys)
					{
						if (num4 > num2)
						{
							num3 = num4;
							break;
						}
					}
					if (num3 != 0.0)
					{
						JSONNode jsonnode = this.bracketBuffer[num3];
						double num5 = 0.0;
						foreach (double num6 in list)
						{
							if (num6 >= num2 || (this.bracketBuffer[num6]["PositionService"]["Positions"].HasKey(keyValuePair.Key) && jsonnode["PositionService"]["Positions"][keyValuePair.Key][0] == this.bracketBuffer[num6]["PositionService"]["Positions"][keyValuePair.Key][0]))
							{
								break;
							}
							num5 = num6;
						}
						if (num5 == 0.0)
						{
							num5 = num3;
						}
						JSONNode jsonnode2 = this.bracketBuffer[num5];
						double num7 = (num2 - num5) / (num3 - num5);
						if (num7 > 10.0 || num7 < 0.0)
						{
							num7 = 1.0;
						}
						Useful.UseMissionsMng.displayConnectionBox = false;
						this.debugInfo = string.Concat(new object[]
						{
							"Network delay ",
							this.serverDelayTime * 1000.0,
							"ms, render delay ",
							num * 1000.0,
							"ms",
							"
Out: ",
							this.NicelyRepresentBytes(this.GetOutSpeed(), 1),
							"/s, In: ",
							this.NicelyRepresentBytes(this.GetInSpeed(), 1),
							"/s
Total out: ",
							this.NicelyRepresentBytes(this.BytesOutOverTehInterwebz, 1),
							", total in: ",
							this.NicelyRepresentBytes(this.BytesInOverTehInterwebz, 1)
						});
						PhysSystem component = keyValuePair.Value.Second.GetComponent<PhysSystem>();
						if (component != null)
						{
							UnityEngine.Debug.Log("lerping");
							Vector3 position = component.OwnRigid.position;
							Vector3 vector = this.StringToVector3(jsonnode2["PositionService"]["Positions"][keyValuePair.Key][0]);
							Vector3 vector2 = this.StringToVector3(jsonnode["PositionService"]["Positions"][keyValuePair.Key][0]);
							Vector3 vector3 = this.StringToVector3(jsonnode2["PositionService"]["Positions"][keyValuePair.Key][2]);
							Vector3 vector4 = this.StringToVector3(jsonnode["PositionService"]["Positions"][keyValuePair.Key][2]);
							component.OwnRigid.position = Vector3.LerpUnclamped(Useful.UseWorldContainer.Origin + this.objectToVector(vector), Useful.UseWorldContainer.Origin + this.objectToVector(vector2), (float)num7) + new Vector3(0f, 0.01f, 0f);
							component.OwnRigid.transform.eulerAngles = this.LerpEulerAngle(this.objectToVector(vector3), this.objectToVector(vector4), (float)num7);
							component.OwnRigid.useGravity = false;
							component.OwnRigid.isKinematic = false;
							component.OwnRigid.detectCollisions = false;
							component.OwnRigid.velocity = Vector3.zero;
							try
							{
								if (this.PersonModels.ContainsKey(keyValuePair.Key))
								{
									component.OwnRigid.position += new Vector3(0f, 0.86f, 0f);
									this.PersonModels[keyValuePair.Key].transform.position = component.OwnRigid.position;
								}
							}
							catch (Exception ex)
							{
								UnityEngine.Debug.Log("failed to move player model for reason " + ex.Message + "
stack trace:
" + ex.StackTrace);
							}
							if (component.ProxyColl != null)
							{
								component.ProxyColl.transform.position = new Vector3(0f, -500f, 0f) - Useful.UseWorldContainer.Origin;
								component.ProxyColl.transform.eulerAngles = component.OwnRigid.transform.eulerAngles;
							}
							try
							{
								component.gameObject.GetComponent<Renderer>().enabled = (Vector3.Distance(this.broadcastPosition, component.OwnRigid.position) < 3000f);
							}
							catch
							{
							}
						}
						UnityEngine.Debug.Log("No component! Wth?");
					}
					else
					{
						UnityEngine.Debug.Log("Bro youre lagging too much!");
						this.debugInfo = string.Concat(new object[]
						{
							"Network delay ",
							this.serverDelayTime * 1000.0,
							"ms, render delay ",
							num,
							"ms
",
							"Your internet connection is too slow, unable to properly process server data.
",
							"Render time ",
							num2
						});
						Useful.UseMissionsMng.displayConnectionBox = true;
						Useful.UseMissionsMng.connectionBoxContent = "Connection paused due to unstable internet.
You can attempt to fix this by going to TFS Settings and setting Collision Warn Time to 8 seconds.";
						double num8 = 0.0;
						foreach (double num9 in this.bracketBuffer.Keys)
						{
							if (num9 > num8)
							{
								num8 = num9;
							}
						}
						if (num8 == 0.0)
						{
							UnityEngine.Debug.Log("No brackets available!");
						}
						else
						{
							PhysSystem component2 = keyValuePair.Value.Second.GetComponent<PhysSystem>();
							if (component2 != null)
							{
								JSONNode jsonnode3 = this.bracketBuffer[num3];
								Vector3 vector5 = this.StringToVector3(jsonnode3["PositionService"]["Positions"][keyValuePair.Key][0].ToString());
								Vector3 vector6 = this.StringToVector3(jsonnode3["PositionService"]["Positions"][keyValuePair.Key][2].ToString());
								component2.OwnRigid.position = Useful.UseWorldContainer.Origin + this.objectToVector(vector5) + new Vector3(0f, 0.01f, 0f);
								component2.OwnRigid.transform.eulerAngles = this.objectToVector(vector6);
								component2.OwnRigid.useGravity = false;
								component2.OwnRigid.isKinematic = false;
								component2.OwnRigid.detectCollisions = false;
								component2.OwnRigid.velocity = Vector3.zero;
							}
						}
					}
				}
				catch (Exception ex2)
				{
					UnityEngine.Debug.Log("failed to move gameobject for reason: " + ex2.Message + "
stack trace:
" + ex2.StackTrace);
					this.LastError = "failed to move gameobject for reason: " + ex2.Message + "
stack trace:
" + ex2.StackTrace;
				}
			}
		}
		catch (Exception ex3)
		{
			UnityEngine.Debug.Log("failed to move gameobject for reason: " + ex3.Message + "
stack trace:
" + ex3.StackTrace);
			this.LastError = "failed to move gameobject for reason: " + ex3.Message + "
stack trace:
" + ex3.StackTrace;
		}
		UnityEngine.Debug.Log("resetting bounds. Why?");
		try
		{
			foreach (KeyValuePair<string, Multiplayer.Pair<Mission.MsObj, GameObject>> keyValuePair2 in this.RenderedPrefabs)
			{
				UnityEngine.Debug.Log("querying engine states");
				if (this.States.ContainsKey(keyValuePair2.Key))
				{
					UnityEngine.Debug.Log("state key found");
					AircraftPhysics component3 = keyValuePair2.Value.Second.GetComponent<AircraftPhysics>();
					if (component3 != null)
					{
						UnityEngine.Debug.Log("updating engines: " + component3.Engines.Length);
						int num10 = 0;
						foreach (AircraftPhysics.EnginePropFan enginePropFan in component3.Engines)
						{
							UnityEngine.Debug.Log(string.Concat(new object[]
							{
								"updating engine ",
								num10 + 1,
								", state ",
								this.States[keyValuePair2.Key]["Eng" + (num10 + 1)].ToString()
							}));
							try
							{
								if (Convert.ToBoolean(this.States[keyValuePair2.Key]["Eng" + (num10 + 1)].ToString()))
								{
									enginePropFan.Trans.Rotate(enginePropFan.SpinAxis, 12f, Space.Self);
								}
							}
							catch
							{
							}
							num10++;
						}
						UnityEngine.Debug.Log("updating gear");
						if (component3.GearDown != Convert.ToBoolean(this.States[keyValuePair2.Key]["GearDown"].ToString()))
						{
							UnityEngine.Debug.Log("toggling landing gear for " + keyValuePair2.Key);
							GameObject[] gearParentObjects = component3.GearParentObjects;
							for (int j = 0; j < gearParentObjects.Length; j++)
							{
								gearParentObjects[j].SetActive(Convert.ToBoolean(this.States[keyValuePair2.Key]["GearDown"].ToString()));
							}
						}
						UnityEngine.Debug.Log("updating vtol physics");
						TiltWingPhysics tiltWingPhysics = keyValuePair2.Value.Second.GetComponent<AircraftPhysics>() as TiltWingPhysics;
						if (tiltWingPhysics != null)
						{
							UnityEngine.Debug.Log("vtol detected");
							Transform[] wingAxles = tiltWingPhysics.WingAxles;
							for (int k = 0; k < wingAxles.Length; k++)
							{
								wingAxles[k].localEulerAngles = new Vector3(Convert.ToSingle(this.States[keyValuePair2.Key]["VTOLAngle"].ToString()), 0f, 0f);
							}
						}
						UnityEngine.Debug.Log("updating livery");
						if (this.States[keyValuePair2.Key].ContainsKey("LiveryId"))
						{
							UnityEngine.Debug.Log("livery chosen: " + Convert.ToInt32(this.States[keyValuePair2.Key]["LiveryId"].ToString()));
							MissionsManager.VehiclePrepPrms vehiclePrepPrms = Useful.UseMissionsMng.FindVehiclePrep(Useful.UseMissionsMng.Prefabs[keyValuePair2.Value.First.PrefabIdx].AssetName);
							UnityEngine.Debug.Log("prep params found");
							int num11 = Convert.ToInt32(this.States[keyValuePair2.Key]["LiveryId"].ToString());
							if (num11 > -1 && num11 < vehiclePrepPrms.Liveries.Length)
							{
								UnityEngine.Debug.Log("livery found");
								string text = vehiclePrepPrms.Liveries[num11];
								UnityEngine.Debug.Log("livery name found: " + text);
								MatIllumOcc component4 = keyValuePair2.Value.Second.GetComponent<MatIllumOcc>();
								if (component4 != null)
								{
									UnityEngine.Debug.Log("material illumination occlusion found");
									component4.SetNewLiveryMaterial(text);
								}
							}
						}
						if (keyValuePair2.Value.First.PrefabIdx == 61 && this.States[keyValuePair2.Key].ContainsKey("PV40Color"))
						{
							UnityEngine.Debug.Log("pv40 detected, color: " + this.States[keyValuePair2.Key]["PV40Color"].ToString());
							string[] array = this.States[keyValuePair2.Key]["PV40Color"].ToString().Split(new char[]
							{
								','
							});
							UnityEngine.Debug.Log("colorset split");
							if (array.Length > 3)
							{
								UnityEngine.Debug.Log("converting colorset");
								float[] array2 = new float[4];
								for (int l = 0; l <= 3; l++)
								{
									array2[l] = float.Parse(array[l].Replace(""", ""));
								}
								UnityEngine.Debug.Log("detecting matillumocc");
								MatIllumOcc component5 = keyValuePair2.Value.Second.GetComponent<MatIllumOcc>();
								if (component5 != null)
								{
									UnityEngine.Debug.Log("setting color material");
									component5.SetNewColoredMaterial(array2);
								}
							}
						}
					}
				}
			}
		}
		catch (Exception ex4)
		{
			UnityEngine.Debug.Log("failed to render state and engine for reason: " + ex4.Message + "
stack trace:
" + ex4.StackTrace);
		}
		UnityEngine.Debug.Log("trying to clean up old unneeded objects");
		try
		{
			foreach (GameObject gameObject in this.ToDespawn)
			{
				UnityEngine.Debug.Log("despawning object " + Convert.ToString(gameObject.name));
				UnityEngine.Object.Destroy(gameObject);
			}
			this.ToDespawn.Clear();
		}
		catch (Exception ex5)
		{
			UnityEngine.Debug.Log("failed to despawn unneeded obj for reason " + ex5.Message + "
stack trace:
" + ex5.StackTrace);
		}
		UnityEngine.Debug.Log("processing pending plane creations");
		foreach (KeyValuePair<string, JSONNode> keyValuePair3 in new Dictionary<string, JSONNode>(this.InProcessing))
		{
			UnityEngine.Debug.Log("trying to render: " + keyValuePair3.Key);
			if (this.missionsManager.CrtMission.TypeOfMission != Mission.TypeGameplay.FreeFlight)
			{
				UnityEngine.Debug.Log("failed to spawn plane: not freeflight");
				break;
			}
			if (this.PlayersBeingRendered.Contains(keyValuePair3.Key))
			{
				UnityEngine.Debug.Log("already rendering " + keyValuePair3.Key + " :(");
			}
			if (this.RenderedPrefabs.ContainsKey(keyValuePair3.Key))
			{
				UnityEngine.Debug.Log(keyValuePair3.Key + "already rendered! why he here?");
				if (this.InProcessing.ContainsKey(keyValuePair3.Key))
				{
					this.InProcessing.Remove(keyValuePair3.Key);
				}
			}
			else if (!this.RenderedPrefabs.ContainsKey(keyValuePair3.Key) && !this.PlayersBeingRendered.Contains(keyValuePair3.Key))
			{
				UnityEngine.Debug.Log("cleared to render " + keyValuePair3.Key);
				if (!this.PlayersBeingRendered.Contains(keyValuePair3.Key))
				{
					this.PlayersBeingRendered.Add(keyValuePair3.Key);
				}
				try
				{
					UnityEngine.Debug.Log("trying to spawn " + keyValuePair3.Key + " of type " + keyValuePair3.Value[1]);
					Mission.MsObj msObj = this.missionsManager.CrtMission.SpawnMissionObject(this.TFSMPNameToPrefabName(keyValuePair3.Value[1]), new Vector3(0f, -2000f, 0f), this.StringToVector3(keyValuePair3.Value[2]), 0f);
					try
					{
						PhysSystem component6 = msObj.GameObjReference.GetComponent<AircraftPhysics>();
						if (component6.ProxyColl != null)
						{
							component6.ProxyColl.transform.position = new Vector3(0f, -500f, 0f);
						}
					}
					catch
					{
					}
					UnityEngine.Debug.Log("setting its position");
					msObj.GameObjReference.transform.localPosition = this.StringToVector3(keyValuePair3.Value[0]);
					msObj.GameObjReference.transform.eulerAngles = this.StringToVector3(keyValuePair3.Value[2]);
					if (msObj.GameObjReference == null)
					{
						UnityEngine.Debug.Log("update: o no gameobject is null");
					}
					else
					{
						UnityEngine.Debug.Log("update: gameobject is probably not null");
					}
					try
					{
						UnityEngine.Object.Destroy(msObj.GameObjReference.GetComponent("Damageable"));
						UnityEngine.Object.Destroy(msObj.GameObjReference.GetComponent("AircraftUserInput"));
						UnityEngine.Object.Destroy(msObj.GameObjReference.GetComponent("AircraftAIInput"));
					}
					catch (Exception ex6)
					{
						UnityEngine.Debug.Log("oh no something bad happened in component removal!
" + ex6.Message + "
" + ex6.StackTrace);
					}
					UnityEngine.Debug.Log("remarking " + keyValuePair3.Key);
					MarkersDisplay useMarkDisplay = Useful.UseMarkDisplay;
					int num12 = useMarkDisplay.AddMarker(this.missionsManager.CrtMission.GetComponentOfMissionObject<PhysSystem>(msObj.UniqueId), 0);
					useMarkDisplay.GetMarker(num12).Label.text = keyValuePair3.Key;
					if (this.PlayerMarkers.ContainsKey(keyValuePair3.Key))
					{
						Useful.UseMarkDisplay.RemoveMarker(this.PlayerMarkers[keyValuePair3.Key]);
						this.PlayerMarkers.Remove(keyValuePair3.Key);
					}
					if (!this.PlayerMarkers.ContainsKey(keyValuePair3.Key))
					{
						this.PlayerMarkers.Add(keyValuePair3.Key, num12);
					}
					UnityEngine.Debug.Log("adding pair to rendered prefabs");
					Multiplayer.Pair<Mission.MsObj, GameObject> value = new Multiplayer.Pair<Mission.MsObj, GameObject>(msObj, msObj.GameObjReference);
					if (this.RenderedPrefabs.ContainsKey(keyValuePair3.Key))
					{
						try
						{
							UnityEngine.Object.Destroy(this.RenderedPrefabs[keyValuePair3.Key].Second);
						}
						catch (Exception ex7)
						{
							UnityEngine.Debug.Log("failed to despawn old plane for reason: " + ex7.Message);
						}
						this.RenderedPrefabs.Remove(keyValuePair3.Key);
					}
					this.RenderedPrefabs.Add(keyValuePair3.Key, value);
					if (keyValuePair3.Value[1] == "InPerson")
					{
						if (this.PersonModels.ContainsKey(keyValuePair3.Key))
						{
							try
							{
								UnityEngine.Object.Destroy(this.PersonModels[keyValuePair3.Key]);
								this.PersonModels.Remove(keyValuePair3.Key);
							}
							catch
							{
							}
						}
						GameObject gameObject2 = GameObject.CreatePrimitive(PrimitiveType.Capsule);
						try
						{
							gameObject2.GetComponent<Collider>().enabled = false;
						}
						catch
						{
						}
						this.PersonModels.Add(keyValuePair3.Key, gameObject2);
					}
					UnityEngine.Debug.Log(string.Concat(new string[]
					{
						"ya i just spawned prefab ",
						keyValuePair3.Value[1],
						" for ",
						keyValuePair3.Key,
						" at position ",
						keyValuePair3.Value[0]
					}));
					if (this.InProcessing.ContainsKey(keyValuePair3.Key))
					{
						this.InProcessing.Remove(keyValuePair3.Key);
					}
				}
				catch (Exception ex8)
				{
					UnityEngine.Debug.Log("oh no something bad happened in spawning plane!
" + ex8.Message + "
" + ex8.StackTrace);
				}
				this.PlayersBeingRendered.Remove(keyValuePair3.Key);
			}
		}
		double totalSeconds2 = this.stopwatch.Elapsed.TotalSeconds;
		UnityEngine.Debug.Log("time taken to update: " + (totalSeconds2 - totalSeconds));
	}
	public Vector3 objectToVector(object vector)
	{
		if (vector is Vector3)
		{
			return (Vector3)vector;
		}
		return Vector3.zero;
	}
	public float GetCurrentEpoch()
	{
		return this.TimeSinceLaunch;
	}
	public Vector3 normalizeRotationVector(Vector3 brokenVector)
	{
		return new Vector3(Mathf.Repeat(brokenVector.x, 360f), Mathf.Repeat(brokenVector.y, 360f), Mathf.Repeat(brokenVector.z, 360f));
	}
	private Vector3 LerpEulerAngle(Vector3 start, Vector3 end, float t)
	{
		for (int i = 0; i < 3; i++)
		{
			if (end[i] - start[i] > 180f)
			{
				int num = i;
				ref Vector3 ptr = ref end;
				ref Vector3 ptr2 = ref ptr;
				int index = num;
				ptr2[index] -= 360f;
			}
			else if (end[i] - start[i] < -180f)
			{
				int num2 = i;
				ref Vector3 ptr3 = ref end;
				ref Vector3 ptr2 = ref ptr3;
				int index = num2;
				ptr2[index] += 360f;
			}
		}
		return Vector3.Lerp(start, end, t);
	}
	public DateTime GetNetworkTime()
	{
		DateTime result;
		try
		{
			byte[] array = new byte[48];
			array[0] = 27;
			IPEndPoint remoteEP = new IPEndPoint(Dns.GetHostEntry("time.nist.gov").AddressList[0], 123);
			using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
			{
				socket.Connect(remoteEP);
				socket.ReceiveTimeout = 3000;
				socket.Send(array);
				socket.Receive(array);
				socket.Close();
			}
			ulong num = (ulong)BitConverter.ToUInt32(array, 40);
			ulong num2 = (ulong)BitConverter.ToUInt32(array, 44);
			num = (ulong)this.SwapEndianness(num);
			num2 = (ulong)this.SwapEndianness(num2);
			ulong num3 = num * 1000UL + num2 * 1000UL / 4294967296UL;
			return new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(num3);
		}
		catch (Exception ex)
		{
			result = new DateTime(2004, 10, 2);
			UnityEngine.Debug.Log("failed to retrieve date/time: " + ex.Message + "
stack trace:
" + ex.StackTrace);
		}
		return result;
	}
	public uint SwapEndianness(ulong x)
	{
		return (uint)(((x & 255UL) << 24) + ((x & 65280UL) << 8) + ((x & 16711680UL) >> 8) + ((x & 18446744073692774400UL) >> 24));
	}
	public double getPreciseDelta()
	{
		if (!this.stopwatch.IsRunning)
		{
			return 0.0;
		}
		double totalSeconds = this.stopwatch.Elapsed.TotalSeconds;
		double num = this.lastStopWatchSpan;
		this.lastStopWatchSpan = totalSeconds;
		return Math.Abs(totalSeconds - num);
	}
	public void SendMessageToast(string message)
	{
		AndroidJavaClass androidJavaClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
		AndroidJavaObject unityActivity = androidJavaClass.GetStatic<AndroidJavaObject>("currentActivity");
		if (unityActivity != null)
		{
			AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
			unityActivity.Call("runOnUiThread", new object[]
			{
				new AndroidJavaRunnable(delegate()
				{
					toastClass.CallStatic<AndroidJavaObject>("makeText", new object[]
					{
						unityActivity,
						message,
						0
					}).Call("show", new object[0]);
				})
			});
		}
	}
	public string NicelyRepresentBytes(int value, int decimalPlaces = 1)
	{
		string result;
		try
		{
			if (value < 0)
			{
				result = "-" + this.NicelyRepresentBytes(-value, decimalPlaces);
			}
			else
			{
				int num = 0;
				decimal num2 = value;
				while (Math.Round(num2, decimalPlaces) >= 1000m)
				{
					num2 /= 1024m;
					num++;
				}
				result = string.Format("{0:n" + decimalPlaces + "} {1}", num2, this.SizeSuffixes[num]);
			}
		}
		catch (Exception ex)
		{
			UnityEngine.Debug.Log("nice fail " + ex.Message + "
" + ex.StackTrace);
			result = "Unknown";
		}
		return result;
	}
	public int GetOutSpeed()
	{
		if (this.LastQueryOut < 1.0)
		{
			this.LastQueryOut = (double)this.TimeSinceLaunch;
			return 0;
		}
		int outSpeedRaw = this.BytesOutOverTehInterwebz - this.BytesAsOfLastQueryOut;
		if ((double)this.TimeSinceLaunch - this.LastQueryOut > 1.0)
		{
			this.OutSpeedRaw = outSpeedRaw;
			this.BytesAsOfLastQueryOut = this.BytesOutOverTehInterwebz;
			this.LastQueryOut = (double)this.TimeSinceLaunch;
		}
		return this.OutSpeedRaw;
	}
	public int GetInSpeed()
	{
		if (this.LastQueryIn < 1.0)
		{
			this.LastQueryIn = (double)this.TimeSinceLaunch;
			return 0;
		}
		int inSpeedRaw = this.BytesInOverTehInterwebz - this.BytesAsOfLastQueryIn;
		if ((double)this.TimeSinceLaunch - this.LastQueryIn > 1.0)
		{
			this.InSpeedRaw = inSpeedRaw;
			this.BytesAsOfLastQueryIn = this.BytesInOverTehInterwebz;
			this.LastQueryIn = (double)this.TimeSinceLaunch;
		}
		return this.InSpeedRaw;
	}
	public string TFSMPNameToPrefabName(string TFSMPName)
	{
		if (TFSMPName == "InPerson")
		{
			return "Person";
		}
		if (TFSMPName == "4x4")
		{
			return "Military 4x4";
		}
		if (TFSMPName == "APC")
		{
			return "APC 6x6";
		}
		if (TFSMPName == "8x8")
		{
			return "Truck 8x8";
		}
		if (TFSMPName == "Flatbed")
		{
			return "Loader";
		}
		if (TFSMPName == "FuelTruck")
		{
			return "Fuel Truck";
		}
		return TFSMPName.Replace("InPerson", "Person");
	}
	public void ParseVoscript(string voscriptRaw)
	{
		string text = voscriptRaw.Trim(new char[]
		{
			'"'
		});
		UnityEngine.Debug.Log("parsing voscript: " + text);
		foreach (string text2 in text.Replace("
", "").Split(new char[]
		{
			'
'
		}))
		{
			string text3 = text2.Split(new char[]
			{
				'('
			})[0];
			string text4 = text2.Split(new char[]
			{
				'('
			})[1].Replace(")", "");
			UnityEngine.Debug.Log("parsing function: " + text3);
			if (text3 == "PopupWindow")
			{
				string[] array2 = text4.Split(new char[]
				{
					','
				});
				Useful.UseMissionsMng.CreatePopupWindow(array2[0].Trim(), array2[1].Trim());
			}
		}
	}
	public Dictionary<string, object> JSONNodeToState(JSONNode state)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		foreach (KeyValuePair<string, JSONNode> keyValuePair in state)
		{
			dictionary[keyValuePair.Key] = keyValuePair.Value;
		}
		return dictionary;
	}
	public Vector3 broadcastPosition;
	public string planeType;
	public Socket socket;
	public Vector3 broadcastRotation;
	public string lastRenderData;
	public Dictionary<string, JSONNode> InProcessing;
	private MissionsManager missionsManager;
	public Dictionary<string, Multiplayer.Pair<Mission.MsObj, GameObject>> RenderedPrefabs;
	public string MyUsername;
	public float lastRecvTime;
	public float pingMS;
	private bool SmoothMovementAllowed;
	public List<GameObject> ToDespawn;
	public JSONNode lastProcessedPositions;
	public string lastProcessedTimestamp;
	public Dictionary<string, int> PlayerMarkers;
	public List<string> PlayersBeingRendered;
	public string TargetServer;
	public string LastError;
	public Dictionary<string, List<object>> playerPositions = new Dictionary<string, List<object>>();
	private float TimeSinceLaunch;
	public string debugInfo;
	private const string NtpServerAddress = "time.google.com";
	public double lastServerTime;
	public double serverDelayTime;
	public Dictionary<double, JSONNode> bracketBuffer;
	public double OldestBracket;
	public double NewestBracket;
	public Stopwatch stopwatch;
	public double lastStopWatchSpan;
	public int BytesOutOverTehInterwebz;
	public int BytesInOverTehInterwebz;
	public double LastQueryIn;
	public double LastQueryOut;
	public int BytesAsOfLastQueryIn;
	public int BytesAsOfLastQueryOut;
	public string[] SizeSuffixes;
	public int OutSpeedRaw;
	public int InSpeedRaw;
	public string underreportedPacket;
	public bool CollisionsEnabled;
	public bool InPersonMode;
	public GameObject debugcube;
	public Dictionary<string, GameObject> PersonModels;
	public string chatPendingSend;
	public string lastServerChat;
	public List<string> playersToBlock;
	public Dictionary<string, object> currentState;
	public Dictionary<string, Dictionary<string, object>> States;
	public class Pair<T, U>
	{
		public T First { get; set; }
		public U Second { get; set; }
		public Pair(T first, U second)
		{
			this.First = first;
			this.Second = second;
		}
	}
}
