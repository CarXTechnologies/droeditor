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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using SplineLogic;
using TrackLayout.Data;
using TrackLayout.Builders;

using ArrayExtension;
using System.IO;

namespace TrackLayout.Editor
{
	[CustomEditor(typeof(TrackLayoutData))]
	public class TrackLayoutDataEditor : UnityEditor.Editor
	{
		private TrackLayoutData m_data;

		private int m_indexToDestroy = -1;
		private bool m_pointWantBePlaced = false;
		private Vector3 m_pointToPlace = Vector3.zero;

		private int m_selectedSpawnPointIndex;
		private int m_selectedLayoutPointIndex;

		private SerializedProperty m_triggerAdditionalWidthProperty;
		private SerializedProperty m_pathProperty;
		private SerializedProperty m_spawnPointsProperty;

		private ArrayDataEditor<SpawnPointData> m_spawnPointDataEditor;
		private ArrayDataEditor<CheckpointData> m_checkpointDataEditor;
		private ArrayDataEditor<ClipZoneData> m_clippingZoneDataEditor;
		private ArrayDataEditor<SectorData> m_sectorDataEditor;
		private ArrayDataEditor<RuleData> m_ruleDataEditor;

		public override void OnInspectorGUI()
		{
			EditorGUILayout.PropertyField(m_triggerAdditionalWidthProperty, true);
			EditorGUILayout.PropertyField(m_pathProperty, true);
			EditorGUILayout.PropertyField(m_spawnPointsProperty, true);

			EditorGUI.indentLevel++;
			m_spawnPointDataEditor.DrawGUI();
			EditorGUI.indentLevel--;

			EditorGUI.indentLevel++;
			EditorGUI.BeginChangeCheck();
			m_checkpointDataEditor.DrawGUI();
			if (EditorGUI.EndChangeCheck() && ValidateSectors())
			{
				return;
			}
			EditorGUI.indentLevel--;

			EditorGUI.indentLevel++;
			m_clippingZoneDataEditor.DrawGUI();
			EditorGUI.indentLevel--;

			EditorGUI.indentLevel++;
			EditorGUI.BeginChangeCheck();
			m_sectorDataEditor.DrawGUI();
			if (EditorGUI.EndChangeCheck() && ValidateSectors())
			{
				return;
			}
			EditorGUI.indentLevel--;

			EditorGUI.indentLevel++;
			m_ruleDataEditor.DrawGUI();
			EditorGUI.indentLevel--;

			if (GUILayout.Button("Project All Points to Closest Ground"))
			{
				Undo.RecordObject(m_data, "Project All Points to Closest Ground");
				for (int i = 0; i < m_data.path.leftPoints.Length; i++)
				{
					ProjectToClosestGround(ref m_data.path.leftPoints[i]);
				}
				for (int i = 0; i < m_data.path.centralPoints.Length; i++)
				{
					ProjectToClosestGround(ref m_data.path.centralPoints[i]);
				}
				for (int i = 0; i < m_data.path.rightPoints.Length; i++)
				{
					ProjectToClosestGround(ref m_data.path.rightPoints[i]);
				}
				EditorUtility.SetDirty(m_data);
			}

			serializedObject.ApplyModifiedProperties();
		}

		private bool ValidateSectors()
		{
			var old = m_data.sectors.Length;
			ValidateSectorsDistances(m_data.checkpoints, ref m_data.sectors);
			EditorUtility.SetDirty(m_data);

			if (old != m_data.sectors.Length)
			{
				m_sectorDataEditor = new ArrayDataEditor<SectorData>(target, () => m_data.sectors, arg => m_data.sectors = arg);
				serializedObject.ApplyModifiedProperties();
				Repaint();
				return true;
			}

			return false;
		}

