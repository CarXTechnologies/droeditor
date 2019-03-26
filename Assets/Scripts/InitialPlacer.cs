using TrackLayout.Data;
using UnityEditor;
using UnityEngine;

public class InitialPlacer : MonoBehaviour
{
	public TrackLayoutData dest;
	public bool looped = true;
	public float width = 6f;

	[Button("PlaceCW")]
	public void PlaceCW()
	{
		Place(false);
	}

	[Button("PlaceCCW")]
	public void PlaceCCW()
	{
		Place(true);
	}

	public void Place(bool isCCW)
	{
		if (dest == null)
		{
			return;
		}

		var centerLine = new Vector3[transform.childCount];
		for (int i = 0; i < transform.childCount; ++i)
		{
			centerLine[i] = transform.GetChild(i).position;
		}

		var leftLine = new Vector3[centerLine.Length];
		var rightLine = new Vector3[centerLine.Length];
		for (int i = 0; i < centerLine.Length; ++i)
		{
			Vector3 direction;
			if (i < centerLine.Length - 1)
			{
				direction = (centerLine[i + 1] - centerLine[i]).normalized;
			}
			else
			{
				direction = (centerLine[i] - centerLine[i - 1]).normalized;
			}
			direction *= width;
			var normal = new Vector3(direction.z, direction.y, -direction.x);

			if (isCCW)
			{
				normal = -normal;
			}
			leftLine[i] = centerLine[i] - normal;
			rightLine[i] = centerLine[i] + normal;
		}

		dest.path.closed = looped;
		dest.path.centralPoints = centerLine;
		dest.path.leftPoints = leftLine;
		dest.path.rightPoints = rightLine;
		EditorUtility.SetDirty(dest);
	}
}
