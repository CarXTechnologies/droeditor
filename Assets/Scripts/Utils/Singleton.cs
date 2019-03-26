// Copyright (C) CarX Technologies, 2019, carx-tech.com
// Author:
//   Mikhail Bazhin
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
using UnityEngine;

/// <summary>
/// Класс реализует синглтон в рамках Unity.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IKeepAliveMonoBehaviourSingleton { };
public interface IAlwaysAccessibleOnQuit { };

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
	public static bool isQuitting = false;
	public static bool isInitialized = false;

	public static T instance
	{
		get
		{
			if (isQuitting)
			{
				if (m_instance is IAlwaysAccessibleOnQuit)
				{
					return m_instance;
				}
				else
				{
					return null;
				}
			}

			return m_instance ?? CreateInstance();
		}
	}

	private static T m_instance;

	protected virtual void Awake()
	{
		if (m_instance == null || m_instance == this)
		{
			m_instance = this as T;

			if (this is IKeepAliveMonoBehaviourSingleton)
			{
				DontDestroyOnLoad(gameObject);
			}

			try
			{
				if (!isInitialized)
				{
					Initialization();
				}
			}
			catch (Exception E)
			{
				Debug.LogException(E);
			}
			finally
			{
				isInitialized = true;
			}
		}
		else
		{
			Destroy(gameObject);
		}
	}

	private void OnDestroy()
	{
		try
		{
			if (this == m_instance)
			{
				try
				{
					Finalization();
				}
				catch (Exception E)
				{
					Debug.LogException(E);
				}
				finally
				{
					isInitialized = false;
				}
			}
		}
		catch (Exception E)
		{
			Debug.LogException(E);
		}

		if (this == m_instance)
		{
			m_instance = null;
		}
	}

	private void OnApplicationQuit()
	{
		isQuitting = true;
	}

	protected virtual void Initialization() { }
	protected virtual void Finalization() { }

	public static T CreateInstance()
	{
		if (m_instance == null)
		{
			try
			{
				m_instance = FindObjectOfType<T>() as T;
			}
			catch (Exception E)
			{
				Debug.Log(E);
			}
			finally
			{
				if (Application.isPlaying)
				{
					if (m_instance == null)
					{
						Debug.Log("Singleton -> Creating: " + typeof(T).Name);
					}
					else
					{
						// кейс, когда мы запрашиваем GameObject, который реально уже существует на сцене, но для которого еще не был вызван Awake:
						// нам придется его удалить, и создать новый с насильным запуском Awake, чтобы все процедуры отработали штатно прямо сейчас
						m_instance.gameObject.SetActive(false);
						Destroy(m_instance.gameObject);
						m_instance = null;

						Debug.LogWarning("Singleton -> Force creating: " + typeof(T).Name);
					}

					var go = new GameObject(typeof(T).ToString());

					// хукаемся так, чтобы m_instance заполнился ДО вызова Awake,
					// тем самым делая неважным порядок вызова Awake в наследовании
					go.SetActive(false);
					m_instance = go.AddComponent<T>();
					go.SetActive(true);
				}
			}
		}
		return m_instance;
	}

	public static bool hasInstance { get { return (!isQuitting || (m_instance is IAlwaysAccessibleOnQuit)) && m_instance != null; } }
}