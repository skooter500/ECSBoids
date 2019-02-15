using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class CubeJobSystem : JobComponentSystem
{
    struct BoidJob : IJobProcessComponentData<Boid>
    {
        [ReadOnly] public float deltaTime;

        [NativeDisableParallelForRestriction]
        public NativeArray<Vector3> positions;
        
        [NativeDisableParallelForRestriction]
        public NativeArray<Quaternion> rotations;

        public float maxSpeed;
        public float maxForce;

        public float dT;

        public Vector3 Seek(Vector3 target, ref Boid b)
        {
            Vector3 toTarget = target - positions[b.boidId];

            Vector3 desired = toTarget.normalized * maxSpeed;
            return desired - b.velocity; 
        }

        public void Execute(ref Boid b)
        {
            Vector3 force = Seek(Vector3.zero, ref  b);
            b.acceleration = force / b.mass;
            b.velocity += b.acceleration * dT;
            positions[b.boidId] += b.velocity * dT; 
            if (b.velocity.magnitude > 0)
            {
                rotations[b.boidId] =  Quaternion.LookRotation(b.velocity);
            }
        }
    }

    struct CopyTransformsToJob:IJobProcessComponentData<Position, Rotation, Boid>
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector3> positions;
        [NativeDisableParallelForRestriction]
        public NativeArray<Quaternion> rotations;

        public void Execute(ref Position p, ref Rotation r, ref Boid b)
        {
            positions[b.boidId] = p.Value;
            rotations[b.boidId] = r.Value;
        }
        
    }
    struct CopyTransformsFromJob:IJobProcessComponentData<Position, Rotation, Boid>
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector3> positions;

        [NativeDisableParallelForRestriction]
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
        public NativeMultiHashMap<int, int> neighbours;
        [NativeDisableParallelForRestriction]

        public NativeArray<Vector3> positions;        
        [NativeDisableParallelForRestriction]
        public NativeArray<Quaternion> rotations;
        public float neighbourDistance;
        public void Execute(int index)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                if (i != index)
                {
                    if (Vector3.Distance(positions[index], positions[i]) < neighbourDistance)
                    {
                        neighbours.Add(index, i);
                    }
                }
            }
        }
    }
    
    NativeMultiHashMap<int, int> neighbours;
    public NativeArray<Vector3> positions;
    public NativeArray<Quaternion> rotations;

    int maxNeighbours = 20;
    int maxBoids = 100;

    float maxForce = 5;
    float maxSpeed = 10;

    protected override void OnCreateManager()
    {
        neighbours = new NativeMultiHashMap<int, int>(maxBoids * maxNeighbours, Allocator.Persistent);
        positions = new NativeArray<Vector3>(maxBoids, Allocator.Persistent);
        rotations = new NativeArray<Quaternion>(maxBoids, Allocator.Persistent);
    }

    protected override void OnDestroyManager()
    {
        neighbours.Dispose();
        positions.Dispose();
        rotations.Dispose();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
   
        
        // Copy entities to the native arrays 
        var ctj = new CopyTransformsToJob()
        {
            positions = this.positions
            ,
            rotations = this.rotations
        };
        var ctjHandle = ctj.Schedule(this, inputDeps);

        // Count Neigthbours
        neighbours.Clear();
        var cnj = new CountNeighboursJob()
        {
            positions = this.positions
            ,
            rotations = this.rotations
            ,
            neighbours = this.neighbours
        };
        var cnjHandle = cnj.Schedule(positions.Length, 10, ctjHandle);
        
        // Integrate the forces
        var boidJob = new BoidJob()
        {
            positions = this.positions,
            rotations = this.rotations,
            dT = Time.deltaTime,
            maxForce = this.maxForce,
            maxSpeed = this.maxSpeed
        };
        var boidHandle = boidJob.Schedule(this, cnjHandle);


        // Copy back to the entities
        var cfj = new CopyTransformsFromJob()
        {
            positions = this.positions
            ,
            rotations = this.rotations
        };
        return cfj.Schedule(this, boidHandle);
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