		private void ValidateSectorsDistances(CheckpointData[] checkpointDatas, ref SectorData[] sectorDatas)
		{
			if (sectorDatas.Length != checkpointDatas.Length - 2)
			{
				var newCount = checkpointDatas.Length - 2 - sectorDatas.Length;
				sectorDatas = sectorDatas.Resize(checkpointDatas.Length - 2);
				for (int i = 0; i < newCount; ++i)
				{
					sectorDatas[checkpointDatas.Length - 3 - i].minSpeed = 30f;
					sectorDatas[checkpointDatas.Length - 3 - i].sideChangeFactor = 1f;
				}
			}

			for (int i = 0; i < checkpointDatas.Length - 2; i++)
			{
				sectorDatas[i].distance = checkpointDatas[i + 1].distance;
				sectorDatas[i].length = checkpointDatas[i + 2].distance - checkpointDatas[i + 1].distance;
			}
		}

		private void OnEnable()
		{
			m_data = target as TrackLayoutData;
			m_data.ValidateNullRefs();

			m_selectedSpawnPointIndex = -1;
			m_selectedLayoutPointIndex = -1;

			m_triggerAdditionalWidthProperty = serializedObject.FindProperty("triggerAdditionalWidth");
			m_pathProperty = serializedObject.FindProperty("path");
			m_spawnPointsProperty = serializedObject.FindProperty("spawnPoints");

			m_spawnPointDataEditor = new ArrayDataEditor<SpawnPointData>(target, () => m_data.spawnPoints, arg => m_data.spawnPoints = arg);
			m_checkpointDataEditor = new ArrayDataEditor<CheckpointData>(target, () => m_data.checkpoints, arg => m_data.checkpoints = arg);
			m_clippingZoneDataEditor = new ArrayDataEditor<ClipZoneData>(target, () => m_data.clipZones, arg => m_data.clipZones = arg);
			m_sectorDataEditor = new ArrayDataEditor<SectorData>(target, () => m_data.sectors, arg => m_data.sectors = arg);
			m_ruleDataEditor = new ArrayDataEditor<RuleData>(target, () => m_data.rules, arg => m_data.rules = arg);

			SceneView.onSceneGUIDelegate += OnSceneGUI;
			Undo.undoRedoPerformed += UndoRedoPerformed;
		}

		private void OnDisable()
		{
			Undo.undoRedoPerformed -= UndoRedoPerformed;
			SceneView.onSceneGUIDelegate -= OnSceneGUI;

			m_spawnPointDataEditor = null;
			m_checkpointDataEditor = null;
			m_clippingZoneDataEditor = null;
			m_sectorDataEditor = null;
			m_ruleDataEditor = null;
		}

		private void UndoRedoPerformed()
		{
			serializedObject.UpdateIfRequiredOrScript();
		}

		private void DrawClippingZones(ISpline centralSpline, ISpline leftSpline, ISpline rightSpline)
		{
			for (int j = 0; j < m_data.clipZones.Length; j++)
			{
				var data = m_data.clipZones[j];
				var points = BuilderUtils.SampleClipZone(centralSpline, leftSpline, rightSpline, data, 5.0f);

				switch (data.type)
				{
					case ClipZoneType.Default:
						Handles.color = Color.white;
						break;

					case ClipZoneType.Bad:
						Handles.color = Color.red;
						break;

					case ClipZoneType.PointsFactor:
						Handles.color = data.factor > 0f ? Color.green : Color.red;
						break;

					case ClipZoneType.SpeedControl:
						Handles.color = data.factor > 0f ? Color.cyan: Color.yellow;
						break;

					default:
						Handles.color = Color.magenta;
						break;
				}
				Handles.DrawPolyLine(points);
				Handles.DrawLine(points[0], points[points.Length - 1]);

				var avgPosition = points[0];
				for (int i = 1; i < points.Length; i++)
				{
					avgPosition += points[i];
				}
				avgPosition /= points.Length;

				if (IsWorldPositionInCameraView(avgPosition, 1000.0f))
				{
					var style = new GUIStyle
					{
						alignment = TextAnchor.MiddleCenter,
						normal =
						{
							textColor = Color.white
						}
					};
					Handles.Label(avgPosition, string.Format("ClipZone #{0}", j), style);
				}
			}
		}

