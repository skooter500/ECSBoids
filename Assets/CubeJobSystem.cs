using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class CubeJobSystem : JobComponentSystem
{
    struct CubeJob : IJobProcessComponentData<Position, Rotation, Boid>
    {
        [ReadOnly] public float deltaTime;

        public void Execute(ref Position p, ref Rotation q, ref Boid b)
        {
            Vector3 pos = p.Value;
            pos.y += deltaTime;
            p.Value = pos;
        }
    }

    struct CopyTransformsToJob:IJobProcessComponentData<Position, Rotation, Boid>
    {
        public NativeArray<Vector3> positions;
        public NativeArray<Quaternion> rotations;

        public void Execute(ref Position p, ref Rotation r, ref Boid b)
        {
            positions[b.boidId] = p.Value;
            rotations[b.boidId] = r.Value;
        }
        
    }
    struct CopyTransformsFromJob:IJobProcessComponentData<Position, Rotation, Boid>
    {
        public NativeArray<Vector3> positions;
        public NativeArray<Quaternion> rotations;

        public void Execute(ref Position p, ref Rotation r, ref Boid b)
        {
            p.Value = positions[b.boidId];
            r.Value = rotations[b.boidId];
        }
        
    }
    struct CountNeighboursJob : IJobParallelFor
    {        
        [NativeDisableParallelForRestriction]
        public NativeArray<int> counts;        
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector3> positions;        
        [NativeDisableParallelForRestriction]
        public NativeArray<Quaternion> rotations;
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
    */

    private NativeArray<int> counts;
    public NativeArray<Vector3> positions;
    public NativeArray<Quaternion> rotations;

    protected override void OnCreateManager()
    {
        counts = new NativeArray<int>(100, Allocator.Persistent);
        positions = new NativeArray<Vector3>(100, Allocator.Persistent);
        rotations = new NativeArray<Quaternion>(100, Allocator.Persistent);
    }

    protected override void OnDestroyManager()
    {
        counts.Dispose();
        positions.Dispose();
        rotations.Dispose();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        /* 
        var job = new CubeJob()
        {
            deltaTime = Time.deltaTime
        };
        */

        var ctj = new CopyTransformsToJob()
        {
            positions = this.positions
            , rotations = this.rotations
        };

        var cfj = new CopyTransformsFromJob()
        {
            positions = this.positions
            , rotations = this.rotations
        };

        /*
        var cnj = new CountNeighboursJob()
        {
            counts = this.counts
            , positions = this.data.positions
            , neighbourDistance = 20
        };
        */

        var ctjHandle = ctj.Schedule(this, inputDeps);

        return cfj.Schedule(this, ctjHandle);
    }

    /*
    [Inject] CubeGroup cubeGroup;
    struct CubeGroup
    {
        public int Length;
        ComponentDataArray<Position> positionCDA;
        ComponentDataArray<Rotation> rotationCDA;
        
    }
    */
}