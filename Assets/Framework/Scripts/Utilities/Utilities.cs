using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BizzyBeeGames
{
	public static class Utilities
	{
		#region Properties

		public static double SystemTimeInMilliseconds { get { return (System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalMilliseconds; } }

		public static float WorldWidth	{ get { return 2f * Camera.main.orthographicSize * Camera.main.aspect; } }
		public static float WorldHeight	{ get { return 2f * Camera.main.orthographicSize; } }
		public static float	XScale		{ get { return (float)UnityEngine.Screen.width / 1080f; } }	
		public static float	YScale		{ get { return (float)UnityEngine.Screen.height / 1920f; } }

		#endregion

		#region Delegates

		public delegate TResult MapFunc<out TResult, TArg>(TArg arg);
		public delegate bool FilterFunc<TArg>(TArg arg);

		#endregion

		#region Public Methods

		public static List<TOut> Map<TIn, TOut>(List<TIn> list, MapFunc<TOut, TIn> func)
		{
			List<TOut> newList = new List<TOut>(list.Count);

			for (int i = 0; i < list.Count; i++)
			{
				newList.Add(func(list[i]));
			}

			return newList;
		}

		public static void Filter<T>(List<T> list, FilterFunc<T> func)
		{
			for (int i = list.Count - 1; i >= 0; i--)
			{
				if (func(list[i]))
				{
					list.RemoveAt(i);
				}
			}
		}

		public static void SwapValue<T>(ref T value1, ref T value2)
		{
			T temp = value1;
			value1 = value2;
			value2 = temp;
		}

		public static float EaseOut(float t)
		{
			return 1.0f - (1.0f - t) * (1.0f - t) * (1.0f - t);
		}
		
		public static float EaseIn(float t)
		{
			return t * t * t;
		}

		/// <summary>
		/// Returns to mouse position
		/// </summary>
		public static Vector2 MousePosition()
		{
			#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBPLAYER
			return (Vector2)Input.mousePosition;
			#else
			if (Input.touchCount > 0)
			{
				return Input.touches[0].position;
			}

			return Vector2.zero;
			#endif
		}

		/// <summary>
		/// Returns true if a mouse down event happened, false otherwise
		/// </summary>
		public static bool MouseDown()
		{
			return Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began);
		}
		
		/// <summary>
		/// Returns true if a mouse up event happened, false otherwise
		/// </summary>
		public static bool MouseUp()
		{
			return (Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Ended));
		}
		
		/// <summary>
		/// Returns true if no mouse events are happening, false otherwise
		/// </summary>
		public static bool MouseNone()
		{
			return (!Input.GetMouseButton(0) && Input.touchCount == 0);
		}

		public static char CharToLower(char c)
		{
			return (c >= 'A' && c <= 'Z') ? (char)(c + ('a' - 'A')) : c;
		}

		public static int GCD(int a, int b)
		{
			int start = Mathf.Min(a, b);
			
			for (int i = start; i > 1; i--)
			{
				if (a % i == 0 && b % i == 0)
				{
					return i;
				}
			}
			
			return 1;
		}

		public static Canvas GetCanvas(Transform transform)
		{
			if (transform == null)
			{
				return null;
			}

			Canvas canvas = transform.GetComponent<Canvas>();

			if (canvas != null)
			{
				return canvas;
			}

			return GetCanvas(transform.parent);
		}

		public static void CallExternalAndroid(string methodname, params object[] args)
		{
			#if UNITY_ANDROID && !UNITY_EDITOR
			AndroidJavaClass	unity			= new AndroidJavaClass ("com.unity3d.player.UnityPlayer");
			AndroidJavaObject	currentActivity	= unity.GetStatic<AndroidJavaObject>("currentActivity");
			currentActivity.Call(methodname, args);
			#endif
		}

		public static T CallExternalAndroid<T>(string methodname, params object[] args)
		{
			#if UNITY_ANDROID && !UNITY_EDITOR
			AndroidJavaClass	unity			= new AndroidJavaClass ("com.unity3d.player.UnityPlayer");
			AndroidJavaObject	currentActivity	= unity.GetStatic<AndroidJavaObject>("currentActivity");
			return currentActivity.Call<T>(methodname, args);
			#else
			return default(T);
			#endif
		}
		
		public static string ConvertToJsonString(object data, bool addQuoteEscapes = false)
		{
			string jsonString = "";
			
			if (data is IDictionary)
			{
				string dictionaryItems = "";

				foreach (DictionaryEntry item in (data as IDictionary))
				{
					if (!string.IsNullOrEmpty(dictionaryItems))
					{
						dictionaryItems += ",";
					}

					if (addQuoteEscapes)
					{
						dictionaryItems += string.Format("\\\"{0}\\\":{1}", item.Key, ConvertToJsonString(item.Value, addQuoteEscapes));
					}
					else
					{
						dictionaryItems += string.Format("\"{0}\":{1}", item.Key, ConvertToJsonString(item.Value, addQuoteEscapes));
					}
				}

				jsonString += "{" + dictionaryItems + "}";
			}
			else if (data is IList)
			{
				IList list = data as IList;
				
				jsonString += "[";
				
				for (int i = 0; i < list.Count; i++)
				{
					if (i != 0)
					{
						jsonString += ",";
					}
					
					jsonString += ConvertToJsonString(list[i], addQuoteEscapes);
				}
				
				jsonString += "]";
			}
			else if (data is string || data is char)
			{
				// If the data is a string then we need to inclose it in quotation marks
				if (addQuoteEscapes)
				{
					jsonString += "\\\"" + data + "\\\"";
				}
				else
				{
					jsonString += "\"" + data + "\"";
				}
			}
			else if (data is bool)
			{
				jsonString += (bool)data ? "true" : "false";
			}
			else
			{
				// Else just return what ever data is as a string
				jsonString += data.ToString();
			}
			
			return jsonString;
		}

		public static void SetLayer(GameObject gameObject, int layer, bool applyToChildren = false)
		{
			gameObject.layer = layer;

			if (applyToChildren)
			{
				for (int i = 0; i < gameObject.transform.childCount; i++)
				{
					SetLayer(gameObject.transform.GetChild(i).gameObject, layer, true);
				}
			}
		}

		public static List<string[]> ParseCSVFile(string fileContents, char delimiter)
		{
			List<string[]>	csvText	= new List<string[]>();
			string[]		lines	= fileContents.Split('\n');

			for (int i = 0; i < lines.Length; i++)
			{
				csvText.Add(lines[i].Split(delimiter));
			}

			return csvText;
		}

		public static void DestroyAllChildren(Transform parent)
		{
			for (int i = parent.childCount - 1; i >= 0; i--)
			{
				GameObject.Destroy(parent.GetChild(i).gameObject);
			}
		}

		public static string FindFile(string fileName, string directory)
		{
			List<string>	files		= new List<string>(System.IO.Directory.GetFiles(directory));
			string[]		directories	= System.IO.Directory.GetDirectories(directory);

			for (int i = 0; i < files.Count; i++)
			{
				if (fileName == System.IO.Path.GetFileNameWithoutExtension(files[i]))
				{
					return files[i];
				}
			}

			for (int i = 0; i < directories.Length; i++)
			{
				string path = FindFile(fileName, directories[i]);

				if (!string.IsNullOrEmpty(path))
				{
					return path;
				}
			}
			
			return null;
		}

		public static string CalculateMD5Hash(string input)
		{
			System.Security.Cryptography.MD5	md5			= System.Security.Cryptography.MD5.Create();
			byte[]								inputBytes	= System.Text.Encoding.ASCII.GetBytes(input);
			byte[]								hash		= md5.ComputeHash(inputBytes);
			System.Text.StringBuilder			sb			= new System.Text.StringBuilder();
			
			for (int i = 0; i < hash.Length; i++)
			{
				sb.Append(hash[i].ToString("x2"));
			}
			
			return sb.ToString();
		}

		public static bool CompareLists<T>(List<T> list1, List<T> list2)
		{
			if (list1.Count != list2.Count)
			{
				return false;
			}

			for (int i = list1.Count - 1; i >= 0; i--)
			{
				bool found = false;

				for (int j = 0; j < list2.Count; j++)
				{
					if (list1[i].Equals(list2[j]))
					{
						found = true;
						list1.RemoveAt(i);
						list2.RemoveAt(j);
						break;
					}
				}

				if (!found)
				{
					return false;
				}
			}

			return true;
		}

		public static void PrintList<T>(List<T> list)
		{
			string str = "";

			for (int i = 0; i < list.Count; i++)
			{
				if (i != 0)
				{
					str += ", ";
				}

				str += list[i].ToString();
			}

			Debug.Log(str);
		}

		public static Vector2 Rotate(Vector2 v, float degrees)
		{
			float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
			float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

			float tx = v.x;
			float ty = v.y;

			v.x = (cos * tx) - (sin * ty);
			v.y = (sin * tx) + (cos * ty);

			return v;
		}

		/// <summary>
		/// Creates a new texture with the given width, height, and base color
		/// </summary>
		public static Texture2D CreateTexture(int width, int height, Color color)
		{
			Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);

			texture.filterMode = FilterMode.Point;

			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					texture.SetPixel(x, y, color);
				}
			}

			texture.Apply();

			return texture;
		}

		public static List<string> GetFilesRecursively(string path, string searchPatter)
		{
			List<string> files = new List<string>();

			if (!System.IO.Directory.Exists(path))
			{
				return files;
			}

			List<string> directories = new List<string>() { path };

			while (directories.Count > 0)
			{
				string directory = directories[0];

				directories.RemoveAt(0);

				files.AddRange(System.IO.Directory.GetFiles(directory, searchPatter));
				directories.AddRange(System.IO.Directory.GetDirectories(directory));
			}

			return files;
		}

		public static Vector2 SwitchToRectTransform(RectTransform from, RectTransform to)
		{
			Vector2 localPoint;
			Vector2 fromPivotDerivedOffset	= new Vector2(from.rect.width * from.pivot.x + from.rect.xMin, from.rect.height * from.pivot.y + from.rect.yMin);
			Vector2 screenP					= RectTransformUtility.WorldToScreenPoint(null, from.position);

			screenP += fromPivotDerivedOffset;

			RectTransformUtility.ScreenPointToLocalPointInRectangle(to, screenP, null, out localPoint);

			Vector2 pivotDerivedOffset = new Vector2(to.rect.width * to.pivot.x + to.rect.xMin, to.rect.height * to.pivot.y + to.rect.yMin);

			return localPoint - pivotDerivedOffset;
		}

		public static Camera GetCanvasCamera(Transform canvasChild)
		{
			Canvas canvas = Utilities.GetCanvas(canvasChild);

			if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
			{
				return canvas.worldCamera;
			}

			return null;
		}

		public static void SetAlpha(UnityEngine.UI.Graphic graphic, float alpha)
		{
			Color color = graphic.color;

			color.a = alpha;

			graphic.color = color;
		}

		public static string FormatTimer(float elapsedTime)
		{
			int mins = Mathf.FloorToInt(elapsedTime / 60f);
			int secs = (int)elapsedTime - mins * 60;

			return string.Format("{0}:{1}", mins.ToString("00"), secs.ToString("00"));
		}

		/// <summary>
		/// Uses XOR encryption to encrypt and decrypt the given text
		/// </summary>
		public static string EncryptDecrypt(string inText, int key)
		{            
			string outText = "";

			for (int i = 0; i < inText.Length; i++)
			{
				char c = inText[i];

				c = (char)(c ^ key);

				outText += c;
			}

			return outText;
		}   

		public static void SetGridCellSize(UnityEngine.UI.GridLayoutGroup gridLayoutGroup)
		{
			RectTransform rectT = gridLayoutGroup.transform as RectTransform;

			bool cols = gridLayoutGroup.constraint == UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;

			float screenSize = cols ? rectT.rect.width : rectT.rect.height;

			// Remove the space reserved for padding and spacing
			if (cols)
			{
				screenSize -= gridLayoutGroup.padding.left + gridLayoutGroup.padding.right + gridLayoutGroup.spacing.x * (gridLayoutGroup.constraintCount - 1);
			}
			else
			{
				screenSize -= gridLayoutGroup.padding.top + gridLayoutGroup.padding.bottom + gridLayoutGroup.spacing.y * (gridLayoutGroup.constraintCount - 1);
			}

			// Calculate the cell size based on the number of columns in the grid
			float cellSize = screenSize / gridLayoutGroup.constraintCount;

			gridLayoutGroup.cellSize = new Vector2(cellSize, cellSize);
		}

		#endregion
	}
}
