// Copyright (C) CarX Technologies, 2019, carx-tech.com
// Author:
//   Aleksandr Turkevich
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
using UnityEditor;
using UnityEditor.SceneManagement;

public static class UnityEditorHotkeys
{
	[MenuItem("EditorExtensions/Save _F1")]
	static void SaveImplementation()
	{
		AssetDatabase.Refresh();
		AssetDatabase.SaveAssets();
		EditorSceneManager.SaveOpenScenes();
	}

	[MenuItem("EditorExtensions/Pause _F11")]
	static void PauseImplementation()
	{
		EditorApplication.ExecuteMenuItem("Edit/Pause");
	}

	[MenuItem("EditorExtensions/Step _F8")]
	static void StepImplementation()
	{
		EditorApplication.ExecuteMenuItem("Edit/Step");
	}

	[MenuItem("EditorExtensions/Refresh and Play _F9")]
	static void RefreshAndPlayImplementation()
	{
		AssetDatabase.Refresh();
		EditorApplication.ExecuteMenuItem("Edit/Play");
	}

	[MenuItem("EditorExtensions/Refresh %_F9")]
	static void Refresh2Implementation()
	{
		AssetDatabase.Refresh();
	}
}