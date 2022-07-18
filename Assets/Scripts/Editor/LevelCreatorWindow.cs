using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;

namespace BizzyBeeGames.PictureColoring
{
	public class LevelCreatorWindow : EditorWindow
	{
		#region Enums

		private enum ImportMode
		{
			Single,
			Batch
		}

		#endregion

		#region Member Variables

		private ImportMode	importMode;

		private Texture2D	levelLineTexture;
		private Texture2D	levelColoredTexture;

		private string		batchModeInputFolder;

		private bool		ignoreWhiteRegions;
		private int			lineDarknessThreshold	= 200;
		private int			regionSizeThreshold		= 24;
		private float		colorMergeThreshold		= 0.1f;

		private Object		outputFolder;
		private string		filename;

		private GameManager	gameManagerReference;
		private bool		addToGameManager = true;
		private int			selectedCategoryIndex;

		private string		errorMessage;

		private List<string>		batchColoredFiles;
		private List<string>		batchLineFiles;
		private LevelCreatorWorker	levelCreatorWorker;

		#endregion

		#region Properties

		private string OutputFolderAssetPath
		{
			get { return EditorPrefs.GetString("OutputFolderAssetPath", ""); }
			set { EditorPrefs.SetString("OutputFolderAssetPath", value); }
		}

		#endregion

		#region Unity Methods

		[MenuItem("Tools/Bizzy Bee Games/Level Creator Window", priority = 200)]
		public static void Init()
		{
			EditorWindow.GetWindow<LevelCreatorWindow>("Level Creator");
		}

		private void OnEnable()
		{
			if (outputFolder == null && !string.IsNullOrEmpty(OutputFolderAssetPath))
			{
				outputFolder = AssetDatabase.LoadAssetAtPath<Object>(OutputFolderAssetPath);
			}

			// Set the reference to the GameManager in the current open scene
			gameManagerReference = FindObjectOfType<GameManager>();
		}

		private void Update()
		{
			if (gameManagerReference == null)
			{
				gameManagerReference = FindObjectOfType<GameManager>();
			}

			if (levelCreatorWorker != null)
			{
				if (levelCreatorWorker.Stopped)
				{
					Debug.Log("Level creator finished");

					EditorUtility.ClearProgressBar();

					AssetDatabase.Refresh();

					string path = (importMode == ImportMode.Single) ? levelCreatorWorker.settings.outPath : batchModeInputFolder;

					AddLevelToGameManager(path);

					levelCreatorWorker = null;

					return;
				}

				if (importMode == ImportMode.Batch && levelCreatorWorker.BatchNeedImagePixels)
				{
					LoadAndSetWorkerBatchPixels();
				}

				bool cancelled = DisplayProgressBar();

				if (cancelled)
				{
					Debug.Log("Cancelling");

					levelCreatorWorker.Stop();
					levelCreatorWorker = null;

					EditorUtility.ClearProgressBar();
				}
			}
		}

		#endregion

		#region Draw Methods

