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
using UnityEngine;
using SplineLogic;
using TrackLayout.Data;

namespace TrackLayout.Builders
{
	public static class BuilderUtils
	{
		public static void CalcStep(float length, float desiredStep, out float step, out int samplesCount)
		{
			var stepCount = Mathf.CeilToInt(Mathf.Abs(length) / desiredStep);
			step = length / stepCount;
			samplesCount = stepCount + 1;
		}

		public static Vector3[] SampleClipZone(ISpline centralSpline, ISpline leftSpline, ISpline rightSpline, ClipZoneData data, float step)
		{
			int sampleCount;
			CalcStep(data.length, step, out step, out sampleCount);

			var fwdPointPrev = Vector3.zero;
			var revPointPrev = Vector3.zero;
			var revPoints = new List<Vector3>();
			var fwdPoints = new List<Vector3>();
			for (int i = 0; i < sampleCount; i++)
			{
				Vector3 cp, lp, lv, rp, rv;
				centralSpline.SamplePoint(data.distance + i * step, out cp);
				leftSpline.ProjectPoint(cp, out lp, out lv);
				rightSpline.ProjectPoint(cp, out rp, out rv);

				var crossVec = rp - lp;
				var crossLength = crossVec.magnitude;
				var crossDir = crossVec / crossLength;

				var p = lp;
				var binormal = crossDir;
				var offsetVec = binormal * (data.width);
				var biasVec = binormal * (crossLength - data.width) * data.bias;

				var fwdPoint = p + biasVec;
				var revPoint = p + offsetVec + biasVec;

				if (i > 0)
				{
					var minSqDist = step * 0.2f;
					minSqDist *= minSqDist;
					if ((fwdPoint - fwdPointPrev).sqrMagnitude > minSqDist)
					{
						fwdPoints.Add(fwdPoint);
						fwdPointPrev = fwdPoint;
					}
					if ((revPoint - revPointPrev).sqrMagnitude > minSqDist)
					{
						revPoints.Add(revPoint);
						revPointPrev = revPoint;
					}
				}
				else
				{
					fwdPoints.Add(fwdPoint);
					revPoints.Add(revPoint);
					fwdPointPrev = fwdPoint;
					revPointPrev = revPoint;
				}
			}
			revPoints.Reverse();
			fwdPoints.AddRange(revPoints);
			return fwdPoints.ToArray();
		}
	}
}