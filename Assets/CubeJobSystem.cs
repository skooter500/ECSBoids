using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class CubeJobSystem : JobComponentSystem
{
    struct CubeJob : IJobProcessComponentData<Position, Rotation>
    {
        [ReadOnly] public float deltaTime;

        public void Execute(ref Position p, ref Rotation q)
        {
            Vector3 pos = p.Value;
            pos.y += deltaTime;
            p.Value = pos;
        }
    }

    struct CountNeighboursJob : IJobParallelFor
    {
        public ComponentDataArray<Position> positions;

        [NativeDisableParallelForRestriction]
        public NativeArray<int> counts;
        public float neighbourDistance;
        public void Execute(int index)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                int count = 0;
                if (i != index)
                {
                    if (Vector3.Distance(positions[index].Value, positions[i].Value) < neighbourDistance)
                    {
                        count++;
                    }
                }
                counts[index] = count;
            }
            throw new System.NotImplementedException();
        }
    }

    private struct Data
    {
        public readonly int Length;
        public EntityArray Entities;
        public ComponentDataArray<Position> positions;
    }

    [Inject] private Data data;

    private NativeArray<int> counts;

    protected override void OnCreateManager()
    {
        counts = new NativeArray<int>(100, Allocator.Persistent);
    }

    protected override void OnDestroyManager()
    {
        counts.Dispose();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new CubeJob()
        {
            deltaTime = Time.deltaTime
        };

        var cnj = new CountNeighboursJob()
        {
            counts = this.counts
            , positions = this.data.positions
            , neighbourDistance = 20
        };

        return cnj.Schedule(data.Length, 10, inputDeps);
    }
}