		private void OnGUI()
		{
			EditorGUILayout.Space();

			BeginBox();

			GUI.enabled = levelCreatorWorker == null;

			GUILayout.Space(2);

			importMode = (ImportMode)EditorGUILayout.EnumPopup("Import Mode", importMode);

			if (importMode == ImportMode.Single)
			{
				EditorGUILayout.Space();

				levelColoredTexture		= EditorGUILayout.ObjectField("Colored Texture", levelColoredTexture, typeof(Texture2D), false, GUILayout.Height(16)) as Texture2D;
				levelLineTexture		= EditorGUILayout.ObjectField("Line Texture", levelLineTexture, typeof(Texture2D), false, GUILayout.Height(16)) as Texture2D;
			}
			else
			{
				EditorGUILayout.HelpBox("To use batch mode, select the folder with your colored and line images. The name of your line image file should be the name of the colored image file with \"-lines\" append to the end. For example if the colored images name is mandala.png then the line image files name should be mandala-lines.png", MessageType.Info);

				EditorGUILayout.Space();

				if (GUILayout.Button("Choose Input Folder"))
				{
					string folder			= string.IsNullOrEmpty(batchModeInputFolder) ? Application.dataPath : batchModeInputFolder;
					string inputFolder		= EditorUtility.OpenFolderPanel("Choose Input Folder", folder, "");

					if (!string.IsNullOrEmpty(inputFolder) && inputFolder != batchModeInputFolder)
					{
						errorMessage = "";

						batchModeInputFolder = inputFolder;

						List<string> missingLineFiles = UpdateBatchFiles();

						if (missingLineFiles != null)
						{
							string errorMessage = "Could not find line files for the following colored image files:";

							for (int i = 0; i < missingLineFiles.Count; i++)
							{
								errorMessage += "\n" + missingLineFiles[i];
							}

							Debug.LogError(errorMessage);
						}
					}
				}

				string message = "";

				if (string.IsNullOrEmpty(batchModeInputFolder))
				{
					message = "<Please choose an input folder>";
				}
				else
				{
					message = batchModeInputFolder + "\n\nImage files found: " + batchColoredFiles.Count;
				}

				EditorGUILayout.HelpBox(message, MessageType.None);
			}

			EditorGUILayout.Space();

			lineDarknessThreshold	= EditorGUILayout.IntSlider("Line Darkness Threshold", lineDarknessThreshold, 255, 0);
			ignoreWhiteRegions		= EditorGUILayout.Toggle("Ignore White Regions", ignoreWhiteRegions);
			regionSizeThreshold		= EditorGUILayout.IntField("Region Size Threshold", regionSizeThreshold);
			colorMergeThreshold		= EditorGUILayout.FloatField("Color Merge Threshold", colorMergeThreshold);

			EditorGUILayout.Space();

			if (importMode == ImportMode.Single)
			{
				filename = EditorGUILayout.TextField("Filename", filename);
			}

			outputFolder = EditorGUILayout.ObjectField("Output Folder", outputFolder, typeof(Object), false);

			OutputFolderAssetPath = (outputFolder != null) ? AssetDatabase.GetAssetPath(outputFolder) : null;

			EditorGUILayout.HelpBox("Files will be placed in the folder: " + GetOutputFolderPath(outputFolder).Remove(0, Application.dataPath.Length + 1), MessageType.None);

			EditorGUILayout.Space();

			if (gameManagerReference == null)
			{
				EditorGUILayout.HelpBox("Could not find a GameManager in the current open scene.", MessageType.Warning);

				gameManagerReference = EditorGUILayout.ObjectField("Game Manager", gameManagerReference, typeof(GameManager), true) as GameManager;
			}
			else
			{
				if (gameManagerReference.Categories == null || gameManagerReference.Categories.Count == 0)
				{
					EditorGUILayout.HelpBox("GameManager has no categories, create categories on the GameManagers inspector to assign levels to categories.", MessageType.Warning);
				}
				else
				{
					addToGameManager = EditorGUILayout.Toggle("Add To GameManager", addToGameManager);

					if (addToGameManager)
					{
						string[] categoryNames = new string[gameManagerReference.Categories.Count];

						for (int i = 0; i < gameManagerReference.Categories.Count; i++)
						{
							categoryNames[i] = gameManagerReference.Categories[i].displayName;
						}

						selectedCategoryIndex = EditorGUILayout.Popup("Category", selectedCategoryIndex, categoryNames);
					}
					else
					{
						GUI.enabled = false;
						selectedCategoryIndex = EditorGUILayout.Popup("Category", selectedCategoryIndex, new string[] { "" });
						GUI.enabled = true;
					}
				}
			}

			EditorGUILayout.Space();

			if (GUILayout.Button("Create Level Files") && Check())
			{
				if (importMode == ImportMode.Single)
				{
					Process(levelColoredTexture, levelLineTexture);
				}
				else
				{
					ProcessBatch();
				}
			}

			if (!string.IsNullOrEmpty(errorMessage))
			{
				EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
			}

			GUI.enabled = true;

			EndBox();

			EditorGUILayout.Space();
		}

		/// <summary>
		/// Begins a new box, must call EndBox
		/// </summary>
		private void BeginBox()
		{
			GUILayout.BeginVertical(GUI.skin.box);
		}

		/// <summary>
		/// Ends the box.
		/// </summary>
		private void EndBox()
		{
			GUILayout.EndVertical();
		}

		#endregion

		#region Private Methods

