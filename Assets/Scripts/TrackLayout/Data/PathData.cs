﻿// Copyright (C) CarX Technologies, 2019, carx-tech.com
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
using UnityEngine;
using SplineLogic;

namespace TrackLayout.Data
{
	[Serializable]
	public struct PathData
	{
		public Vector3[] centralPoints;
		public Vector3[] leftPoints;
		public Vector3[] rightPoints;
		public bool closed;

		public ISpline BuildSpline(PathLaneType type)
		{
			switch (type)
			{
				case PathLaneType.Left:
					return new PathSpline(leftPoints, closed);
				case PathLaneType.Central:
					return new PathSpline(centralPoints, closed);
				case PathLaneType.Right:
					return new PathSpline(rightPoints, closed);
			}
			return null;
		}
	}
}