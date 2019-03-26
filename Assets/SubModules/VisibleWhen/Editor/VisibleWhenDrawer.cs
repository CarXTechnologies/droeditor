using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

[CustomPropertyDrawer(typeof(VisibleWhenAttribute))]
public class VisibleWhenDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		if(position.height == 0f)
		{
			position.width = 0f;
			position.x = int.MaxValue;
			label.text = "";
		}
		EditorGUI.BeginProperty(position, label, property);
		EditorGUI.PropertyField(position, property, label);
		EditorGUI.EndProperty();
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		var isVisible = ConditionalVisibility.IsVisible(fieldInfo, GetParent(property));
		return isVisible ? base.GetPropertyHeight(property, label) : 0f;
	}

	public object GetParent(SerializedProperty prop)
	{
		var path = prop.propertyPath.Replace(".Array.data[", "[");
		object obj = prop.serializedObject.targetObject;
		var elements = path.Split('.');
		foreach (var element in elements.Take(elements.Length - 1))
		{
			if (element.Contains("["))
			{
				var elementName = element.Substring(0, element.IndexOf("["));
				var index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
				obj = GetValue(obj, elementName, index);
			}
			else
			{
				obj = GetValue(obj, element);
			}
		}
		return obj;
	}

	public object GetValue(object source, string name)
	{
		if (source == null)
			return null;
		var type = source.GetType();
		var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
		if (f == null)
		{
			var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
			if (p == null)
				return null;
			return p.GetValue(source, null);
		}
		return f.GetValue(source);
	}

	public object GetValue(object source, string name, int index)
	{
		var enumerable = GetValue(source, name) as IEnumerable;
		var enm = enumerable.GetEnumerator();
		while (index-- >= 0)
			enm.MoveNext();
		return enm.Current;
	}
}

public static class ConditionalVisibility
{
	static bool AlwaysVisible(object target) { return true; }
	static Dictionary<MemberInfo, Func<object, bool>> _isVisibleCache = new Dictionary<MemberInfo, Func<object, bool>>();

	public static bool IsVisible(MemberInfo member, object target)
	{
		Func<object, bool> isVisible;
		if (!_isVisibleCache.TryGetValue(member, out isVisible))
		{
			var attr = member.GetCustomAttribute<VisibleWhenAttribute>();
			if (attr == null)
			{
				_isVisibleCache[member] = AlwaysVisible;
				return true;
			}

			var targetType = target.GetType();
			var conditionMemberNames = attr.ConditionMembers;
			var conditions = new List<Func<object, bool>>(conditionMemberNames.Length);

			for (int i = 0; i < conditionMemberNames.Length; i++)
			{
				string conditionMemberName;
				StringExtensions.ComparsionOperation operation;
				float value;
				conditionMemberNames[i].GetMathOperation(out conditionMemberName, out operation, out value);

				if (string.IsNullOrEmpty(conditionMemberName))
				{
					Debug.Log("Empty condition is used in VisibleWhen annotated on member: " + member.Name);
					continue;
				}

				bool negate = conditionMemberName[0] == '!';
				if (negate)
				{
					conditionMemberName = conditionMemberName.Remove(0, 1);
				}

				var conditionMember = targetType.GetMemberFromAll(conditionMemberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
				if (conditionMember == null)
				{
					Debug.Log("Member not found: " + conditionMemberName);
					return true;
				}

				Assert.IsTrue(attr.Operator == '|' || attr.Operator == '&',
					"Only AND ('&') and OR ('|') operators are supported");

				Func<object, bool> condition = null;
				if (negate)
				{
					condition = (x) => !CheckCondition(conditionMember, x, operation, value);
				}
				else
				{
					condition = (x) => CheckCondition(conditionMember, x, operation, value);
				}
				Assert.IsNotNull(condition, "Should have assigned a condition by now for member type: " + conditionMember.MemberType);
				conditions.Add(condition);
			}

			isVisible = (tgt) =>
			{
				bool ret = attr.Operator == '&';
				for (int i = 0; i < conditions.Count; i++)
				{
					var condition = conditions[i];
					if (attr.Operator == '&')
					{
						ret = ret && condition(tgt);
					}
					else
					{
						ret = ret || condition(tgt);
					}
				}
				return ret;
			};

			_isVisibleCache[member] = isVisible;
		}

		var result = isVisible(target);
		return result;
	}

	public static bool CheckCondition(MemberInfo member, object obj, StringExtensions.ComparsionOperation operation, float value)
	{
		object val = null;
		switch (member.MemberType)
		{
			case MemberTypes.Field:
				val = (member as FieldInfo).GetValue(obj);
				break;

			case MemberTypes.Property:
				val = (member as PropertyInfo).GetValue(obj);
				break;

			case MemberTypes.Method:
				val = (member as MethodInfo).Invoke(obj, null);
				break;
		}

		float refVal = 0f;
		if (val is bool)
		{
			refVal = (bool)val ? 1.0f : 0f;
		}
		else
		{
			refVal = Convert.ToSingle(val);
		}

		switch (operation)
		{
			case StringExtensions.ComparsionOperation.EQ:  return Mathf.Abs(value - refVal) < 0.0001f;
			case StringExtensions.ComparsionOperation.NEQ: return Mathf.Abs(value - refVal) > 0.0001f;
			case StringExtensions.ComparsionOperation.L:   return refVal < value;
			case StringExtensions.ComparsionOperation.LE:  return refVal <= value;
			case StringExtensions.ComparsionOperation.G:   return refVal > value;
			case StringExtensions.ComparsionOperation.GE:  return refVal >= value;
		}

		return false;
	}

	public static MemberInfo GetMemberFromAll(this Type type, string memberName, BindingFlags flags)
	{
		var peak = type.IsAssignableFrom(typeof(MonoBehaviour)) ? typeof(MonoBehaviour)
				 : type.IsAssignableFrom(typeof(ScriptableObject)) ? typeof(ScriptableObject)
				 : typeof(object);

		return GetMemberFromAll(type, memberName, peak, flags);
	}

	public static MemberInfo GetMemberFromAll(this Type type, string memberName, Type peak, BindingFlags flags)
	{
		var result = GetAllMembers(type, peak, flags).FirstOrDefault(x => x.Name == memberName);
		return result;
	}

	public static IEnumerable<MemberInfo> GetAllMembers(this Type type, Type peak, BindingFlags flags)
	{
		if (type == null || type == peak)
			return Enumerable.Empty<MemberInfo>();

		return type.GetMembers(flags).Concat(GetAllMembers(type.BaseType, peak, flags));
	}

	public static T GetCustomAttribute<T>(this MemberInfo member, bool inherit = false) where T : Attribute
	{
		var all = member.GetCustomAttributes(typeof(T), inherit);
		return (all == null || all.Length == 0) ? null : all[0] as T;
	}
}