using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ArrayExtension
{
    public static class ImmutableExtensions
    {
		public static T[] Insert<T>(this T[] lhs, int index, T value)
		{
			if (lhs.IsEmpty())
			{
				return new T[] { value };
			}

			if (index < 0)
			{
				index = 0;
			}

			if(index > lhs.Length)
			{
				index = lhs.Length;
			}

			var result = new T[lhs.Length + 1];
			if (index > 0)
			{
				Array.Copy(lhs, 0, result, 0, index);
			}
			result[index] = value;
			if(index < lhs.Length)
			{
				Array.Copy(lhs, index, result, index + 1, lhs.Length - index);
			}
			return result;
		}

        public static bool IsEmpty<T>(this T[] array)
        {
            return (array == null) || (array.Length == 0);
        }

        public static bool IsNonEmpty<T>(this T[] array)
        {
            return (array != null) && (array.Length != 0);
        }

        public static T[] RemoveAt<T>(this T[] array, int index)
        {
            if (array.IsEmpty())
            {
                return new T[0];
            }

			if ((index < 0) || (index >= array.Length))
			{
				return array.Clone() as T[];
			}

			var result = new T[array.Length - 1];

            if (index > 0)
            {
                Array.Copy(array, 0, result, 0, index);
            }

            if (index < array.Length - 1)
            {
                Array.Copy(array, index + 1, result, index, array.Length - index - 1);
            }

            return result;
        }

        public static T[] Resize<T>(this T[] array, int newSize)
        {
            if ((!array.IsEmpty()) && (newSize == array.Length))
            {
                return array.Clone() as T[];
            }

            if (newSize <= 0)
            {
                return new T[0];
            }

            var result = new T[newSize];
            if (array.IsNonEmpty())
            {
                Array.Copy(array, 0, result, 0, Math.Min(newSize, array.Length));
            }
            return result;
        }
    }
}