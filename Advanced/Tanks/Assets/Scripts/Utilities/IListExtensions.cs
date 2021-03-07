using System;
using System.Collections.Generic;
using UnityDebug = UnityEngine.Debug;
using UnityRandom = UnityEngine.Random;

namespace Tanks.Extensions
{
	/// <summary>
	/// Extension methods for ILists
	/// </summary>
	public static class IListExtensions
	{
		/// <summary>
		/// Select an item from a list using a weighted selection.
		/// </summary>
		/// <remarks>This is an O(n) operation, not constant-time like equal random selection.</remarks>
		/// <param name="elements">An <see cref="System.Collections.Generic.IList{T}"/> of elements to choose from</param>
		/// <param name="weightSum">The sum of all the weights of the elements</param>
		/// <param name="getElementWeight">A delegate to retrieve the weight of a specific element</param>
		/// <returns>An element randomly selected from <paramref name="elements"/></returns>
		public static T WeightedSelection<T>(this IList<T> elements, int weightSum, Func<T, int> getElementWeight)
		{
			int index = elements.WeightedSelectionIndex(weightSum, getElementWeight);
			return elements[index];
		}


		/// <summary>
		/// Select the index of an item from a list using a weighted selection.
		/// </summary>
		/// <remarks>This is an O(n) operation, not constant-time like equal random selection.</remarks>
		/// <param name="elements">An <see cref="System.Collections.Generic.IList{T}"/> of elements to choose from</param>
		/// <param name="weightSum">The sum of all the weights of the elements</param>
		/// <param name="getElementWeight">A delegate to retrieve the weight of a specific element</param>
		/// <returns>The index of an element randomly selected from <paramref name="elements"/></returns>
		public static int WeightedSelectionIndex<T>(this IList<T> elements, int weightSum, Func<T, int> getElementWeight)
		{
			if (weightSum <= 0)
			{
				throw new ArgumentException("WeightSum should be a positive value", "weightSum");
			}

			int selectionIndex = 0;
			int selectionWeightIndex = UnityRandom.Range(0, weightSum);
			int elementCount = elements.Count;

			if (elementCount == 0)
			{
				throw new InvalidOperationException("Cannot perform selection on an empty collection");
			}

			int itemWeight = getElementWeight(elements[selectionIndex]);
			while (selectionWeightIndex >= itemWeight)
			{
				selectionWeightIndex -= itemWeight;
				selectionIndex++;

				if (selectionIndex >= elementCount)
				{
					throw new ArgumentException("Weighted selection exceeded indexable range. Is your weightSum correct?", "weightSum");
				}

				itemWeight = getElementWeight(elements[selectionIndex]);
			}

			return selectionIndex;
		}


		/// <summary>
		/// Shuffle this List into a new array copy
		/// </summary>
		public static T[] Shuffle<T>(this IList<T> original)
		{
			int numItems = original.Count;
			T[] result = new T[numItems];

			for (int i = 0; i < numItems; ++i)
			{
				int j = UnityRandom.Range(0, i + 1);

				if (j != i)
				{
					result[i] = result[j];
				}

				result[j] = original[i];
			}

			return result;
		}
	}
}