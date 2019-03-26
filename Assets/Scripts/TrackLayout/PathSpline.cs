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
using System.Collections;
using System.Collections.Generic;
using SplineLogic;
using UnityEngine;
using UnityEngine.Assertions;

namespace TrackLayout
{
	/*
	 * http://www.iquilezles.org/www/articles/minispline/minispline.htm
	 * http://www.malinc.se/m/MakingABezierSpline.php
	 * https://www.habrador.com/tutorials/interpolation/1-catmull-rom-splines/
	 * https://medium.com/@all2one/how-to-compute-the-length-of-a-spline-e44f5f04c40
	 */

	public class PathSpline : ISpline
	{
		// GaussLengendreCoefficient : x = abscissa, y = weight
		private static readonly Vector2[] s_gaussLengendreCoefficients =
		{
			new Vector2(0.0f, 0.5688889f),
			new Vector2(-0.5384693f, 0.47862867f),
			new Vector2(0.5384693f, 0.47862867f),
			new Vector2(-0.90617985f, 0.23692688f),
			new Vector2(0.90617985f, 0.23692688f)
		};

		private readonly bool m_isLooped;
		private readonly Vector3 m_endPoint;
		private readonly Vector3[] m_segments;
		private readonly float[] m_lengths;
		private readonly float m_totalLength;
		private readonly int m_segmentsCount;

		public float totalLength { get { return m_totalLength; } }
		public int segmentsCount { get { return m_segmentsCount; } }
		public bool isLooped { get { return m_isLooped; } }

		public PathSpline(Vector3[] points, bool isLooped)
		{
			if (points.Length < 2)
			{
				return;
			}

			m_isLooped = isLooped;
			if (m_isLooped)
			{
				var bias = 0;
				if ((points[0] - points[points.Length - 1]).sqrMagnitude < 0.01f)
				{
					bias = 1;
					if (points.Length - bias < 2)
					{
						return;
					}
				}

				m_segmentsCount = points.Length - bias;
				m_endPoint = points[0];
			}
			else
			{
				m_segmentsCount = points.Length - 1;
				m_endPoint = points[points.Length - 1];
			}
			m_segments = new Vector3[m_segmentsCount * 4];
			m_lengths = new float[m_segmentsCount];

			for (int current = 0; current < m_segmentsCount; current++)
			{
				int previous, start, end, next;
				if (m_isLooped)
				{
					previous = current == 0 ? m_segmentsCount - 1 : current - 1;
					start = current;
					end = current == m_segmentsCount - 1 ? 0 : current + 1;
					next = end == m_segmentsCount - 1 ? 0 : end + 1;
				}
				else
				{
					previous = current == 0 ? current : current - 1;
					start = current;
					end = current + 1;
					next = end == m_segmentsCount ? end : end + 1;
				}

				var p0 = points[previous] * 0.5f;
				var p1 = points[start] * 0.5f;
				var p2 = points[end] * 0.5f;
				var p3 = points[next] * 0.5f;
				var a = 2 * p1;
				var b = p2 - p0;
				var c = 2 * p0 - 5 * p1 + 4 * p2 - p3;
				var d = -p0 + 3 * p1 - 3 * p2 + p3;
				m_segments[current * 4] = a;
				m_segments[current * 4 + 1] = b;
				m_segments[current * 4 + 2] = c;
				m_segments[current * 4 + 3] = d;

				float length = 0.0f;
				for (int i = 0; i < s_gaussLengendreCoefficients.Length; i++)
				{
					var t = 0.5f * (1.0f + s_gaussLengendreCoefficients[i].x);
					Vector3 velocity;
					ComputeTangent(b, c, d, t, out velocity);
					length += velocity.magnitude * s_gaussLengendreCoefficients[i].y;
				}
				length *= 0.5f;
				m_totalLength += length;
				m_lengths[current] = length;
			}
		}

		public float GetLength(int segmentIndex)
		{
			return m_lengths[segmentIndex];
		}

		public Vector3 GetPoint(int index)
		{
			return index < m_segmentsCount ? m_segments[index * 4] : m_endPoint;
		}

