using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;


public struct BakeJob: IJobParallelFor
{
	private NativeArray<int> meshIds;

	public BakeJob(NativeArray<int> meshIds)
	{
		this.meshIds = meshIds;
	}

	public void Execute(int index)
	{
		Physics.BakeMesh(meshIds[index], false);
	}
}


public static class JobifiedBaking
{
	const int meshesPerJob = 10;
	public static void BakeMeshes(List<Chunk> chunks)
	{
		NativeArray<int> meshIds = new NativeArray<int>(chunks.Count, Allocator.TempJob);

		for (int i = 0; i < chunks.Count; i++)
		{
			meshIds[i] = chunks[i].mesh.GetInstanceID();
		}

		var job = new BakeJob(meshIds);
		job.Schedule(meshIds.Length, meshesPerJob).Complete();

		meshIds.Dispose();

		for (int i = 0; i < chunks.Count; i++)
		{
			chunks[i].SetCollider();
		}
	}
}