		private static List<Vector3> m_samples = new List<Vector3>();

		private void ProccessEditEvents()
		{
			int controlID = GUIUtility.GetControlID(FocusType.Passive);
			if (Event.current.type == EventType.Layout)
			{
				HandleUtility.AddDefaultControl(controlID);
			}

			if ((Event.current.modifiers == EventModifiers.Shift) || (Event.current.modifiers == EventModifiers.Control))
			{
				SceneView.RepaintAll();
			}

			if (Event.current.type == EventType.MouseDown)
			{
				if ((m_indexToDestroy != -1) && (Event.current.button == 0))
				{
					Event.current.Use();
					Undo.RecordObject(m_data, "Path point removed");
					m_data.path.leftPoints = m_data.path.leftPoints.RemoveAt(m_indexToDestroy);
					m_data.path.centralPoints = m_data.path.centralPoints.RemoveAt(m_indexToDestroy);
					m_data.path.rightPoints = m_data.path.rightPoints.RemoveAt(m_indexToDestroy);
					EditorUtility.SetDirty(m_data);
					m_indexToDestroy = -1;
				}

				if (m_pointWantBePlaced && (Event.current.button == 0))
				{
					Event.current.Use();
					m_pointWantBePlaced = false;
					Undo.RecordObject(m_data, "Path point added");
					var centralSpline = m_data.path.BuildSpline(PathLaneType.Central);
					var leftSpline = m_data.path.BuildSpline(PathLaneType.Left);
					var rightSpline = m_data.path.BuildSpline(PathLaneType.Right);

					float leftDist, rightDist, tmp, best, best2;
					Vector3 tmpVec3, leftPoint, rightPoint;
					leftSpline.ProjectPoint(m_pointToPlace, out leftDist, out tmp);
					leftSpline.SamplePoint(leftDist, out leftPoint);
					rightSpline.ProjectPoint(m_pointToPlace, out rightDist, out tmp);
					rightSpline.SamplePoint(rightDist, out rightPoint);

					int index; var spl = (PathSpline)centralSpline;
					spl.InaccurateFindNearest(m_pointToPlace, out index, out best, out tmpVec3, out best2);

					m_data.path.leftPoints = m_data.path.leftPoints.Insert(index + 1, leftPoint);
					m_data.path.centralPoints = m_data.path.centralPoints.Insert(index + 1, m_pointToPlace);
					m_data.path.rightPoints = m_data.path.rightPoints.Insert(index + 1, rightPoint);
					EditorUtility.SetDirty(m_data);
				}
			}
		}

		private void DrawSpline(ISpline spline, float step, Color color, bool drawIndices = false, bool allowEdit = false)
		{
			m_samples.Clear();
			((PathSpline)spline).Sample(step, ref m_samples);
			var array = m_samples.ToArray();
			Handles.color = color;
			Handles.DrawPolyLine(array);
			if (drawIndices)
			{
				for (int i = 0; i < spline.pointCount; i++)
				{
					var positionWS = spline.GetWorldPoint(i);
					if (IsWorldPositionInCameraView(positionWS, 100.0f))
					{
						Handles.Label(positionWS, " " + i);
					}
				}
			}

			if (allowEdit)
			{
				m_indexToDestroy = -1;
				if (Event.current.modifiers == EventModifiers.Control)
				{
					Handles.color = Color.red;
					var point = HandleUtility.ClosestPointToPolyLine(array);
					var spl = (PathSpline)spline;
					var index = 0;
					float best, best2;
					Vector3 tmp;
					spl.InaccurateFindNearest(point, out index, out best, out tmp, out best2);
					if(best > 0.5f && index < spl.pointCount - 1)
					{
						++index;
					}
					if (index >= 0 && index < spl.pointCount)
					{
						m_indexToDestroy = (point - spl.GetWorldPoint(index)).sqrMagnitude < 10f ? index : -1;
						Handles.CubeHandleCap(0, spl.GetWorldPoint(index), Quaternion.identity, 1f, EventType.Repaint);
					}
				}

				m_pointWantBePlaced = false;
				if (Event.current.modifiers == EventModifiers.Shift)
				{
					Handles.color = Color.green;
					var point = HandleUtility.ClosestPointToPolyLine(array);
					Handles.CubeHandleCap(0, point, Quaternion.identity, 1f, EventType.Repaint);
					m_pointWantBePlaced = true;
					m_pointToPlace = point;
				}
			}
		}