		private bool Check()
		{
			errorMessage = "";

			if (importMode == ImportMode.Batch)
			{
				if (string.IsNullOrEmpty(batchModeInputFolder))
				{
					errorMessage = "Please choose an import folder";
					return false;
				}

				if (batchColoredFiles.Count == 0)
				{
					errorMessage = "There are no images in the selected input folder";
					return false;
				}

				return true;
			}

			if (levelColoredTexture == null && levelLineTexture == null)
			{
				errorMessage = "Please specify a Colored Texture and a Line Texture";
				return false;
			}
			else if (levelColoredTexture == null)
			{
				errorMessage = "Please specify a Colored Texture";
				return false;
			}
			else if (levelLineTexture == null)
			{
				errorMessage = "Please specify a Line Texture";
				return false;
			}

			if (levelLineTexture.width != levelColoredTexture.width ||
			    levelLineTexture.height != levelColoredTexture.height)
			{
				errorMessage = "The Colored Texture and Line Texture are not the same size. The images need to be the same width/height.";
				return false;
			}

			bool isColoredTextureReadable	= CheckIsReadWriteEnabled(levelColoredTexture);
			bool isLineTextureReadable		= CheckIsReadWriteEnabled(levelLineTexture);

			if (!isColoredTextureReadable && !isLineTextureReadable)
			{
				errorMessage = "The Colored Texture and Line Texture are not read/write enabled. Please select the texture in the Project window then in the Inspector window select Read/Write Enabled and click Apply.";
				return false;
			}
			else if (!isColoredTextureReadable)
			{
				errorMessage = "The Colored Texture is not read/write enabled. Please select the texture in the Project window then in the Inspector window select Read/Write Enabled and click Apply.";
				return false;
			}
			else if (!isLineTextureReadable)
			{
				errorMessage = "The Line Texture is not read/write enabled. Please select the texture in the Project window then in the Inspector window select Read/Write Enabled and click Apply.";
				return false;
			}

			return true;
		}

		private void Process(Texture2D coloredTexture, Texture2D lineTexture)
		{
			string folderPath	= GetOutputFolderPath(outputFolder);
			string outFilename	= GetFilenameToUse(folderPath);
			string outPath		= folderPath + "/" + outFilename;

			if (System.IO.File.Exists(outPath + ".txt"))
			{
				bool replace = EditorUtility.DisplayDialog("Filename Already Exists", "The file \"" + outFilename + "\" already exists in the output folder, would you like to replace it?", "Yes", "Cancel");

				if (!replace)
				{
					return;
				}
			}

			if (!System.IO.Directory.Exists(folderPath))
			{
				System.IO.Directory.CreateDirectory(folderPath);
			}

			LevelCreatorWorker.Settings settings = new LevelCreatorWorker.Settings();

			settings.lineTexturePixels		= levelLineTexture.GetPixels();
			settings.colorTexturePixels		= levelColoredTexture.GetPixels();
			settings.imageSize				= new Vector2(levelColoredTexture.width, levelColoredTexture.height);
			settings.lineThreshold			= lineDarknessThreshold;
			settings.ignoreWhiteRegions		= ignoreWhiteRegions;
			settings.regionSizeThreshold	= regionSizeThreshold;
			settings.colorMergeThreshold	= colorMergeThreshold;
			settings.outPath				= outPath;

			levelCreatorWorker = new LevelCreatorWorker(settings);
			levelCreatorWorker.StartWorker();
		}

		private void ProcessBatch()
		{
			string folderPath = GetOutputFolderPath(outputFolder);

			if (!System.IO.Directory.Exists(folderPath))
			{
				System.IO.Directory.CreateDirectory(folderPath);
			}

			LevelCreatorWorker.Settings settings = new LevelCreatorWorker.Settings();

			settings.lineThreshold			= lineDarknessThreshold;
			settings.ignoreWhiteRegions		= ignoreWhiteRegions;
			settings.regionSizeThreshold	= regionSizeThreshold;
			settings.colorMergeThreshold	= colorMergeThreshold;
			settings.outPath				= folderPath;

			levelCreatorWorker = new LevelCreatorWorker(settings, batchColoredFiles, batchLineFiles);
			levelCreatorWorker.StartWorker();
		}
        
		/// <summary>
		/// Gets the full path to the output folder
		/// </summary>
		private string GetOutputFolderPath(Object outputFolder)
		{
			string folderPath = GetFolderAssetPath(outputFolder);

			// If the folder path is null then set the path to the Resources folder
			if (string.IsNullOrEmpty(folderPath))
			{
				return Application.dataPath + "/Resources";
			}

			if (!folderPath.EndsWith("/Resources", System.StringComparison.Ordinal) && !folderPath.Contains("/Resources/"))
			{
				folderPath += "/Resources";
			}

			return Application.dataPath + "/" + folderPath;
		}

