using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class LABColor
{
	public float	l;
	public float	a;
	public float	b;

	public LABColor(Color color)
	{
		TextureUtilities.RGBToLAB(color, out l, out a, out b);
	}
}

public class PaletteItem
{
	public Color		color		{ get; private set; }
	public LABColor 	labColor	{ get; private set; }
	public List<int>	xCoords		{ get; private set; }
	public List<int>	yCoords		{ get; private set; }

	public PaletteItem(Color c)
	{
		color		= c;
		labColor	= new LABColor(color);
		xCoords		= new List<int>();
		yCoords		= new List<int>();
	}

	public PaletteItem(Color c, int xPixel, int yPixel)
	{
		color		= c;
		labColor	= new LABColor(color);

		xCoords		= new List<int>() { xPixel };
		yCoords		= new List<int>() { yPixel };
	}

	public void AddCoords(int x, int y)
	{
		xCoords.Add(x);
		yCoords.Add(y);
	}

	public void AddCoords(List<int> x, List<int> y)
	{
		xCoords.AddRange(x);
		yCoords.AddRange(y);
	}

	public void Merge(Color mergeColor, int xPixel, int yPixel)
	{
		float totalPixels = xCoords.Count + 1;

		float c1Amount = (float)xCoords.Count / totalPixels;
		float c2Amount = 1f / totalPixels;

		float r = color.r * c1Amount + mergeColor.r * c2Amount;
		float g = color.g * c1Amount + mergeColor.g * c2Amount;
		float b = color.b * c1Amount + mergeColor.b * c2Amount;

		color		= new Color(r, g, b, 1f);
		labColor	= new LABColor(color);

		xCoords.Add(xPixel);
		yCoords.Add(yPixel);
	}

	public void Merge(PaletteItem paletteItem)
	{
		float totalPixels = xCoords.Count + paletteItem.xCoords.Count;

		float c1Amount = (float)xCoords.Count / totalPixels;
		float c2Amount = (float)paletteItem.xCoords.Count / totalPixels;

		float r = color.r * c1Amount + paletteItem.color.r * c2Amount;
		float g = color.g * c1Amount + paletteItem.color.g * c2Amount;
		float b = color.b * c1Amount + paletteItem.color.b * c2Amount;

		color		= new Color(r, g, b, 1f);
		labColor	= new LABColor(color);

		xCoords.AddRange(paletteItem.xCoords);
		yCoords.AddRange(paletteItem.yCoords);
	}

	public static PaletteItem Merge(PaletteItem paletteItem1, PaletteItem paletteItem2)
	{
		PaletteItem paletteItem = new PaletteItem(paletteItem1.color);

		paletteItem.AddCoords(paletteItem1.xCoords, paletteItem1.yCoords);

		paletteItem.Merge(paletteItem2);

		return paletteItem;
	}
}

public static class TextureUtilities
{
	#region Classes

	public class PaletteItemDiff
	{
		public PaletteItem	paletteItem;
		public float		diff;
	}

	#endregion

	#region Enums

	public enum ScaleType
	{
		CenterPixel,
		BoxSampling
	}

	#endregion

	#region Public Methods

	/// <summary>
	/// Trims any 100% alpha pixels off the edge of the given texture
	/// </summary>
	public static Texture2D TrimAlpha(Texture2D texture)
	{
		int top		= int.MinValue;
		int bottom	= int.MaxValue;
		int left	= int.MaxValue;
		int right	= int.MinValue;

		for (int x = 0; x < texture.width; x++)
		{
			for (int y = 0; y < texture.height; y++)
			{
				Color color = texture.GetPixel(x, y);

				if (color.a != 0)
				{
					top		= Mathf.Max(top, y);
					bottom	= Mathf.Min(bottom, y);
					left	= Mathf.Min(left, x);
					right	= Mathf.Max(right, x);
				}
			}
		}

		int width	= right - left + 1;
		int height	= top - bottom + 1;

		Texture2D trimmedTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);

		trimmedTexture.filterMode = FilterMode.Point;