		private void DrawSpawnPoints()
		{
			if (m_data.spawnPoints != null)
			{
				for (int i = 0; i < m_data.spawnPoints.Length; i++)
				{
					var spawnPoint = m_data.spawnPoints[i];
					Handles.Label(spawnPoint.position, string.Format("\n\nSpawnPoint#{0}", i));
					Handles.color = Color.white;
					var handleSize = 2.0f;
					if (Handles.Button(spawnPoint.position, spawnPoint.rotation, handleSize, handleSize * 0.5f, Handles.ConeHandleCap))
					{
						m_selectedSpawnPointIndex = m_selectedSpawnPointIndex == i ? -1 : i;
					}
					if (m_selectedSpawnPointIndex == i)
					{
						EditorGUI.BeginChangeCheck();
						spawnPoint.position = Handles.DoPositionHandle(spawnPoint.position, spawnPoint.rotation);
						spawnPoint.rotation = Handles.DoRotationHandle(spawnPoint.rotation, spawnPoint.position);

						if (EditorGUI.EndChangeCheck())
						{
							Undo.RecordObject(m_data, "Spawn Point Changed");
							m_data.spawnPoints[i] = spawnPoint;
							EditorUtility.SetDirty(m_data);
						}
					}
				}
			}
		}

		private void DrawCheckpoints(ISpline centralSpline, ISpline leftSpline, ISpline rightSpline)
		{
			for (int i = 0; i < m_data.checkpoints.Length; i++)
			{
				Vector3 cp;
				centralSpline.SamplePoint(m_data.checkpoints[i].distance, out cp);
				if (IsWorldPositionInCameraView(cp, 1000.0f))
				{
					Vector3 lp, ld, rp, rd;
					leftSpline.ProjectPoint(cp, out lp, out ld);
					rightSpline.ProjectPoint(cp, out rp, out rd);
					var style = new GUIStyle();
					style.normal.textColor = Color.green;
					Handles.Label(cp, string.Format("{0}#{1}", m_data.checkpoints[i].type, i), style);
					var xlp = lp;
					var xrp = rp;
					ExtendPoints(cp, ref xlp, ref xrp, m_data.triggerAdditionalWidth);
					Handles.color = Color.yellow;
					Handles.DrawLine(xlp, lp);
					Handles.DrawLine(xrp, rp);

					Handles.color = Color.green;
					Handles.DrawLine(lp, rp);
				}
			}
		}

		private void DrawRules(ISpline centralSpline, ISpline leftSpline, ISpline rightSpline)
		{
			for (int i = 0; i < m_data.rules.Length; i++)
			{
				Vector3 cp;
				centralSpline.SamplePoint(m_data.rules[i].distance, out cp);
				if (IsWorldPositionInCameraView(cp, 1000.0f))
				{
					Vector3 lp, ld, rp, rd;
					leftSpline.ProjectPoint(cp, out lp, out ld);
					rightSpline.ProjectPoint(cp, out rp, out rd);
					var style = new GUIStyle();
					style.normal.textColor = Color.yellow;
					Handles.Label(cp, string.Format("\nRule #{0}", i), style);
					var xlp = lp;
					var xrp = rp;
					ExtendPoints(cp, ref xlp, ref xrp, m_data.triggerAdditionalWidth);
					Handles.color = Color.yellow;
					Handles.DrawLine(xlp, lp);
					Handles.DrawLine(xrp, rp);

					Handles.color = Color.yellow;
					Handles.DrawLine(lp, rp);
				}
			}
		}

