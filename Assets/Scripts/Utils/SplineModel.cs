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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace SplineLogic
{

	/// <summary>
	/// Класс для формирования сплайна из набора вершин и параметров, образующих кривую.
	/// </summary>
	public class SplineModel : ISpline
	{
		private Vector3[] m_worldPoints = new Vector3[0];
		private Vector3[] m_worldVelocities = new Vector3[0];
		private float[] m_distances = new float[0];
		private int m_cachedInstanceID = -1;
		private bool m_closed;

		public bool closed
		{
			get { return m_closed; }
			set { m_closed = value; }
		}

		public SplineModel(int cachedInstanceID, Vector3[] points, Vector3[] velocities, float[] distances, bool closed)
		{
			ResetData(cachedInstanceID, points, velocities, distances, closed);
		}

		public void ResetData(int cachedInstanceID, Vector3[] points, Vector3[] velocities, float[] distances, bool closed)
		{
			Assert.IsTrue(points != null && velocities != null && distances != null);
			Assert.IsTrue(points.Length == velocities.Length && points.Length == distances.Length);

			m_cachedInstanceID = cachedInstanceID;

			m_worldPoints = new Vector3[points.Length];
			points.CopyTo(m_worldPoints, 0);

			m_worldVelocities = new Vector3[velocities.Length];
			velocities.CopyTo(m_worldVelocities, 0);

			m_distances = new float[distances.Length];
			distances.CopyTo(m_distances, 0);

			this.closed = closed;
		}


		#region ISpline

		public float length
		{
			get { return pointCount > 1 ? m_distances[pointCount - 1] : 0; }
		}

		public int pointCount
		{
			get { return m_worldPoints.Length; }
		}

		public void SetWorldPoints(Vector3[] points)
		{
			m_worldPoints = new Vector3[points.Length];
			points.CopyTo(m_worldPoints, 0);
		}

		public Vector3 GetWorldPoint(int index)
		{
			Assert.IsTrue(index >= 0 && index < m_worldPoints.Length);
			return m_worldPoints[index];
		}

		public void GetWorldPoints(out Vector3[] points)
		{
			points = new Vector3[m_worldPoints.Length];
			m_worldPoints.CopyTo(points, 0);
		}

		public float GetDistanceAt(int index)
		{
			Assert.IsTrue(index >= 0 && index < m_distances.Length);
			return m_distances[index];
		}

		public Vector3 GetWorldVelocity(int index)
		{
			Assert.IsTrue(index >= 0 && index < m_worldVelocities.Length);
			return m_worldVelocities[index];
		}

		public Projection ProjectPoint(Vector3 pnt, out Vector3 pos, out Vector3 dir)
		{
			int segmentIndex = -1;
			float T;
			return ProjectPointImpl(pnt, ref segmentIndex, out T, out pos, out dir);
		}

		public Projection ProjectPoint(Vector3 pnt, ref int segmentIndex, out Vector3 pos, out Vector3 dir)
		{
			float T;
			return ProjectPointImpl(pnt, ref segmentIndex, out T, out pos, out dir);
		}

		public Projection ProjectPoint(Vector3 pnt, out float along, out float across)
		{
			int segmentIndex = -1;
			Vector3 pos, dir;
			return ProjectPoint(pnt, ref segmentIndex, out pos, out dir, out along, out across);
		}

		public Projection ProjectPoint(Vector3 pnt, ref int segmentIndex, out float along, out float across)
		{
			Vector3 pos, dir;
			return ProjectPoint(pnt, ref segmentIndex, out pos, out dir, out along, out across);
		}

		public Projection ProjectPoint(Vector3 pnt, out Vector3 pos, out Vector3 dir, out float along, out float across)
		{
			int segmentIndex = -1;
			return ProjectPoint(pnt, ref segmentIndex, out pos, out dir, out along, out across);
		}

		public Projection ProjectPoint(Vector3 pnt, ref int segmentIndex, out Vector3 pos, out Vector3 dir, out float along, out float across)
		{
			float T;
			var proj = ProjectPointImpl(pnt, ref segmentIndex, out T, out pos, out dir);

			float d1 = m_distances[segmentIndex];
			float d2 = m_distances[segmentIndex + 1];
			along = d1 + (d2 - d1) * T;

			float dx = pnt.x - pos.x;
			float dy = pnt.y - pos.y;
			float dz = pnt.z - pos.z;
			across = (float)System.Math.Sqrt((double)(dx * dx + dy * dy + dz * dz));

			float cpY = (pnt.z - pos.z) * dir.x - (pnt.x - pos.x) * dir.z;

			if (cpY > 0.0f)
			{
				across = -across;
			}

			return proj;
		}

		private Projection ProjectPointImpl(Vector3 pnt, ref int segmentIndex, out float T, out Vector3 pos, out Vector3 dir)
		{
			T = 0f;
			pos = new Vector3();
			dir = new Vector3();

			Projection proj2, proj = Projection.Inside;
			float T2, sqrDistance, sqrDistance2, closestSqrDistance, closestSqrDistance2;
			Vector3 pos2, dir2;

			closestSqrDistance = closestSqrDistance2 = float.MaxValue;

			const float sqrDistanceThreshold = 25f * 25f;
			const float sqrDistanceYScale = 10f;

			int segmentsCount = m_worldPoints.Length - 1;

			int firstSegmentIndex = 0;
			int currentSegmentIndex = segmentIndex;
			bool currentSegmentIndexValid = false;

			if (currentSegmentIndex >= 0 && currentSegmentIndex < segmentsCount)
			{
				currentSegmentIndexValid = true;
				firstSegmentIndex = currentSegmentIndex;
			}

			proj2 = ProjectPointOnSegment(pnt, firstSegmentIndex, out T2, out pos2, out dir2);

			if (proj2 == Projection.Inside)
			{
				closestSqrDistance = GetSqrDistance(pnt, pos2, sqrDistanceYScale);
				closestSqrDistance2 = closestSqrDistance;
				proj = proj2;
				segmentIndex = firstSegmentIndex;
				T = T2;
				pos = pos2;
				dir = dir2;
			}

			for (int i = 0, iMax = segmentsCount; i < iMax; ++i)
			{
				if (i == firstSegmentIndex)
					continue;

				proj2 = ProjectPointOnSegment(pnt, i, out T2, out pos2, out dir2);

				if (proj2 == Projection.Inside)
				{
					sqrDistance = GetSqrDistance(pnt, pos2, sqrDistanceYScale);

					sqrDistance2 = sqrDistance;

					if (currentSegmentIndexValid && !IsNeighboringSegments(i, currentSegmentIndex))
					{
						sqrDistance2 += sqrDistanceThreshold;
					}

					if (sqrDistance2 < closestSqrDistance2)
					{
						closestSqrDistance = sqrDistance;
						closestSqrDistance2 = sqrDistance2;
						proj = proj2;
						segmentIndex = i;
						T = T2;
						pos = pos2;
						dir = dir2;
					}
				}
			}

			pos2 = m_worldPoints[0];
			dir2 = m_worldVelocities[0];

			sqrDistance = GetSqrDistance(pnt, pos2, sqrDistanceYScale);

			sqrDistance2 = sqrDistance;

			if (currentSegmentIndexValid && !IsNeighboringSegments(0, currentSegmentIndex))
			{
				sqrDistance2 += sqrDistanceThreshold;
			}

			if (sqrDistance2 < closestSqrDistance2)
			{
				closestSqrDistance = sqrDistance;
				closestSqrDistance2 = sqrDistance2;
				proj = Projection.Before;
				segmentIndex = 0;
				T = 0f;
				pos = pos2;
				dir = dir2;
			}

			for (int i = 1, iMax = m_worldPoints.Length; i < iMax; ++i)
			{
				pos2 = m_worldPoints[i];
				dir2 = m_worldVelocities[i];

				sqrDistance = GetSqrDistance(pnt, pos2, sqrDistanceYScale);

				sqrDistance2 = sqrDistance;

				if (currentSegmentIndexValid && !IsNeighboringSegments(i - 1, currentSegmentIndex))
				{
					sqrDistance2 += sqrDistanceThreshold;
				}

				if (sqrDistance2 < closestSqrDistance2)
				{
					closestSqrDistance = sqrDistance;
					closestSqrDistance2 = sqrDistance2;
					segmentIndex = i - 1;
					T = 1f;
					pos = pos2;
					dir = dir2;

					if (i == iMax - 1)
					{
						proj = Projection.After;
					}
				}
			}

			Assert.IsTrue(T >= 0f && T <= 1f);

			return proj;
		}

		private Projection ProjectPointOnSegment(Vector3 pnt, int segmentIndex, out float T, out Vector3 pos, out Vector3 dir)
		{
			Vector3 p1 = m_worldPoints[segmentIndex];
			Vector3 p2 = m_worldPoints[segmentIndex + 1];

			var proj = ProjectPointOnLine(pnt, p1, p2, out T, out pos, out dir);

			const float marginT = 0.2f;

			if (T + marginT >= 0f && T - marginT <= 1f)
			{
				int stepCount, closestI = 0;
				float stepLength, T2, sqrDistance, closestSqrDistance;
				Vector3 pos2, dir2;

				const float maxStepLength = 10f;
				const float invMaxStepLength = 1f / maxStepLength;

				DivideSegmentOnSteps(segmentIndex, maxStepLength, invMaxStepLength, out stepCount, out stepLength);

				var sc = new SampleCache();
				sc.Reset();
				FillSampleCache(ref sc, segmentIndex);

				closestSqrDistance = float.MaxValue;

				for (int i = 0; i < stepCount; ++i)
				{
					GetSamplePos(ref sc, (i + 1) * stepLength, ref p2);

					var proj2 = ProjectPointOnLine(pnt, p1, p2, out T2, out pos2, out dir2);

					sqrDistance = GetSqrDistance(pnt, pos2, 1f);

					if (sqrDistance < closestSqrDistance)
					{
						closestSqrDistance = sqrDistance;
						proj = proj2;
						closestI = i;
						T2 = T2 < 0 ? 0 : (T2 > 1 ? 1 : T2);
						T = (i * stepLength + T2 * stepLength) * sc.invSegmentLength;
						pos = pos2;
						dir = dir2;
					}

					p1.x = p2.x;
					p1.y = p2.y;
					p1.z = p2.z;
				}

				if ((proj == Projection.Before && closestI != 0) || (proj == Projection.After && closestI != stepCount - 1))
				{
					proj = Projection.Inside;
				}
			}

			return proj;
		}

		private Projection ProjectPointOnLine(Vector3 pnt, Vector3 p1, Vector3 p2, out float T, out Vector3 pos, out Vector3 dir)
		{
			dir.x = p2.x - p1.x;
			dir.y = p2.y - p1.y;
			dir.z = p2.z - p1.z;

			float d = -(dir.x * pnt.x + dir.y * pnt.y + dir.z * pnt.z);
			float div = dir.x * dir.x + dir.y * dir.y + dir.z * dir.z;
			T = -(d + (dir.x * p1.x + dir.y * p1.y + dir.z * p1.z)) / div;

			var proj = Projection.Inside;

			if (T < 0f)
			{
				proj = Projection.Before;
				pos.x = p1.x;
				pos.y = p1.y;
				pos.z = p1.z;
			}
			else if (T > 1f)
			{
				proj = Projection.After;
				pos.x = p2.x;
				pos.y = p2.y;
				pos.z = p2.z;
			}
			else
			{
				pos.x = p1.x + dir.x * T;
				pos.y = p1.y + dir.y * T;
				pos.z = p1.z + dir.z * T;
			}

			return proj;
		}

		public void SamplePoint(float distance, out Vector3 pnt, out Vector3 vel, out Vector3 acc)
		{
			var sc = new SampleCache();
			sc.Reset();
			SamplePoint(distance, ref sc, out pnt, out vel, out acc);
		}

		public void SamplePoint(float distance, out Vector3 pnt, out Vector3 vel)
		{
			var sc = new SampleCache();
			sc.Reset();
			SamplePoint(distance, ref sc, out pnt, out vel);
		}

		public void SamplePoint(float distance, out Vector3 pnt)
		{
			var sc = new SampleCache();
			sc.Reset();
			SamplePoint(distance, ref sc, out pnt);
		}

		public void SamplePoint(float distance, ref SampleCache sc, out Vector3 pos, out Vector3 vel, out Vector3 acc)
		{
			pos = vel = acc = new Vector3();
			int segmentIndex = GetSegmentIndexForDistance(distance);
			float distanceOnSegment = distance - m_distances[segmentIndex];
			FillSampleCache(ref sc, segmentIndex);
			GetSamplePos(ref sc, distanceOnSegment, ref pos);
			GetSampleVel(ref sc, distanceOnSegment, ref vel);
			GetSampleAcc(ref sc, distanceOnSegment, ref acc);
		}

		public void SamplePoint(float distance, ref SampleCache sc, out Vector3 pos, out Vector3 vel)
		{
			pos = vel = new Vector3();
			int segmentIndex = GetSegmentIndexForDistance(distance);
			float distanceOnSegment = distance - m_distances[segmentIndex];
			FillSampleCache(ref sc, segmentIndex);
			GetSamplePos(ref sc, distanceOnSegment, ref pos);
			GetSampleVel(ref sc, distanceOnSegment, ref vel);
		}

		public void SamplePoint(float distance, ref SampleCache sc, out Vector3 pos)
		{
			pos = new Vector3();
			int segmentIndex = GetSegmentIndexForDistance(distance);
			float distanceOnSegment = distance - m_distances[segmentIndex];
			FillSampleCache(ref sc, segmentIndex);
			GetSamplePos(ref sc, distanceOnSegment, ref pos);
		}

		private void GetSamplePos(ref SampleCache sc, float distanceOnSegment, ref Vector3 pos)
		{
			float T = distanceOnSegment * sc.invSegmentLength;
			float b1 = T * T;
			float b2 = T * T * T;
			pos.x = sc.c0 * b2 + sc.c3 * b1 + sc.c6 * T + sc.c9;
			pos.y = sc.c1 * b2 + sc.c4 * b1 + sc.c7 * T + sc.c10;
			pos.z = sc.c2 * b2 + sc.c5 * b1 + sc.c8 * T + sc.c11;
		}

		private void GetSampleVel(ref SampleCache sc, float distanceOnSegment, ref Vector3 vel)
		{
			float T = distanceOnSegment * sc.invSegmentLength;
			float b1 = T * 2f;
			float b2 = T * T * 3f;
			vel.x = sc.c0 * b2 + sc.c3 * b1 + sc.c6;
			vel.y = sc.c1 * b2 + sc.c4 * b1 + sc.c7;
			vel.z = sc.c2 * b2 + sc.c5 * b1 + sc.c8;
		}

		private void GetSampleAcc(ref SampleCache sc, float distanceOnSegment, ref Vector3 acc)
		{
			float T = distanceOnSegment * sc.invSegmentLength;
			float b1 = T * 6f;
			acc.x = sc.c0 * b1 + sc.c3 * 2f;
			acc.y = sc.c1 * b1 + sc.c4 * 2f;
			acc.z = sc.c2 * b1 + sc.c5 * 2f;
		}

		private void GetSampleJrk(ref SampleCache sc, ref Vector3 jrk)
		{
			jrk.x = sc.c0 * 6f;
			jrk.y = sc.c1 * 6f;
			jrk.z = sc.c2 * 6f;
		}

		private bool FillSampleCache(ref SampleCache sc, int segmentIndex)
		{
			if (sc.instanceID != m_cachedInstanceID)
			{
				sc.segmentIndex = -1;
				sc.instanceID = m_cachedInstanceID;
			}

			if (segmentIndex != sc.segmentIndex)
			{
				sc.segmentIndex = segmentIndex;

				float segmentLength = m_distances[segmentIndex + 1] - m_distances[segmentIndex];
				sc.invSegmentLength = 1f / segmentLength;

				Vector3 p1 = m_worldPoints[segmentIndex];
				Vector3 p2 = m_worldPoints[segmentIndex + 1];
				Vector3 v1 = m_worldVelocities[segmentIndex];
				Vector3 v2 = m_worldVelocities[segmentIndex + 1];

				float v1x_d = v1.x * segmentLength;
				float v1y_d = v1.y * segmentLength;
				float v1z_d = v1.z * segmentLength;

				float v2x_d = v2.x * segmentLength;
				float v2y_d = v2.y * segmentLength;
				float v2z_d = v2.z * segmentLength;

				sc.c0 = p1.x * 2f - p2.x * 2f + v1x_d + v2x_d;
				sc.c1 = p1.y * 2f - p2.y * 2f + v1y_d + v2y_d;
				sc.c2 = p1.z * 2f - p2.z * 2f + v1z_d + v2z_d;
				sc.c3 = p2.x * 3f - p1.x * 3f - v1x_d * 2f - v2x_d;
				sc.c4 = p2.y * 3f - p1.y * 3f - v1y_d * 2f - v2y_d;
				sc.c5 = p2.z * 3f - p1.z * 3f - v1z_d * 2f - v2z_d;
				sc.c6 = v1x_d;
				sc.c7 = v1y_d;
				sc.c8 = v1z_d;
				sc.c9 = p1.x;
				sc.c10 = p1.y;
				sc.c11 = p1.z;

				return true;
			}

			return false;
		}

		private int GetSegmentIndexForDistance(float distance)
		{
			Assert.IsTrue(distance >= 0.0f && distance <= length);

			int segmentIndex = 0;
			int iMax = m_distances.Length - 1;

			while (segmentIndex < iMax && m_distances[segmentIndex + 1] < distance)
			{
				++segmentIndex;
			}

			return segmentIndex < iMax ? segmentIndex : iMax - 1;
		}

		private bool IsNeighboringSegments(int segmentIndex1, int segmentIndex2)
		{
			if (segmentIndex1 > segmentIndex2)
			{
				int temp = segmentIndex1;
				segmentIndex1 = segmentIndex2;
				segmentIndex2 = temp;
			}

			const int threshold = 2;

			if (segmentIndex2 - segmentIndex1 > threshold)
			{
				int segmentsCount = m_worldPoints.Length - 1;

				if (!closed || (segmentIndex2 + threshold) < segmentsCount || (segmentIndex2 + threshold) % segmentsCount < segmentIndex1)
				{
					return false;
				}
			}

			return true;
		}

		private void DivideSegmentOnSteps(int segmentIndex, float maxStepLength, float invMaxStepLength, out int stepCount, out float stepLength)
		{
			stepCount = 1;
			stepLength = m_distances[segmentIndex + 1] - m_distances[segmentIndex];

			if (stepLength > maxStepLength)
			{
				stepCount = (int)System.Math.Ceiling((double)(stepLength * invMaxStepLength));
				stepLength = stepLength / (float)stepCount;
			}
		}

		private float GetSqrDistance(Vector3 p1, Vector3 p2, float yScale)
		{
			float dx = p1.x - p2.x;
			float dy = p1.y - p2.y;
			float dz = p1.z - p2.z;

			dx = dx * dx;
			dy = dy * dy * yScale;
			dz = dz * dz;

			return dx + dy + dz;
		}

		public static float CalcRadius(Vector3 vel, Vector3 acc)
		{
			float vx = vel.x;
			float vy = vel.y;
			float vz = vel.z;
			float ax = acc.x;
			float ay = acc.y;
			float az = acc.z;
			float radius = (vx * vx + vy * vy + vz * vz) / (float)System.Math.Sqrt((double)(ax * ax + ay * ay + az * az));
			return (vz * ax - vx * az < 0f) ? -radius : radius;
		}

		#endregion
	}

	public enum Projection
	{
		Inside, // t >= 0 && t <= 1
		Before, // t < 0
		After   // t > 1
	}

	public struct SampleCache
	{
		public int instanceID;
		public int segmentIndex;
		public float invSegmentLength;
		public float c0;
		public float c1;
		public float c2;
		public float c3;
		public float c4;
		public float c5;
		public float c6;
		public float c7;
		public float c8;
		public float c9;
		public float c10;
		public float c11;

		public Matrix4x4 mtx1;
		public Matrix4x4 mtx2;
		public Matrix4x4 mtx3;

		public void Reset()
		{
			instanceID = -1;
			segmentIndex = -1;
			mtx1 = mtx2 = mtx3 = new Matrix4x4();
			invSegmentLength = 0f;
		}
	}

	public interface ISpline
	{
		float length { get; }
		int pointCount { get; }
		bool closed { get; }

		void SetWorldPoints(Vector3[] points);
		void GetWorldPoints(out Vector3[] points);

		Vector3 GetWorldPoint(int index);
		Vector3 GetWorldVelocity(int index);

		Projection ProjectPoint(Vector3 pnt, out Vector3 pos, out Vector3 dir);
		Projection ProjectPoint(Vector3 pnt, out float along, out float across);
		Projection ProjectPoint(Vector3 pnt, ref int segmentIndex, out float along, out float across);
		void SamplePoint(float distance, out Vector3 pnt, out Vector3 vel, out Vector3 acc);
		void SamplePoint(float distance, out Vector3 pnt, out Vector3 vel);
		void SamplePoint(float distance, out Vector3 pnt);

		void SamplePoint(float distance, ref SampleCache sc, out Vector3 pos, out Vector3 vel, out Vector3 acc);
		void SamplePoint(float distance, ref SampleCache sc, out Vector3 pos);
	}

	public abstract class SplineBehaviour : MonoBehaviour
	{
		public abstract ISpline GetBaseSpline();
	}
}
