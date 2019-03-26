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

namespace TrackLayout
{
	[Flags]
	public enum IgnoreFlags
	{
		None = 0x0,
		Background = 0x1,
		PerfectLine = 0x2,
		Checkpoints = 0x4,
		Sectors = 0x8,
		ClipZones = 0x10,
		Rules = 0x20
	}

	public enum BlipType
	{
		Player0,
		Player1
	}

	public enum CheckpointType
	{
		Start,
		Finish,
		Checkpoint
	}

	public enum ClipZoneType
	{
		Default,
		Bad,
		PointsFactor,
		SpeedControl,
	}


	public enum RuleFlag
	{
		TargetSide,
		MinSpeed,

	}

	public enum TargetSide
	{
		NotActive,
		Left,
		Right,
		StrictLeft,
		StrictRight,
		Depleted
	}

	public enum PathLaneType
	{
		Left,
		Central,
		Right,
		RaceAI,
	}
}