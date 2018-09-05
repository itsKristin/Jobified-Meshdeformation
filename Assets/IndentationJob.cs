using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;

public struct IndentationJob : IJobParallelFor {

	public NativeArray<Vector3> contactPoints;
	public NativeArray<Vector3> initialVertices;
	public NativeArray<Vector3> modifiedVertices;

	public float force;
	public float radius;

	public void Execute(int i)
	{
		for(int c = 0; c < contactPoints.Length; c++)
		{
			Vector3 pointToVert = (modifiedVertices[i] - contactPoints[c]);
			float distance = pointToVert.sqrMagnitude;

			if(distance < radius)
			{
				Vector3 newVertice = initialVertices[i] + Vector3.down * (force);
				modifiedVertices[i] = newVertice;
			}
			
		}
	}
}