		private void ExtendPoints(Vector3 cp, ref Vector3 lp, ref Vector3 rp, float extender)
		{
			var way_lp = (lp - cp).normalized;
			var way_rp = (rp - cp).normalized;
			lp += way_lp * extender;
			rp += way_rp * extender;
		}

		private void DrawSectors(ISpline centralSpline, ISpline leftSpline, ISpline rightSpline)
		{
			for (int i = 0; i < m_data.sectors.Length; i++)
			{
				Vector3 cp;
				centralSpline.SamplePoint(m_data.sectors[i].distance, out cp);
				if (IsWorldPositionInCameraView(cp, 1000.0f))
				{
					Vector3 lp, ld, rp, rd;
					leftSpline.ProjectPoint(cp, out lp, out ld);
					rightSpline.ProjectPoint(cp, out rp, out rd);
					var style = new GUIStyle();
					style.normal.textColor = Color.cyan;
					Handles.Label(cp, string.Format("\n\nSector #{0} Beginning", i), style);
					Handles.color = Color.cyan;
					Handles.DrawLine(lp, rp);
				}
			}
			for (int i = 0; i < m_data.sectors.Length; i++)
			{
				Vector3 cp;
				centralSpline.SamplePoint(m_data.sectors[i].distance + m_data.sectors[i].length, out cp);
				if (IsWorldPositionInCameraView(cp, 1000.0f))
				{
					Vector3 lp, ld, rp, rd;
					leftSpline.ProjectPoint(cp, out lp, out ld);
					rightSpline.ProjectPoint(cp, out rp, out rd);
					var style = new GUIStyle();
					style.normal.textColor = Color.cyan;
					Handles.Label(cp, string.Format("\n\n\nSector #{0} Ending", i), style);
					Handles.color = Color.cyan;
					Handles.DrawLine(lp, rp);
				}
			}
		}

		private void OnSceneGUI(SceneView sceneView)
		{
			ProccessEditEvents();

			for (int i = 0; i < m_data.path.centralPoints.Length; i++)
			{
				var cp = m_data.path.centralPoints[i];
				var lp = m_data.path.leftPoints[i];
				var rp = m_data.path.rightPoints[i];

				Handles.color = new Color(1.0f, 1.0f, 1.0f, 0.25f);
				Handles.DrawLine(lp, cp);
				Handles.DrawLine(cp, rp);

				Handles.color = Color.white;
				DrawPointHandle(i, ref m_data.path.centralPoints, ref m_selectedLayoutPointIndex, m_data);
				DrawPointHandle(i, ref m_data.path.leftPoints, ref m_selectedLayoutPointIndex, m_data);
				DrawPointHandle(i, ref m_data.path.rightPoints, ref m_selectedLayoutPointIndex, m_data);
			}

			DrawSpawnPoints();

			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			var centralSpline = m_data.path.BuildSpline(PathLaneType.Central);
			var leftSpline = m_data.path.BuildSpline(PathLaneType.Left);
			var rightSpline = m_data.path.BuildSpline(PathLaneType.Right);

			DrawSpline(centralSpline, 3, new Color(1.0f, 1.0f, 1.0f, 0.5f), true, true);
			DrawSpline(leftSpline, 3, Color.white);
			DrawSpline(rightSpline, 3, Color.white);

			DrawClippingZones(centralSpline, leftSpline, rightSpline);
			DrawCheckpoints(centralSpline, leftSpline, rightSpline);
			DrawRules(centralSpline, leftSpline, rightSpline);
			DrawSectors(centralSpline, leftSpline, rightSpline);
		}