		private void LimitDistance(ref float distance)
		{
			if (isLooped)
			{
				if (distance > m_totalLength)
				{
					distance -= m_totalLength;
				}
				else if (distance < 0.0f)
				{
					distance += m_totalLength;
				}
			}
			else
			{
				distance = Mathf.Clamp(distance, 0.0f, m_totalLength);
			}
		}

		public void Sample(float step, ref List<Vector3> samples)
		{
			if (m_segmentsCount == 0)
			{
				return;
			}

			var stepCount = Mathf.CeilToInt(m_totalLength / step);
			var sampleCount = stepCount + 1;
			step = m_totalLength / stepCount;

			var coveredLength = 0.0f;
			var segmentDistance = 0.0f;
			var segmentIndex = 0;
			var segmentLength = m_lengths[segmentIndex];
			for (int i = 0; i < sampleCount; i++)
			{
				while (coveredLength > segmentDistance + segmentLength && coveredLength < m_totalLength)
				{
					segmentIndex++;
					segmentDistance += segmentLength;
					segmentLength = m_lengths[segmentIndex];
				}
				var t = ((coveredLength - segmentDistance)) / segmentLength;
				Vector3 p;
				Sample(segmentIndex, t, out p);
				samples.Add(p);
				coveredLength += step;
			}
		}

		public void Sample(int segmentIndex, float t, out Vector3 position)
		{
			segmentIndex *= 4;
			var a = m_segments[segmentIndex];
			var b = m_segments[segmentIndex + 1];
			var c = m_segments[segmentIndex + 2];
			var d = m_segments[segmentIndex + 3];

			ComputePosition(a, b, c, d, t, out position);
		}

		public void Sample(float distance, out Vector3 position)
		{
			LimitDistance(ref distance);

			int segmentIndex;
			float coveredLength;
			GetSegmentDistance(distance, out segmentIndex, out coveredLength);
			var length = m_lengths[segmentIndex];
			var t = (length - (coveredLength - distance)) / length;
			segmentIndex *= 4;
			var a = m_segments[segmentIndex];
			var b = m_segments[segmentIndex + 1];
			var c = m_segments[segmentIndex + 2];
			var d = m_segments[segmentIndex + 3];

			ComputePosition(a, b, c, d, t, out position);
		}

		public void Sample(float distance, out Vector3 position, out Vector3 tangent)
		{
			LimitDistance(ref distance);

			int segmentIndex;
			float coveredLength;
			GetSegmentDistance(distance, out segmentIndex, out coveredLength);
			var length = m_lengths[segmentIndex];
			var t = (length - (coveredLength - distance)) / length;
			segmentIndex *= 4;
			var a = m_segments[segmentIndex];
			var b = m_segments[segmentIndex + 1];
			var c = m_segments[segmentIndex + 2];
			var d = m_segments[segmentIndex + 3];

			ComputePosition(a, b, c, d, t, out position);
			ComputeTangent(b, c, d, t, out tangent);
		}

		public void Sample(float distance, out Vector3 position, out Vector3 tangent, out Vector3 normal)
		{
			LimitDistance(ref distance);

			int segmentIndex;
			float coveredLength;
			GetSegmentDistance(distance, out segmentIndex, out coveredLength);
			var length = m_lengths[segmentIndex];
			var t = (length - (coveredLength - distance)) / length;
			segmentIndex *= 4;
			var a = m_segments[segmentIndex];
			var b = m_segments[segmentIndex + 1];
			var c = m_segments[segmentIndex + 2];
			var d = m_segments[segmentIndex + 3];

			ComputePosition(a, b, c, d, t, out position);
			ComputeTangent(b, c, d, t, out tangent);
			ComputeCurvature(c, d, t, out normal);
		}

		public void GetSegmentDistance(float distance, out int segmentIndex, out float coveredLength)
		{
			coveredLength = 0.0f;
			for (int i = 0; i < m_lengths.Length; i++)
			{
				coveredLength += m_lengths[i];
				if (distance < coveredLength)
				{
					segmentIndex = i;
					return;
				}
			}
			segmentIndex = m_lengths.Length - 1;
		}