		/// <summary>
		/// Gets the folder path.
		/// </summary>
		private string GetFolderAssetPath(Object folderObject)
		{
			if (folderObject != null)
			{
				// Get the full system path to the folder
				string fullPath = Application.dataPath.Remove(Application.dataPath.Length - "Assets".Length) + UnityEditor.AssetDatabase.GetAssetPath(folderObject);

				// If it's not a folder then set the path to null so the default path is choosen
				if (!System.IO.Directory.Exists(fullPath))
				{
					return "";
				}

				return UnityEditor.AssetDatabase.GetAssetPath(folderObject).Remove(0, "Assets/".Length);
			}

			return "";
		}

		/// <summary>
		/// Checks if the given texture is read/write enabled in its settings
		/// </summary>
		private bool CheckIsReadWriteEnabled(Texture2D texture)
		{
			if (texture == null)
			{
				return false;
			}

			string			assetPath	= AssetDatabase.GetAssetPath(texture);
			TextureImporter	importer	= AssetImporter.GetAtPath(assetPath) as TextureImporter;

			return importer.isReadable;
		}

		/// <summary>
		/// Gets the filename to use for the level files
		/// </summary>
		private string GetFilenameToUse(string folderPath)
		{
			if (!string.IsNullOrEmpty(filename))
			{
				return filename;
			}

			return levelColoredTexture.name;
		}

		/// <summary>
		/// Updates the list of batch file paths
		/// </summary>
		private List<string> UpdateBatchFiles()
		{
			batchColoredFiles	= new List<string>();
			batchLineFiles		= new List<string>();

			if (System.IO.Directory.Exists(batchModeInputFolder))
			{
				string[] files = System.IO.Directory.GetFiles(batchModeInputFolder, "*.png");

				List<string> coloredFiles	= new List<string>();
				List<string> lineFiles		= new List<string>();

				// Gather all the colored and line files
				for (int i = 0; i < files.Length; i++)
				{
					string filePath = files[i];

					if (System.IO.Path.GetFileNameWithoutExtension(filePath).EndsWith("-lines"))
					{
						lineFiles.Add(filePath);
					}
					else
					{
						coloredFiles.Add(filePath);
					}
				}

				List<string> missingLineFiles = new List<string>();

				// Check that each colored file has a line file
				for (int i = 0; i < coloredFiles.Count; i++)
				{
					string coloredFile		= coloredFiles[i];
					string coloredFileName	= System.IO.Path.GetFileNameWithoutExtension(coloredFile);

					bool foundLineFile = false;

					for (int j = 0; j < lineFiles.Count; j++)
					{
						string lineFile		= lineFiles[j];
						string lineFileName = System.IO.Path.GetFileNameWithoutExtension(lineFile);

						if (coloredFileName + "-lines" == lineFileName)
						{
							batchColoredFiles.Add(coloredFile);
							batchLineFiles.Add(lineFile);
							foundLineFile = true;
							break;
						}
					}

					if (!foundLineFile)
					{
						missingLineFiles.Add(coloredFileName);
					}
				}

				return missingLineFiles.Count == 0 ? null : missingLineFiles;
			}
			else
			{
				batchModeInputFolder = "";
			}

			return null;
		}

		private bool DisplayProgressBar()
		{
			string title	= "Creating Level Files";
			string message	= "";

			if (importMode == ImportMode.Batch)
			{
				title = string.Format("Process image {0} of {1}: {2}", levelCreatorWorker.ProgressCurBatchFile + 1, batchColoredFiles.Count, levelCreatorWorker.ProgressBatchFilename);
			}

			LevelCreatorWorker.AlgoProgress.Step step = levelCreatorWorker.ProgressStep;

			int curRegion		= 0;
			int totalRegions	= 0;
			int pointsLeft		= 0;
			int totalPoints		= 0;

			ulong	time = ((ulong)Utilities.SystemTimeInMilliseconds / 300);
			int		dots = (int)(time % 3) + 1;

			switch (step)
			{
				case LevelCreatorWorker.AlgoProgress.Step.LoadingTextures:
				{
					message = AddDots("Loading images", dots);
					break;
				}
				case LevelCreatorWorker.AlgoProgress.Step.GatheringRegions:
				{
					message = AddDots("Parsing textures into pixel regions", dots);
					break;
				}
				case LevelCreatorWorker.AlgoProgress.Step.GetPoints:
				{
					curRegion		= levelCreatorWorker.ProgressCurrentRegion;
					totalRegions	= levelCreatorWorker.ProgressTotalRegions;
					message			= AddDots(string.Format("Region {0} of {1}: Converting boundry pixels to vector points", curRegion + 1, totalRegions), dots);

					break;
				}
				case LevelCreatorWorker.AlgoProgress.Step.Triangulation:
				{
					curRegion		= levelCreatorWorker.ProgressCurrentRegion;
					totalRegions	= levelCreatorWorker.ProgressTotalRegions;
					pointsLeft		= levelCreatorWorker.ProgressPointsLeft;
					totalPoints		= levelCreatorWorker.ProgressTotalPoints;
					message			= string.Format("Region {0} of {1}: Generating triangles from vector points... {2} points left", curRegion + 1, totalRegions, pointsLeft);

					break;
				}
			}

			float progress = 0;

			if (step != LevelCreatorWorker.AlgoProgress.Step.GatheringRegions)
			{
				float t = 1f / totalRegions;

				progress = curRegion * t;

				if (step == LevelCreatorWorker.AlgoProgress.Step.Triangulation)
				{
					progress += Mathf.Lerp(0, t, 1f - (float)pointsLeft / (float)totalPoints);
				}
			}

			return EditorUtility.DisplayCancelableProgressBar(title, message, progress);
		}

