using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BizzyBeeGames
{
	public static class ColorUtils
	{
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
		private static void RGBToLAB(Color color, out float l, out float a, out float b)
		{
			// First we need to convert the RGB color to the xyz color space then we convert that to the LAB space
			float x, y, z;

			RGBToXYZ(color, out x, out y, out z);

			// Now conver the x,y,z to l,a,b
			XYZToLAB(x, y, z, out l, out a, out b);
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
	}
}
