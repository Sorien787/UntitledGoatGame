using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq.Expressions;

namespace UnityUtils
{
	public static class UnityUtils
	{

		public static string GetPropertyName<T>(Expression<Func<T>> propertyLambda)
		{
			if (!(propertyLambda.Body is MemberExpression me))
			{
				throw new ArgumentException("You must pass a lambda of the form: '() => Class.Property' or '() => object.Property'");
			}
			return me.Member.Name;
		}

		public static Quaternion SmoothDampQuat(Quaternion rot, Quaternion target, ref Quaternion deriv, float time)
		{
			if (Time.deltaTime < Mathf.Epsilon) return rot;
			// account for double-cover
			var Dot = Quaternion.Dot(rot, target);
			var Multi = Dot > 0f ? 1f : -1f;
			target.x *= Multi;
			target.y *= Multi;
			target.z *= Multi;
			target.w *= Multi;
			// smooth damp (nlerp approx)
			var Result = new Vector4(
				Mathf.SmoothDamp(rot.x, target.x, ref deriv.x, time),
				Mathf.SmoothDamp(rot.y, target.y, ref deriv.y, time),
				Mathf.SmoothDamp(rot.z, target.z, ref deriv.z, time),
				Mathf.SmoothDamp(rot.w, target.w, ref deriv.w, time)
			).normalized;

			// ensure deriv is tangent
			var derivError = Vector4.Project(new Vector4(deriv.x, deriv.y, deriv.z, deriv.w), Result);
			deriv.x -= derivError.x;
			deriv.y -= derivError.y;
			deriv.z -= derivError.z;
			deriv.w -= derivError.w;

			return new Quaternion(Result.x, Result.y, Result.z, Result.w);
		}

		public static string NumberToWords(int number)
		{
			if (number == 0)
				return "zero";

			if (number < 0)
				return "minus " + NumberToWords(Math.Abs(number));

			string words = "";

			if ((number / 1000000) > 0)
			{
				words += NumberToWords(number / 1000000) + " million ";
				number %= 1000000;
			}

			if ((number / 1000) > 0)
			{
				words += NumberToWords(number / 1000) + " thousand ";
				number %= 1000;
			}

			if ((number / 100) > 0)
			{
				words += NumberToWords(number / 100) + " hundred ";
				number %= 100;
			}

			if (number > 0)
			{
				if (words != "")
					words += "and ";

				var unitsMap = new[] { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
				var tensMap = new[] { "zero", "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };

				if (number < 20)
					words += unitsMap[number];
				else
				{
					words += tensMap[number / 10];
					if ((number % 10) > 0)
						words += "-" + unitsMap[number % 10];
				}
			}

			return words;
		}

		public static bool IsLayerInMask(in LayerMask mask, in int layer) 
		{
			return (mask == (mask | (1 << layer)));
		}

		public static string TurnTimeToString(in float time)
		{
			string extra = "";
			int seconds = Mathf.FloorToInt(time % 60);
			if (seconds < 10)
				extra = "0";
			int minutes = Mathf.FloorToInt(time / 60);
			return minutes.ToString() + ":" + extra + seconds.ToString();
		}
	}

	public class ListenerSet<T> : HashSet<T>
	{
		public void ForEachListener(Action<T> act)
		{
			foreach (T t in this)
			{
				act.Invoke(t);
			}
		}
	}

	[Serializable]
	public class ObservableVariable<T>
	{
		Action<T, T> OnValueChanged;

		public ObservableVariable(Action<T, T> OnChangedFunc)
		{
			OnValueChanged = OnChangedFunc;
		}

		[SerializeField] T _value;

		public T Value
		{
			get => _value;
			set
			{
				T previous = _value;
				_value = value;
				OnValueChanged?.Invoke(previous, _value);
			}
		}
	}

	[Serializable]
	public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
	{
		[SerializeField]
		private List<TKey> keys = new List<TKey>();

		[SerializeField]
		private List<TValue> values = new List<TValue>();

		// save the dictionary to lists
		public void OnBeforeSerialize()
		{
			keys.Clear();
			values.Clear();
			foreach (KeyValuePair<TKey, TValue> pair in this)
			{
				keys.Add(pair.Key);
				values.Add(pair.Value);
			}
		}

		// load dictionary from lists
		public void OnAfterDeserialize()
		{
			this.Clear();

			if (keys.Count != values.Count)
				throw new System.Exception(string.Format("there are {0} keys and {1} values after deserialization. Make sure that both key and value types are serializable.", keys.Count, values.Count));

			for (int i = 0; i < keys.Count; i++)
				this.Add(keys[i], values[i]);
		}
	}
}


