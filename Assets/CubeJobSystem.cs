using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class CubeJobSystem : JobComponentSystem
{
    [BurstCompile]
    struct SeperationJob: IJobProcessComponentData<Boid, Seperation>
    {
        [NativeDisableParallelForRestriction]
        public NativeMultiHashMap<int, int> neighbours;

        [NativeDisableParallelForRestriction]
        public NativeArray<Vector3> positions;

        public void Execute(ref Boid b, ref Seperation s)
        {

            Vector3 force = Vector3.zero;
            NativeMultiHashMapIterator<int> iterator;
            int neighbourID;
            if (neighbours.TryGetFirstValue(b.boidId, out neighbourID, out iterator))
            {
                Vector3 toNeighbour = positions[b.boidId] - positions[neighbourID];
                force += (Vector3.Normalize(toNeighbour) / toNeighbour.magnitude);

                while(neighbours.TryGetNextValue(out neighbourID, ref iterator))
                {
                    // The same as above
                    toNeighbour = positions[b.boidId] - positions[neighbourID];
                    force += (Vector3.Normalize(toNeighbour) / toNeighbour.magnitude);
                }
            }

            s.force = force * s.weight;
            b.force += s.force;
        }        
    }


    [BurstCompile]
    struct BoidJob : IJobProcessComponentData<Boid>
    {
        [ReadOnly] public float deltaTime;

        [NativeDisableParallelForRestriction]
        public NativeArray<Vector3> positions;
        
        [NativeDisableParallelForRestriction]
        public NativeArray<Quaternion> rotations;

        public float maxSpeed;
        public float maxForce;
        public float damping;
        public float banking;

        public float dT;

        public Vector3 seekTarget;

        public Vector3 Seek(Vector3 target, ref Boid b)
        {
            Vector3 toTarget = target - positions[b.boidId];

            Vector3 desired = toTarget.normalized * maxSpeed;
            return desired - b.velocity; 
        }

        public void Execute(ref Boid b)
        {
            /*
            Vector3 force = Seek(Vector3.zero, ref  b);
            b.acceleration = force / b.mass;
            b.velocity += b.acceleration * dT;
            positions[b.boidId] += b.velocity * dT;
            if (b.velocity.magnitude > float.Epsilon)
            {
                rotations[b.boidId] = Quaternion.LookRotation(b.velocity.normalized);
            }
            */

            Vector3 newAcceleration = b.force / b.mass;
            b.acceleration = Vector3.Lerp(b.acceleration, newAcceleration, dT);
            b.velocity += b.acceleration * dT;

            b.velocity = Vector3.ClampMagnitude(b.velocity, maxSpeed);

            if (b.velocity.magnitude > 0)
            {
                Vector3 tempUp = Vector3.Lerp(b.up, Vector3.up + (b.acceleration * banking), dT * 3.0f);
                rotations[b.boidId] = Quaternion.LookRotation(b.velocity, tempUp);
                b.up = rotations[b.boidId] * Vector3.up;

                positions[b.boidId] += b.velocity * dT;
                b.velocity *= (1.0f - (damping * dT));
            }
            b.force = Vector3.zero;
        }
    }

    [BurstCompile]
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

    [BurstCompile]
    struct CopyTransformsFromJob :IJobProcessComponentData<Position, Rotation, Boid>
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

    [BurstCompile]
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
    int numBoids;

    float maxForce = 5;
    float maxSpeed = 5;

    Bootstrap bootstrap;

    protected override void OnCreateManager()
    {
        bootstrap = GameObject.FindObjectOfType<Bootstrap>();
        numBoids = bootstrap.numBoids;


        neighbours = new NativeMultiHashMap<int, int>(numBoids * maxNeighbours, Allocator.Persistent);
        positions = new NativeArray<Vector3>(numBoids, Allocator.Persistent);
        rotations = new NativeArray<Quaternion>(numBoids, Allocator.Persistent);

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
            positions = this.positions,
            rotations = this.rotations
        };
        var ctjHandle = ctj.Schedule(this, inputDeps);

        // Count Neigthbours
        neighbours.Clear();
        var cnj = new CountNeighboursJob()
        {
            positions = this.positions,
            rotations = this.rotations,
            neighbours = this.neighbours,
            neighbourDistance = 50

        };
        var cnjHandle = cnj.Schedule(positions.Length, 10, ctjHandle);

        var seperationJob = new SeperationJob()
        {
            positions = this.positions,
            neighbours = this.neighbours
        };

        var sjHandle = seperationJob.Schedule(this, cnjHandle);
        
        // Integrate the forces
        var boidJob = new BoidJob()
        {
            positions = this.positions,
            rotations = this.rotations,
            dT = Time.deltaTime,
            maxForce = this.maxForce,
            maxSpeed = this.maxSpeed,
            seekTarget = bootstrap.seekTarget,
            damping = 0.01f,
            banking = 0.1f

        };
        var boidHandle = boidJob.Schedule(this, sjHandle);

        // Copy back to the entities
        var cfj = new CopyTransformsFromJob()
        {
            positions = this.positions,
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