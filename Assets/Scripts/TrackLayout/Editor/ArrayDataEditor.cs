// Copyright (C) CarX Technologies, 2019, carx-tech.com
// Author:
//   Sviatoslav Gampel
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TrackLayout.Editor
{
	public class ArrayDataEditor<T>
	{
		private readonly Object m_target;
		private readonly string m_header;
		private readonly string m_description;
		private readonly FieldInfo[] m_fields;
		private readonly Func<T[]> m_getter;
		private readonly Action<T[]> m_setter;
		private readonly ReorderableList m_list;

		private GUIStyle m_descriptionStyle;
		private GUIStyle m_indexStyle;

		public ArrayDataEditor(Object target, Func<T[]> getter, Action<T[]> setter)
		{
			m_target = target;
			m_getter = getter;
			m_setter = setter;
			m_list = new ReorderableList(m_getter(), typeof(T), true, true, true, true);
			m_list.drawHeaderCallback = DrawHeaderCallback;
			m_list.drawElementCallback = DrawElementCallback;
			m_list.onAddCallback = OnAddCallback;
			m_list.onRemoveCallback = OnRemoveCallback;
			m_list.headerHeight = EditorGUIUtility.singleLineHeight * 2;

			var type = typeof(T);
			m_header = type.Name + "(s)";
			m_fields = type.GetFields();
			m_description = string.Join(" | ", m_fields.Select(info => info.Name).ToArray());
		}

		private void DrawHeaderCallback(Rect rect)
		{
			if (m_descriptionStyle == null)
			{
				m_descriptionStyle = new GUIStyle("label");
				m_descriptionStyle.alignment = TextAnchor.UpperCenter;
			}
			GUI.Label(rect, m_header);
			rect.y += EditorGUIUtility.singleLineHeight;

			GUI.Label(rect, m_description, m_descriptionStyle);
		}

		private T DrawItem(T item, Rect rect, int index)
		{
			rect.y += 2;

			if (m_indexStyle == null)
			{
				m_indexStyle = new GUIStyle("label");
				m_indexStyle.normal.textColor = Color.gray;
			}

			var labelRect = rect;
			labelRect.x -= 2;
			labelRect.width = 12;
			GUI.Label(labelRect, index.ToString(), m_indexStyle);

			rect.height = EditorGUIUtility.singleLineHeight;
			var widthRemainder = rect.width;
			var width = widthRemainder / m_fields.Length;

			var obj = (object)item;
			for (int i = 0; i < m_fields.Length; i++)
			{
				var field = m_fields[i];
				var value = field.GetValue(obj);

				rect.width = width;

				if (field.FieldType == typeof(int))
				{
					value = EditorGUI.IntField(rect, "", (int)value);
				}
				else if (field.FieldType == typeof(float))
				{
					value = EditorGUI.FloatField(rect, "", (float)value);
				}
				else if (field.FieldType == typeof(bool))
				{
					value = EditorGUI.Toggle(rect, "", (bool)value);
				}
				else if (field.FieldType.BaseType == typeof(Enum))
				{
					value = EditorGUI.EnumPopup(rect, (Enum)value);
				}
				else if (field.FieldType == typeof(Color))
				{
					value = EditorGUI.ColorField(rect, (Color)value);
				}
				else if (field.FieldType.BaseType == typeof(Object))
				{
					value = EditorGUI.ObjectField(rect, (Object)value, field.FieldType, false);
				}
				else if (field.FieldType == typeof(string))
				{
					value = EditorGUI.TextField(rect, (string)value);
				}
				else if (field.FieldType == typeof(Vector3))
				{
					value = EditorGUI.Vector3Field(rect, "", (Vector3)value);
				}
				else if (field.FieldType == typeof(Vector4))
				{
					value = EditorGUI.Vector4Field(rect, "", (Vector4)value);
				}
				else if (field.FieldType == typeof(Quaternion))
				{
					value = Quaternion.Euler(EditorGUI.Vector3Field(rect, "", ((Quaternion)value).eulerAngles));
				}
				field.SetValue(obj, value);
				rect.x += rect.width;
				widthRemainder -= rect.width;
			}
			return (T)obj;
		}

		private void OnRemoveCallback(ReorderableList list)
		{
			Undo.RecordObject(m_target, typeof(T).Name + " Removed");
			var itemList = m_getter().ToList();
			itemList.RemoveAt(list.index);
			m_setter(itemList.ToArray());
			list.list = m_getter();
		}

		private void OnAddCallback(ReorderableList list)
		{
			Undo.RecordObject(m_target, typeof(T).Name + " Removed");
			if (list.count == 0)
			{
				var item = default(T);
				var itemList = m_getter().ToList();
				itemList.Insert(0, item);
				m_setter(itemList.ToArray());
				list.list = m_getter();
			}
			else
			{
				var selectedIndex = list.index > 0 ? list.index : list.count - 1;
				var itemList = m_getter().ToList();
				var item = itemList[selectedIndex];
				itemList.Insert(selectedIndex + 1, item);
				m_setter(itemList.ToArray());
				list.list = m_getter();
			}
		}

		private void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
		{
			EditorGUI.BeginChangeCheck();
			var item = DrawItem(m_getter()[index], rect, index);
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(m_target, typeof(T).Name + " Changed");
				m_getter()[index] = item;
				EditorUtility.SetDirty(m_target);
			}
		}

		public void DrawGUI()
		{
			m_list.DoLayoutList();
		}
	}
}