		public Vector3 InaccurateFindNearest(Vector3 point, out int bestSegmentIndex, out float bestT, out Vector3 bestDelta, out float bestDistanceSq, int fromSegmentIndex = 0)
		{
			const float hScale = 1.0f;
			const float vScale = 100.0f;

			var closestPointIndex = 0;
			var minDistanceSq = float.MaxValue;

			Vector3 delta;
			for (int i = fromSegmentIndex; i < m_segmentsCount; i++)
			{
				var a = GetPoint(i);
				var b = GetPoint(i + 1);
				var closestPoint = ClosestPointOnSegemnt(a, b, point);
				delta.x = closestPoint.x - point.x;
				delta.y = closestPoint.y - point.y;
				delta.z = closestPoint.z - point.z;

				var distanceSq = delta.x * delta.x * hScale + delta.y * delta.y * vScale + delta.z * delta.z * hScale;
				if (distanceSq < minDistanceSq)
				{
					minDistanceSq = distanceSq;
					closestPointIndex = i;
				}
			}

			bestSegmentIndex = 0;
			bestT = 0.0f;
			bestDelta.x = 0.0f;
			bestDelta.y = 0.0f;
			bestDelta.z = 0.0f;
			bestDistanceSq = float.MaxValue;
			Vector3 bestPoint;
			bestPoint.x = 0.0f;
			bestPoint.y = 0.0f;
			bestPoint.z = 0.0f;
			for (int i = -1; i < 2; i++)
			{
				var segmentIndex = closestPointIndex + i;
				if (segmentIndex < 0)
				{
					segmentIndex = m_segmentsCount - 1;
				}
				else if (segmentIndex >= m_segmentsCount)
				{
					segmentIndex -= m_segmentsCount;
				}
				float localDistanceSq;
				Vector3 localPoint, localDelta;
				var localT = InaccurateFindNearestOnSegment(point, segmentIndex, out localPoint, out localDelta, out localDistanceSq);
				if (localDistanceSq < bestDistanceSq)
				{
					bestSegmentIndex = segmentIndex;
					bestT = localT;
					bestPoint = localPoint;
					bestDelta = localDelta;
					bestDistanceSq = localDistanceSq;
				}
			}

			return bestPoint;
		}

		public static Vector3 ClosestPointOnSegemnt(Vector3 a, Vector3 b, Vector3 p)
		{
			Vector3 lineVec, pointVec;
			lineVec.x = b.x - a.x;
			lineVec.y = b.y - a.y;
			lineVec.z = b.z - a.z;
			pointVec.x = p.x - a.x;
			pointVec.y = p.y - a.y;
			pointVec.z = p.z - a.z;

			var lineVecLengthSq = lineVec.x * lineVec.x + lineVec.y * lineVec.y + lineVec.z * lineVec.z;
			var lineVecLengthInv = 1.0f / Mathf.Sqrt(lineVecLengthSq);

			var t = pointVec.x * lineVec.x + pointVec.y * lineVec.y + pointVec.z * lineVec.z;
			if (t > 0)
			{
				t *= lineVecLengthInv * lineVecLengthInv;
				if (t < 1.0f)
				{
					pointVec.x = a.x + lineVec.x * t;
					pointVec.y = a.y + lineVec.y * t;
					pointVec.z = a.z + lineVec.z * t;
					return pointVec;
				}
				return b;
			}
			return a;
		}