		trimmedTexture.SetPixels(texture.GetPixels(left, bottom, width, height));

		trimmedTexture.Apply();

		return trimmedTexture;
	}

	public static Color[] Scale(Color[] inPixels, int width, int height, int scaleToWidth, int scaleToHeight)
	{
		float xScale = (float)width / (float)scaleToWidth;
		float yScale = (float)height / (float)scaleToHeight;

		Color[] outPixels = new Color[scaleToWidth * scaleToHeight];

		for (int x = 0; x < scaleToWidth; x++)
		{
			for (int y = 0; y < scaleToHeight; y++)
			{
				int xStart	= Mathf.RoundToInt((float)x * xScale);
				int yStart	= Mathf.RoundToInt((float)y * yScale);
				int xEnd	= Mathf.RoundToInt(((float)x + 1f) * xScale);
				int yEnd	= Mathf.RoundToInt(((float)y + 1f) * yScale);

				outPixels[x + y * scaleToWidth] = GetColorAverage(inPixels, width, height, xStart, yStart, xEnd - xStart, yEnd - yStart);
			}
		}

		return outPixels;
	}

	/// <summary>
	/// Gets the PaletteItem that is closest to the given PaletteItem
	/// </summary>
	public static PaletteItem GetClosestPaletteItem(Color color, List<PaletteItem> palette, out float diff)
	{
		PaletteItem	closestPaletteItem	= null;
		float		minDiff				= float.MaxValue;

		for (int i = 0; i < palette.Count; i++)
		{
			PaletteItem paletteItem = palette[i];

			if (color == paletteItem.color)
			{
				diff = 0f;

				return paletteItem;
			}

			float colorDiff = GetColorDiff(color, paletteItem.color);

			if (colorDiff < minDiff)
			{
				closestPaletteItem	= paletteItem;
				minDiff				= colorDiff;
			}
		}

		diff = minDiff;

		return closestPaletteItem;
	}

	/// <summary>
	/// Gets the average color of all the pixels in the box defined by the given xStart/yStart, width/height
	/// </summary>
	public static Color GetColorAverage(Color[] colors, int width, int height, int xStart, int yStart, int blockWidth, int blockHeight)
	{
		float totalAlpha = 0f;
		float num = 0;

		for (int x = 0; x < blockWidth; x++)
		{
			for (int y = 0; y < blockHeight; y++)
			{
				int		index	= (y + yStart) * width + (x + xStart);
				Color	color	= colors[index];

				totalAlpha += color.a;

				num++;
			}
		}

		return new Color(0, 0, 0, totalAlpha / num);
	}

	/// <summary>
	/// Returns the difference between the colors as a float
	/// </summary>
	public static float GetColorDiff2(Color color1, Color color2)
	{
		// Now we calcuate the difference based on the standard CIE76 formula
		float rDiff		= color1.r - color2.r;
		float gDiff		= color1.g - color2.g;
		float bDiff		= color1.b - color2.b;
		float colorDiff	= Mathf.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);

		return colorDiff;
	}

	/// <summary>
	/// Returns the difference between the colors as a float
	/// </summary>
	public static float GetColorDiff(Color color1, Color color2)
	{
		// First we need to conver the RGB colors to LAB color space
		float l1, a1, b1;
		float l2, a2, b2;

		RGBToLAB(color1, out l1, out a1, out b1);
		RGBToLAB(color2, out l2, out a2, out b2);

		// Now we calcuate the difference based on the standard CIE76 formula
		float lDiff		= l2 - l1;
		float aDiff		= a2 - a1;
		float bDiff		= b2 - b1;
		float colorDiff	= Mathf.Sqrt(lDiff * lDiff + aDiff * aDiff + bDiff * bDiff);

		return colorDiff;
	}

	/// <summary>
	/// Converts an RGB color to the LAB color space
	/// </summary>
	public static void RGBToLAB(Color color, out float l, out float a, out float b)
	{
		// First we need to convert the RGB color to the xyz color space then we convert that to the LAB space
		float x, y, z;

		RGBToXYZ(color, out x, out y, out z);

		// Now conver the x,y,z to l,a,b
		XYZToLAB(x, y, z, out l, out a, out b);
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

	/// <summary>
	/// Scales for texture.
	/// </summary>
	public static void ScaleForTexture(Texture2D texture, Transform transform)
	{
		float xScale	= 1;
		float yScale	= 1;

		// Set the scale of the textureImage so it matches the textures aspect ratio and fits in it's parent
		if (texture.width > texture.height)
		{
			yScale = (float)texture.height / (float)texture.width;
		}
		else
		{
			xScale = (float)texture.width / (float)texture.height;
		}

		transform.localScale = new Vector3(xScale, yScale, 1f);
	}

	/// <summary>
	/// Gets a hash value for the given Texture2D
	/// </summary>
	public static string Texture2DHash(Texture2D texture)
	{
		// encrypt bytes
		MD5CryptoServiceProvider	md5			= new MD5CryptoServiceProvider();
		byte[]						hashBytes	= md5.ComputeHash(texture.EncodeToPNG());

		// Convert the encrypted bytes back to a string (base 16)
		string hashString = "";

		for (int i = 0; i < hashBytes.Length; i++)
		{
			hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
		}

		return hashString.PadLeft(32, '0');
	}

	/// <summary>
	/// Exports the palette to a csv file
	/// </summary>
	public static string ExportTextureToFile(
		string 				id,
		int					xPixels,
		int					yPixels,
		List<PaletteItem>	palette,
		string				outputFolderPath,
		string				filename,
		int					blankItemIndex		= -1,
		bool				isLevelLocked		= false,
		int					unlockAmount		= 0,
		bool				awardOnCompletion	= false,
		int					awardAmount			= 0)
	{
		List<List<int>> colorNumbers = new List<List<int>>();

		// Initialize the colorNumbers list
		for (int y = 0; y < yPixels; y++)
		{
			colorNumbers.Add(new List<int>());

			for (int x = 0; x < xPixels; x++)
			{
				colorNumbers[y].Add(0);
			}
		}

		for (int i = 0; i < palette.Count; i++)
		{
			PaletteItem	paletteItem		= palette[i];
			int			paletteIndex	= i;

			// Check if there is a blank color assigned
			if (blankItemIndex != -1)
			{
				if (paletteIndex == blankItemIndex)
				{
					// Set the index to -1 to indicate that this is a blank pixel
					paletteIndex = -1;
				}
				else if (paletteIndex > blankItemIndex)
				{
					// We wont include the blank palette items color in the list of colors so adjust the index
					paletteIndex -= 1;
				}
			}

			// For each of the palettes coordinates, add it to the colorNumbers list
			for (int j = 0; j < paletteItem.xCoords.Count; j++)
			{
				int x = paletteItem.xCoords[j];
				int y = paletteItem.yCoords[j];

				colorNumbers[y][x] = paletteIndex;
			}
		}

		string contents	= "";
		string version	= "1";

		// Add a version number to the file incase changes are made to the format after release so older file formats can still be parsed correctly
		contents += version + "\n";

		// Add an id of the texture to the file
		contents += id + "\n";

		// Add level lock info
		contents += string.Format("{0},{1}\n", isLevelLocked, isLevelLocked ? unlockAmount : 0);

		// Add complete award infp
		contents += string.Format("{0},{1}\n", awardOnCompletion, awardOnCompletion ? awardAmount : 0);

		// Add the number of x and y cells
		contents += string.Format("{0},{1}\n", xPixels, yPixels);

		// Add all the color numbers for each pixel in the picutre
		for (int y = 0; y < yPixels; y++)
		{
			for (int x = 0; x < xPixels; x++)
			{
				if (x != 0)
				{
					contents += ",";
				}

				contents += colorNumbers[y][x];
			}

			contents += "\n";
		}

		bool first = true;

		// Add all the colors
		for (int i = 0; i < palette.Count; i++)
		{
			// Skip the blank color palette item
			if (i == blankItemIndex)
			{
				continue;
			}

			PaletteItem paletteItem = palette[i];

			if (!first)
			{
				contents += "\n";
			}

			first = false;

			contents += string.Format("{0},{1},{2}", paletteItem.color.r, paletteItem.color.g, paletteItem.color.b);
		}

		// Check if the output folder exists and if not create it
		if (!System.IO.Directory.Exists(outputFolderPath))
		{
			System.IO.Directory.CreateDirectory(outputFolderPath);
		}

		// Save the picture file
		string filepath = string.Format("{0}/{1}.csv", outputFolderPath, string.IsNullOrEmpty(filename) ? id : filename);

		System.IO.File.WriteAllText(filepath, contents);

		return contents;
	}

	#if UNITY_EDITOR
	/// <summary>
	/// Checks if the given texture is read/write enabled in its settings
	/// </summary>
	public static bool CheckIsReadWriteEnabled(Texture2D texture)
	{
		if (texture == null)
		{
			return false;
		}

		string						assetPath	= UnityEditor.AssetDatabase.GetAssetPath(texture);
		UnityEditor.TextureImporter	importer	= UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;

		return importer.isReadable;
	}
	#endif

	#endregion

	#region Private Methods

	/// <summary>
	/// Adds the given color to the picture info at the given xCell, yCell
	/// </summary>
	private static void AddColorToPalette(Color color, int xPixel, int yPixel, List<PaletteItem> palette, float threshold)
	{
		float diff = 0f;

		PaletteItem	closestPaletteItem = GetClosestPaletteItem(color, palette, out diff);

		if (closestPaletteItem == null || diff > threshold)
		{
			palette.Add(new PaletteItem(color, xPixel, yPixel));
		}
		else
		{
			closestPaletteItem.Merge(color, xPixel, yPixel);
		}
	}

	/// <summary>
	/// Modifies the given texture using the palette
	/// </summary>
	private static void ApplyPaletteToTexture(Texture2D texture, List<PaletteItem> palette)
	{
		for (int i = 0; i < palette.Count; i++)
		{
			PaletteItem paletteItem = palette[i];

			for (int j = 0; j < paletteItem.xCoords.Count; j++)
			{
				texture.SetPixel(paletteItem.xCoords[j], paletteItem.yCoords[j], paletteItem.color);
			}
		}

		texture.Apply();
	}

	/// <summary>
	/// Converts an RGB color to the XYZ color space
	/// </summary>
	private static void RGBToXYZ(Color color, out float x, out float y, out float z)
	{
		float r = RGBToXYZHelper(color.r);
		float g = RGBToXYZHelper(color.g);
		float b = RGBToXYZHelper(color.b);

		x = r * 0.412453f + g * 0.357580f + b * 0.180423f;
		y = r * 0.212671f + g * 0.715160f + b * 0.072169f;
		z = r * 0.019334f + g * 0.119193f + b * 0.950227f;
	}

	private static float RGBToXYZHelper(float value)
	{
		return (value > 0.04045f) ?  Mathf.Pow(((value + 0.055f) / 1.055f), 2.4f) : value / 12.92f;
	}

	/// <summary>
	/// Converts an XYZ color to the LAB color space
	/// </summary>
	private static void XYZToLAB(float x, float y, float z, out float l, out float a, out float b)
	{
		x = XYZToLABHelper(x / 95.047f);
		y = XYZToLABHelper(y / 100f);
		z = XYZToLABHelper(z / 108.883f);

		l = (116 * y) - 16;
		a = 500 * (x - y);
		b = 200 * (y - z);
	}

	private static float XYZToLABHelper(float value)
	{
		return (value > 0.008856) ?  Mathf.Pow(value, 1f / 3f) : (7.787f * value) + (16f / 116f);
	}

	private static string PaletteIndexKey(int x, int y)
	{
		return string.Format("{0}_{1}", x, y);
	}

	#endregion
}
