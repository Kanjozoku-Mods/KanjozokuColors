using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace KanjozokuColors {
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
	[BepInProcess("Kanjozoku Game.exe")]
    public class KanjoCol : BaseUnityPlugin {
		
		public Harmony Harmony { get; } = new Harmony(PluginInfo.PLUGIN_GUID);
		
		public static KanjoCol Instance = null;
		
        private void Awake() {
			/* Keep Instance */
			Instance = this;
			
			/* Unity Patching */
			Harmony.PatchAll();
			Logger.LogInfo($"{PluginInfo.PLUGIN_GUID} is loaded!");
        }
		
		private void _Log(string msg, LogLevel lvl) {
			Logger.Log(lvl, msg);
		}

		public static void Log(string msg, LogLevel lvl = LogLevel.Info) {
			if (KanjoCol.Instance == null)
				return;
			Instance._Log(msg, lvl);
		}
    }
	
	[HarmonyPatch]
	public static class PaintPatch {
		static Text redText = null;
		static Text greenText = null;
		static Text blueText = null;
		static string[] COLORS = new string[] { "RED", "GREEN", "BLUE" };
		
		static int redIdx = 0;
		static int greenIdx = 0;
		static int blueIdx = 0;
		
		static Menu _this = null;
		
		static GameObject red = null;
		static GameObject green = null;
		static GameObject blue = null;
		static GameObject sel = null;

		public static void SetColor() {
			Color col = new Color(redIdx / 50f, greenIdx / 50f, blueIdx / 50f);
			
			if (_this.bodyPaintMode) {
				_this.activeCar.SetBodyColor(col);
			} else {
				_this.activeCar.SetWheelsColor(col);
			}
			
			_this.price = 2000;
			
			redText.text = $"RED: {redIdx}%";
			greenText.text = $"GREEN: {greenIdx}%";
			blueText.text = $"BLUE: {blueIdx}%";

			_this.paintPriceText.text = _this.price.ToString("#,##0") + " JPY";
		}
		
		public static void NextColor(int col) {
			if (col == 0) {
				if (redIdx > 99)
					return;
				redIdx++;
			} else if (col == 1) {
				if (greenIdx > 99)
					return;
				greenIdx++;
			} else if (col == 2) {
				if (blueIdx > 99)
					return;
				blueIdx++;
			}
			_this.clickSound.Play();
			SetColor();
		}
	
		public static void PreviousColor(int col) {
			if (col == 0) {
				if (redIdx < 1)
					return;
				redIdx--;
			} else if (col == 1) {
				if (greenIdx < 1)
					return;
				greenIdx--;
			} else if (col == 2) {
				if (blueIdx < 1)
					return;
				blueIdx--;
			}
			_this.clickSound.Play();
			SetColor();
		}
		
		[HarmonyPatch(typeof(Menu), "Start")]
		static class AddComponents {
			private static void Postfix(Menu __instance) { // This is super unclean - if someone can do this better - please
				sel = __instance.paintMenu.transform.Find("selection").gameObject;

				var nxt = sel.transform.Find("next").gameObject.GetComponent<Button>();
				var prv = sel.transform.Find("prev").gameObject.GetComponent<Button>();
				nxt.onClick.SetPersistentListenerState(0, UnityEventCallState.Off);
				prv.onClick.SetPersistentListenerState(0, UnityEventCallState.Off);

				blue = UnityEngine.Object.Instantiate<GameObject>(sel);

				nxt.onClick.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
				prv.onClick.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);

				blue.name = "color_blue";
				blue.transform.SetParent(__instance.paintMenu.transform);
				blue.transform.localScale = sel.transform.localScale;
				blue.transform.localPosition = sel.transform.localPosition;
				
				
				red = UnityEngine.Object.Instantiate<GameObject>(blue);

				red.name = "color_red";
				red.transform.SetParent(__instance.paintMenu.transform);
				red.transform.localScale = blue.transform.localScale;
				red.transform.localPosition = blue.transform.localPosition;
				red.transform.localPosition += new Vector3(0, 150, 0);
				
				green = UnityEngine.Object.Instantiate<GameObject>(red);

				green.name = "color_green";
				green.transform.SetParent(__instance.paintMenu.transform);
				green.transform.localScale = blue.transform.localScale;
				green.transform.localPosition = blue.transform.localPosition;
				green.transform.localPosition += new Vector3(0, 75, 0);
				
				green.transform.Find("icon (1)").gameObject.SetActive(false);
				blue.transform.Find("icon (1)").gameObject.SetActive(false);
				
				redText = red.transform.Find("text").gameObject.GetComponent<Text>();
				greenText = green.transform.Find("text").gameObject.GetComponent<Text>();
				blueText = blue.transform.Find("text").gameObject.GetComponent<Text>();
				
				
				nxt = blue.transform.Find("next").gameObject.GetComponent<Button>();
				nxt.onClick.AddListener(() => NextColor(2));
				prv = blue.transform.Find("prev").gameObject.GetComponent<Button>();
				prv.onClick.AddListener(() => PreviousColor(2));
				
				nxt = green.transform.Find("next").gameObject.GetComponent<Button>();
				nxt.onClick.AddListener(() => NextColor(1));
				prv = green.transform.Find("prev").gameObject.GetComponent<Button>();
				prv.onClick.AddListener(() => PreviousColor(1));
				
				nxt = red.transform.Find("next").gameObject.GetComponent<Button>();
				nxt.onClick.AddListener(() => NextColor(0));
				prv = red.transform.Find("prev").gameObject.GetComponent<Button>();
				prv.onClick.AddListener(() => PreviousColor(0));
				
				_this = __instance;
			}
		}
		
		private static void ActivateColors() {
			red.SetActive(true);
			green.SetActive(true);
			blue.SetActive(true);
			sel.SetActive(false);
		}
		
		private static void DeActivateColors() {
			red.SetActive(false);
			green.SetActive(false);
			blue.SetActive(false);
			sel.SetActive(true);
		}
		
		[HarmonyPatch(typeof(Menu), "SetBodyPaintMode")]
		static class _SetBodyPaintMode {
			private static bool Prefix(Menu __instance) {
				__instance.clickSound.Play();
				__instance.bodyPaintMode = true;
				__instance.liveryMode = false;
				ActivateColors();
				__instance.activeCar.SetCurrentPaints();
				SetColor();
				return false;
			}
		}
		
		[HarmonyPatch(typeof(Menu), "SetWheelsPaintMode")]
		static class _SetWheelsPaintMode {
			private static bool Prefix(Menu __instance) {
				__instance.clickSound.Play();
				__instance.bodyPaintMode = false;
				__instance.liveryMode = false;
				ActivateColors();
				__instance.activeCar.SetCurrentPaints();
				SetColor();
				return false;
			}
		}
		
		[HarmonyPatch(typeof(Menu), "SetLiveryMode")]
		static class _SetLiveryMode {
			private static void Prefix() {
				DeActivateColors();
			}
		}

		[HarmonyPatch(typeof(Menu), "GoPaint")]
		static class _GoPaint {
			private static void Postfix() {
				ActivateColors();
				SetColor();
			}
		}
		
		[HarmonyPatch(typeof(Menu), "BuyPaint")]
		static class _BuyPaint {
			private static bool Prefix(Menu __instance) {
				GlobalManager globalManager = Traverse.Create(__instance).Field("globalManager").GetValue() as GlobalManager;
				
				KanjoCol.Log("BuyPaint", LogLevel.Message);
				
				if (globalManager.playerData.cash >= __instance.price) {
					globalManager.playerData.cash -= __instance.price;
					__instance.purchaseSound.Play();
					if (!__instance.liveryMode) {
						SeriazibleColor col = new SeriazibleColor { r = redIdx / 50f, g = greenIdx / 50f, b = blueIdx / 50f };
						if (__instance.bodyPaintMode) {
							__instance.activeCar.carData.bodyColor = col;
						} else {
							__instance.activeCar.carData.wheelsColor = col;
						}
						
						
					} else {
						__instance.activeCar.carData.livery = __instance.liveryIndex;
					}
					
					
					globalManager.SaveData();
					__instance.GoBack();
					__instance.ResetStats();
				}
				return false;
			}
		}
	}
}