		private float InaccurateFindNearestOnSegment(Vector3 point, int segmentIndex, out Vector3 foundPoint, out Vector3 delta, out float foundDistanceSq)
		{
			segmentIndex *= 4;
			var a = m_segments[segmentIndex];
			var b = m_segments[segmentIndex + 1];
			var c = m_segments[segmentIndex + 2];
			var d = m_segments[segmentIndex + 3];

			float distanceSq0, distanceSq1, distanceSq2;
			Vector3 delta0, delta1, delta2;
			Vector3 foundPoint0, foundPoint1, foundPoint2;

			var foundT0 = InaccurateFindNearestOnSegment(point, a, b, c, d, 0.0f, out foundPoint0, out delta0, out distanceSq0);
			var foundT1 = InaccurateFindNearestOnSegment(point, a, b, c, d, 0.5f, out foundPoint1, out delta1, out distanceSq1);
			var foundT2 = InaccurateFindNearestOnSegment(point, a, b, c, d, 1.0f, out foundPoint2, out delta2, out distanceSq2);

			float t;
			if (distanceSq0 <= distanceSq1 && distanceSq0 <= distanceSq2)
			{
				foundDistanceSq = distanceSq0;
				t = foundT0;
				delta = delta0;
				foundPoint = foundPoint0;
			}
			else if (distanceSq1 <= distanceSq2)
			{
				foundDistanceSq = distanceSq1;
				t = foundT1;
				delta = delta1;
				foundPoint = foundPoint1;
			}
			else
			{
				foundDistanceSq = distanceSq2;
				t = foundT2;
				delta = delta2;
				foundPoint = foundPoint2;
			}

			return t;
		}

		private static float InaccurateFindNearestOnSegment(Vector3 targetPoint, Vector3 a, Vector3 b, Vector3 c,Vector3 d, float startT, out Vector3 foundPoint, out Vector3 delta, out float distanceSq)
		{
			var lastBestT = startT;
			var lastMove = 1.0f;
			var scale = 0.75f;

			float t, move;
			Vector3 lastBestTangent;

			t = lastBestT;
			ComputePosition(a, b, c, d, t, out foundPoint);
			for (int i = 0; i < 3; i++)
			{
				ComputeTangent(b, c, d, t, out lastBestTangent);
				delta.x = targetPoint.x - foundPoint.x;
				delta.y = targetPoint.y - foundPoint.y;
				delta.z = targetPoint.z - foundPoint.z;
				move = (lastBestTangent.x * delta.x + lastBestTangent.y * delta.y + lastBestTangent.z * delta.z) / (lastBestTangent.x * lastBestTangent.x + lastBestTangent.y * lastBestTangent.y + lastBestTangent.z * lastBestTangent.z);
				lastMove *= scale;
				move = Mathf.Clamp(move, -lastMove, lastMove);
				lastBestT += move;
				lastBestT = Mathf.Clamp(lastBestT, 0.0f, 1.0f);
				lastMove = move > 0 ? move : -move;

				t = lastBestT;
				ComputePosition(a, b, c, d, t, out foundPoint);
			}
			delta.x = targetPoint.x - foundPoint.x;
			delta.y = targetPoint.y - foundPoint.y;
			delta.z = targetPoint.z - foundPoint.z;
			distanceSq = delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;
			return lastBestT;
		}

		private static void ComputePosition(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t, out Vector3 result)
		{
			// p = a + b*t + c*t^2 + d*t^3;
			result.x = a.x + t * (b.x + t * (c.x + d.x * t));
			result.y = a.y + t * (b.y + t * (c.y + d.y * t));
			result.z = a.z + t * (b.z + t * (c.z + d.z * t));
		}

		private static void ComputeTangent(Vector3 b, Vector3 c, Vector3 d, float t, out Vector3 result)
		{
			// v = b + 2*c*t + 3*d*t^2
			result.x = b.x + t * (2.0f * c.x + 3.0f * d.x * t);
			result.y = b.y + t * (2.0f * c.y + 3.0f * d.y * t);
			result.z = b.z + t * (2.0f * c.z + 3.0f * d.z * t);
		}

		private static void ComputeCurvature(Vector3 c, Vector3 d, float t, out Vector3 result)
		{
			// a = 2*c + 6*d*t;
			result.x = c.x * 2.0f + 6.0f * d.x * t;
			result.y = c.y * 2.0f + 6.0f * d.y * t;
			result.z = c.z * 2.0f + 6.0f * d.z * t;
		}

