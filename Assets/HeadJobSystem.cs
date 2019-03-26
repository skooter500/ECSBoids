using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
/*

public class HeadJobSystem : JobComponentSystem
{
    [BurstCompile]
    struct HeadJob : IJobProcessComponentData<Head, Position, Rotation>
    {
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector3> boidPositions;

        public void Execute(ref Head h, ref Position p, ref Rotation r)
        {

        }
    }

    private BoidJobSystem

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var headJob = new HeadJob
    }
}
*/