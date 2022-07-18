using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames.PictureColoring
{
	[ExecuteInEditMode]
	public class ColorNumbersText : Text
	{
		#region Properties

		public LevelData	LevelData			{ get; set; }
		public float		LetterSpacing		{ get; set; }
		public float		MaxNumberSize		{ get; set; }
		public float		MinNumberSize		{ get; set; }
		public float		Padding				{ get; set; }
		public float		MinSizeToShow		{ get; set; }

		#endregion

		#region Protected Methods

		protected override void OnPopulateMesh(VertexHelper toFill)
		{
			base.OnPopulateMesh(toFill);

			List<UIVertex> stream = new List<UIVertex>();

			toFill.GetUIVertexStream(stream);

			if (stream.Count > 0 && LevelData != null)
			{
				LevelSaveData	levelSaveData	= LevelData.LevelSaveData;
				List<Region>	regions			= LevelData.LevelFileData.regions;
				List<UIVertex>	newStream		= new List<UIVertex>();

				for (int i = 0; i < regions.Count; i++)
				{
					Region region = regions[i];

					// Only add the regions number if it is not colored in yet
					if (region.colorIndex > -1 && !levelSaveData.coloredRegions.Contains(region.id))
					{
						AddNumber(region, stream, newStream);
					}
				}

				toFill.AddUIVertexTriangleStream(newStream);
			}
			else
			{
				toFill.AddUIVertexTriangleStream(stream);
			}
		}

	    #endregion

		#region Private Methods

		private void AddNumber(Region region, List<UIVertex> characterStream, List<UIVertex> stream)
		{
			// Get all the individual digits in the number
			List<int> digits = GetDigits(region.colorIndex + 1);

			// Get all the verticies for the numbers
			AddVerticies(region, digits, characterStream, stream);
		}

		/// <summary>
		/// Gets the digits.
		/// </summary>
		private List<int> GetDigits(int number)
		{
			// Get all the individual digits in the number
			List<int> digits = new List<int>();

			if (number == 0)
			{
				digits.Add(0);
			}
			else
			{
				while (number > 0)
				{
					int digit = number % 10;

					number = (number - digit) / 10;

					digits.Add(digit);
				}
			}

			return digits;
		}

		/// <summary>
		/// Adds the verticies.
		/// </summary>
		private void AddVerticies(Region region, List<int> digits, List<UIVertex> characterStream, List<UIVertex> stream)
		{
			float x = region.numberX;
			float y = region.numberY;

			float numSize = Mathf.Min(MaxNumberSize, Mathf.Max(MinNumberSize, region.numberSize));

			if (numSize < MinSizeToShow)
			{
				return;
			}

			float totalWidth	= 0;
			float maxHeight		= 0;

			for (int i = 0; i < digits.Count; i++)
			{
				int numberIndex = digits[i] * 6;

				UIVertex vert1 = characterStream[numberIndex];
				UIVertex vert2 = characterStream[numberIndex + 1];
				UIVertex vert3 = characterStream[numberIndex + 2];

				float texWidth 	= Mathf.Abs(vert1.position.x - vert2.position.x) + 1;
				float texHeight	= Mathf.Abs(vert1.position.y - vert3.position.y) + 1;

				totalWidth	+= texWidth + (i > 0 ? LetterSpacing : 0);
				maxHeight	= Mathf.Max(maxHeight, texHeight);
			}

			totalWidth	+= Padding * 2f;
			maxHeight	+= Padding * 2f;

			float scale = numSize / Mathf.Max(totalWidth, maxHeight);

			x -= (totalWidth * scale) / 2f;
			x += (Padding) * scale;

			for (int i = digits.Count - 1; i >= 0; i--)
			{
				int		numberIndex		= digits[i] * 6;
				bool	useBlankVert	= (digits.Count == 1 && digits[0] == 0);

				// Get the original vertices for the number
				UIVertex vert1 = useBlankVert ? UIVertex.simpleVert : characterStream[numberIndex];
				UIVertex vert2 = useBlankVert ? UIVertex.simpleVert : characterStream[numberIndex + 1];
				UIVertex vert3 = useBlankVert ? UIVertex.simpleVert : characterStream[numberIndex + 2];
				UIVertex vert4 = useBlankVert ? UIVertex.simpleVert : characterStream[numberIndex + 3];
				UIVertex vert5 = useBlankVert ? UIVertex.simpleVert : characterStream[numberIndex + 4];
				UIVertex vert6 = useBlankVert ? UIVertex.simpleVert : characterStream[numberIndex + 5];

				// Get the offset to the center of the character
				//float texWidth = Mathf.Abs(vert1.position.x - vert2.position.x) / 2f;
				//float textHeight = Mathf.Abs(vert1.position.y - vert3.position.y) / 2f;

				float texWidth	= Mathf.Abs(vert1.position.x - vert2.position.x) + 1;
				float texHeight	= Mathf.Abs(vert1.position.y - vert3.position.y) + 1;

				texWidth	= (texWidth * scale) / 2f;
				texHeight	= (texHeight * scale) / 2f;

				x += texWidth;

				// Position the character on the grid
				vert1.position = new Vector3(x - texWidth, y + texHeight, vert1.position.z);
				vert2.position = new Vector3(x + texWidth, y + texHeight, vert2.position.z);
				vert3.position = new Vector3(x + texWidth, y - texHeight, vert3.position.z);
				vert4.position = new Vector3(x + texWidth, y - texHeight, vert4.position.z);
				vert5.position = new Vector3(x - texWidth, y - texHeight, vert5.position.z);
				vert6.position = new Vector3(x - texWidth, y + texHeight, vert6.position.z);

				// Add to the new stream
				stream.Add(vert1);
				stream.Add(vert2);
				stream.Add(vert3);
				stream.Add(vert4);
				stream.Add(vert5);
				stream.Add(vert6);
				
				x += texWidth + LetterSpacing * scale;
			}

			//if (digits.Count > 1)
			//{
			//	int startStreamIndex = stream.Count - digits.Count * 6;

			//	for (int i = 0; i < digits.Count; i++)
			//	{
			//		float letterSpacing = i * LetterSpacing;

			//		for (int j = 0; j < 6; j++)
			//		{
			//			int			streamIndex	= startStreamIndex + i * 6 + j;
			//			UIVertex	vert		= stream[streamIndex];

			//			vert.position = new Vector3(vert.position.x - halfWidth + letterSpacing, vert.position.y);

			//			stream[streamIndex] = vert;

			//		}
			//	}
			//}
		}

		#endregion
	}
}