		// ISpline Interface Implementaion
		public float length { get { return m_totalLength; } }
		public int pointCount { get { return m_segmentsCount; } }
		public bool closed { get { return m_isLooped; } }
		public Vector3 GetWorldPoint(int index)
		{
			return GetPoint(index);
		}

		public Projection ProjectPoint(Vector3 pnt, out Vector3 pos, out Vector3 dir)
		{
			int segmentIndex;
			float bestT;
			Vector3 bestDelta;
			float bestDistanceSq;
			pos = InaccurateFindNearest(pnt, out segmentIndex, out bestT, out bestDelta, out bestDistanceSq);
			var b = m_segments[segmentIndex * 4 + 1];
			var c = m_segments[segmentIndex * 4 + 2];
			var d = m_segments[segmentIndex * 4 + 3];
			ComputeTangent(b, c, d, bestT, out dir);
			return Projection.Inside;
		}

		public Projection ProjectPoint(Vector3 pnt, out float along, out float across)
		{
			int segmentIndex;
			float bestT;
			Vector3 bestDelta;
			float bestDistanceSq;
			InaccurateFindNearest(pnt, out segmentIndex, out bestT, out bestDelta, out bestDistanceSq);
			along = 0.0f;
			for (int i = 0; i < segmentIndex; i++)
			{
				along += m_lengths[i];
			}
			along += m_lengths[segmentIndex] * bestT;
			across = Mathf.Sqrt(bestDistanceSq);
			var b = m_segments[segmentIndex * 4 + 1];
			var c = m_segments[segmentIndex * 4 + 2];
			var d = m_segments[segmentIndex * 4 + 3];
			Vector3 dir;
			ComputeTangent(b, c, d, bestT, out dir);
			if (bestDelta.z * dir.x - bestDelta.x * dir.z > 0.0f)
			{
				across = -across;
			}
			return Projection.Inside;
		}

		public Projection ProjectPoint(Vector3 pnt, ref int segmentIndex, out float along, out float across)
		{
			segmentIndex = Mathf.Clamp(segmentIndex, 0, m_segmentsCount);

			float bestT;
			Vector3 bestDelta;
			float bestDistanceSq;
			InaccurateFindNearest(pnt, out segmentIndex, out bestT, out bestDelta, out bestDistanceSq, segmentIndex);
			along = 0.0f;
			for (int i = 0; i < segmentIndex; i++)
			{
				along += m_lengths[i];
			}
			along += m_lengths[segmentIndex] * bestT;
			across = Mathf.Sqrt(bestDistanceSq);
			var b = m_segments[segmentIndex * 4 + 1];
			var c = m_segments[segmentIndex * 4 + 2];
			var d = m_segments[segmentIndex * 4 + 3];
			Vector3 dir;
			ComputeTangent(b, c, d, bestT, out dir);
			if (bestDelta.z * dir.x - bestDelta.x * dir.z > 0.0f)
			{
				across = -across;
			}
			return Projection.Inside;
		}

		public void SamplePoint(float distance, out Vector3 pnt, out Vector3 vel, out Vector3 acc)
		{
			Sample(distance, out pnt, out vel, out acc);
		}

		public void SamplePoint(float distance, out Vector3 pnt, out Vector3 vel)
		{
			Sample(distance, out pnt, out vel);

		}

		public void SamplePoint(float distance, out Vector3 pnt)
		{
			Sample(distance, out pnt);
		}

		public Vector3 GetWorldVelocity(int index)
		{
			throw new NotImplementedException();
		}

		public void SamplePoint(float distance, ref SampleCache sc, out Vector3 pos, out Vector3 vel, out Vector3 acc)
		{
			throw new NotImplementedException();
		}

		public void SamplePoint(float distance, ref SampleCache sc, out Vector3 pos)
		{
			throw new NotImplementedException();
		}

		public void SetWorldPoints(Vector3[] points)
		{
			throw new NotImplementedException();
		}

		public void GetWorldPoints(out Vector3[] points)
		{
			throw new NotImplementedException();
		}
	}
}