		private static void DrawPointHandle(int index, ref Vector3[] points, ref int selectedIndex, Object data, bool visibilityTest = true, float handleSize = 0.03f)
		{
			var point = points[index];
			Handles.color = Color.white;
			if (!visibilityTest || IsWorldPositionInCameraView(point))
			{
				handleSize *= HandleUtility.GetHandleSize(point);
				if (Handles.Button(point, Quaternion.identity, handleSize, handleSize * 0.5f, Handles.DotHandleCap))
				{
					selectedIndex = selectedIndex == index ? -1 : index;

				}
			}
			if (selectedIndex == index)
			{
				EditorGUI.BeginChangeCheck();
				point = Handles.DoPositionHandle(point, Quaternion.identity);
				if (Event.current.modifiers != EventModifiers.Alt)
				{
					ProjectToGround(ref point);
				}

				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(data, "Move Point");
					points[index] = point;
					EditorUtility.SetDirty(data);
				}
			}
		}

		private static bool IsWorldPositionInCameraView(Vector3 worldPos, float viewDistance = 250.0f)
		{
			var viewportPos = Camera.current.WorldToViewportPoint(worldPos);
			return viewportPos.x <= 1.0f && viewportPos.x >= 0.0f && viewportPos.y <= 1.0f && viewportPos.y >= 0.0f && viewportPos.z >= 0.0f && viewportPos.z <= viewDistance;
		}

		private static void ProjectToGround(ref Vector3 point)
		{
			var queriesHitBackfaces = Physics.queriesHitBackfaces;
			Physics.queriesHitBackfaces = true;
			RaycastHit hit;
			var diffDown = float.MaxValue;
			var diffUp = float.MaxValue;
			if (Physics.Raycast(point + Vector3.up * 0.01f, Vector3.down, out hit, 200f))
			{
				diffDown = hit.point.y - point.y;
			}
			if (Physics.Raycast(point - Vector3.up * 0.01f, Vector3.up, out hit, 200f))
			{
				diffUp = hit.point.y - point.y;
			}

			var diff = Mathf.Abs(diffDown) < Mathf.Abs(diffUp) ? diffDown : diffUp;
			if (diff <= 2f)
			{
				point.y += diff;
			}
			Physics.queriesHitBackfaces = queriesHitBackfaces;
		}

		private static void ProjectToClosestGround(ref Vector3 point)
		{
			var queriesHitBackfaces = Physics.queriesHitBackfaces;
			Physics.queriesHitBackfaces = true;
			RaycastHit hit;
			var diffDown = float.MaxValue;
			var diffUp = float.MaxValue;
			if (Physics.Raycast(point + Vector3.up * 0.01f, Vector3.down, out hit, 200f))
			{
				diffDown = hit.point.y - point.y;
			}
			if (Physics.Raycast(point - Vector3.up * 0.01f, Vector3.up, out hit, 200f))
			{
				diffUp = hit.point.y - point.y;
			}

			var diff = Mathf.Abs(diffDown) < Mathf.Abs(diffUp) ? diffDown : diffUp;
			if (diff <= 2f)
			{
				point.y += diff;
			}
			Physics.queriesHitBackfaces = queriesHitBackfaces;
		}

		[MenuItem("Track Layout/Spawn TrackLayoutData")]
		public static void Spawn()
		{
			var outputPath = "Assets" + Path.DirectorySeparatorChar + "Resources" + Path.DirectorySeparatorChar;
			if (!Directory.Exists(outputPath))
			{
				Directory.CreateDirectory(outputPath);
			}
			var trackData = ScriptableObject.CreateInstance<TrackLayoutData>();
			trackData.name = "Default";

			AssetDatabase.CreateAsset(trackData, outputPath + trackData.name + ".asset");
		}
	}
}