		private string AddDots(string message, int dots)
		{
			for (int i = 0; i < dots; i++)
			{
				message += ".";
			}

			return message;
		}

		private void LoadAndSetWorkerBatchPixels()
		{
			string lineFile		= levelCreatorWorker.BatchLineFilePath;
			string coloredFile	= levelCreatorWorker.BatchColoredFilePath;

			Texture2D lineTexture		= new Texture2D(1, 1);
			Texture2D coloredTexture	= new Texture2D(1, 1);

			ImageConversion.LoadImage(lineTexture, System.IO.File.ReadAllBytes(lineFile));
			ImageConversion.LoadImage(coloredTexture, System.IO.File.ReadAllBytes(coloredFile));

			if (lineTexture.width != coloredTexture.width || lineTexture.height != coloredTexture.height)
			{
				levelCreatorWorker.BatchLoadTextureError = string.Format("Error loading {0}, the colored and line images are not the same size.", System.IO.Path.GetFileNameWithoutExtension(coloredFile));
			}
			else
			{
				levelCreatorWorker.BatchLineTexturePixels	= lineTexture.GetPixels();
				levelCreatorWorker.BatchColorTexturePixels	= coloredTexture.GetPixels();
				levelCreatorWorker.BatchImageSize			= new Vector2(lineTexture.width, lineTexture.height);
			}

			DestroyImmediate(lineTexture);
			DestroyImmediate(coloredTexture);

			levelCreatorWorker.BatchNeedImagePixels = false;
		}

		private void AddLevelToGameManager(string path)
		{
			// Add the level file TextAsset to the selected category in the GameManager
			if (addToGameManager && gameManagerReference != null && gameManagerReference.Categories != null && selectedCategoryIndex < gameManagerReference.Categories.Count)
			{
				CategoryData categoryData = gameManagerReference.Categories[selectedCategoryIndex];

				if (categoryData.levels == null)
				{
					categoryData.levels = new List<LevelData>();
				}

				List<string> levelsToAdd = new List<string>();

				if (importMode == ImportMode.Single)
				{
					levelsToAdd.Add(path.Remove(0, Application.dataPath.Length - "Assets".Length) + ".txt");
				}
				else
				{
					levelsToAdd.AddRange(System.IO.Directory.GetFiles(path, "*.png"));

					for (int i = levelsToAdd.Count - 1; i >= 0; i--)
					{
						string filename = System.IO.Path.GetFileNameWithoutExtension(levelsToAdd[i]);

						if (filename.EndsWith("-lines"))
						{
							levelsToAdd.RemoveAt(i);
						}
						else
						{
							levelsToAdd[i] = "Assets/" + GetFolderAssetPath(outputFolder) + "/" + filename + ".txt";
						}
					}
				}

				for (int i = 0; i < levelsToAdd.Count; i++)
				{
					string		levelFileAssetPath	= levelsToAdd[i];
					TextAsset	levelFileAsset		= AssetDatabase.LoadAssetAtPath<TextAsset>(levelFileAssetPath);

					bool alreadyExists = false;

					for (int j = 0; j < categoryData.levels.Count; j++)
					{
						if (categoryData.levels[j].levelFile == levelFileAsset)
						{
							alreadyExists = true;
							break;
						}
					}

					if (!alreadyExists)
					{
						LevelData levelData = new LevelData();
						levelData.levelFile = levelFileAsset;

						categoryData.levels.Add(levelData);
					}
				}
			}
		}

		#endregion
	}
}
