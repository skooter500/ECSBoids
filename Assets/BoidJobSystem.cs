﻿using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ew
{
    public struct Boid : IComponentData
    {
        public int boidId;
        public Vector3 force;
        public Vector3 velocity;
        public Vector3 up;

        public Vector3 acceleration;
        public float mass;
        public float maxSpeed;
        public float maxForce;
        public float weight;
        public int taggedCount;

        public Vector3 fleeForce; // Have to put this here because there is a limit to the number of components in IJobProcessComponentData
        public Vector3 seekForce; // Have to put this here because there is a limit to the number of components in IJobProcessComponentData
    }

    public struct Flee : IComponentData
    {
        public Vector3 force;
    }
    public struct Seek : IComponentData
    {
        public Vector3 force;
    }

    public struct Seperation : IComponentData
    {
        public Vector3 force;
    }

    public struct Constrain : IComponentData
    {
        public Vector3 force;
    }

    public struct Cohesion : IComponentData
    {
        public Vector3 force;
    }

    public struct Alignment : IComponentData
    {
        public Vector3 force;
    }

    public struct Wander : IComponentData
    {
        public Vector3 force;

        public float distance;
        public float radius;
        public float jitter;
        public Vector3 target;
    }


    public class BoidJobSystem : JobComponentSystem
    {
        public static BoidJobSystem Instance;
        static public bool checkNaN(Quaternion v)
        {
            if (float.IsNaN(v.x) || float.IsInfinity(v.x))
            {
                return true;
            }
            if (float.IsNaN(v.y) || float.IsInfinity(v.y))
            {
                return true;
            }
            if (float.IsNaN(v.z) || float.IsInfinity(v.z))
            {
                return true;
            }
            if (float.IsNaN(v.w) || float.IsInfinity(v.w))
            {
                return true;
            }
            return false;
        }

        static public bool checkNaN(Vector3 v)
        {
            if (float.IsNaN(v.x) || float.IsInfinity(v.x))
            {
                return true;
            }
            if (float.IsNaN(v.y) || float.IsInfinity(v.y))
            {
                return true;
            }
            if (float.IsNaN(v.z) || float.IsInfinity(v.z))
            {
                return true;
            }
            return false;
        }

        [BurstCompile]
        struct PartitionSpaceJob : IJobParallelFor
        {

            [NativeDisableParallelForRestriction]
            public NativeArray<Vector3> positions;

            public NativeMultiHashMap<int, int>.Concurrent cells;
            public bool threedcells;
            public int cellSize;
            public int gridSize;

            public static Vector3 CellToPosition(int cell, int cellSize, int gridSize)
            {
                int row = cell / gridSize;
                int col = cell - (row * gridSize);

                return new Vector3(col * cellSize, 0, row * cellSize);
            }

            public static int PositionToCell(Vector3 pos, int cellSize, int gridSize)
            {
                return ((int)(pos.x / cellSize))
                    + ((int)(pos.z / cellSize)) * gridSize;
            }
            public static int PositionToCell3D(Vector3 pos, int cellSize, int gridSize)
            {
                return ((int)(pos.x / cellSize))
                    + ((int)(pos.z / cellSize)) * gridSize
                    + ((int)(pos.y / cellSize)) * gridSize * gridSize;
            }

            public void Execute(int i)
            {
                int cell = threedcells
                    ? PositionToCell3D(positions[i], cellSize, gridSize)
                    : PositionToCell(positions[i], cellSize, gridSize);
                cells.Add(cell, i);
            }
        }



        [BurstCompile]
        struct SeperationJob : IJobProcessComponentData<Boid, Seperation>
        {
            [ReadOnly]
            public NativeArray<int> neighbours;

            [NativeDisableParallelForRestriction]
            public NativeArray<Vector3> positions;
            public float weight;
            public int maxNeighbours;
            public Unity.Mathematics.Random random;

            public int spineOffset;
            public int spineLength;

            public void Execute(ref Boid b, ref Seperation s)
            {
                Vector3 force = Vector3.zero;
                int neighbourStartIndex = maxNeighbours * b.boidId;
                int mySpineId = b.boidId * (spineLength + 1);
                Vector3 myPosition = positions[mySpineId + spineOffset];
                for (int i = 0; i < b.taggedCount; i++)
                {
                    int neighbourId = neighbours[neighbourStartIndex + i];
                    if (neighbourId == b.boidId)
                    {
                        continue;
                    }
                    int neighbourSpineId = (neighbourId * (spineLength + 1)) + spineOffset;
                    //Vector3 toNeighbour = positions[b.boidId] - positions[neighbourId];
                    Vector3 toNeighbour = myPosition - positions[neighbourSpineId];
                    float mag = toNeighbour.magnitude;
                    //force += (Vector3.Normalize(toNeighbour) / mag);


                    if (mag > 0) // Need this check otherwise this behaviour can return NAN
                    {
                        force += (Vector3.Normalize(toNeighbour) / mag);
                    }
                    else
                    {
                        // same position, so generate a random force
                        Vector3 f = random.NextFloat3Direction();
                        force += f * b.maxForce;
                    }

                }
                s.force = force * weight;
            }
        }

        [BurstCompile]
        struct ConstrainJob : IJobProcessComponentData<Boid, Constrain>
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<Vector3> positions;
            public Vector3 centre;
            public float radius;
            public float weight;

            public void Execute(ref Boid b, ref Constrain c)
            {
                Vector3 force = Vector3.zero;
                Vector3 toTarget = positions[b.boidId] - centre;
                if (toTarget.magnitude > radius)
                {
                    force = Vector3.Normalize(toTarget) * (radius - toTarget.magnitude);
                }
                c.force = force * weight;
            }
        }

        [BurstCompile]
        struct FleeJob : IJobProcessComponentData<Boid, Flee>
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<Vector3> positions;
            public Vector3 enemyPos;
            public float distance;
            public float weight;

            public void Execute(ref Boid b, ref Flee f)
            {
                Vector3 desired = enemyPos - positions[b.boidId];
                if (desired.magnitude <= distance)
                {
                    desired.Normalize();
                    desired *= b.maxSpeed;
                    b.fleeForce = (b.velocity - desired) * weight;
                }
                else
                {
                    b.fleeForce = Vector3.zero;
                }

            }
        }

        [BurstCompile]
        struct SeekJob : IJobProcessComponentData<Boid, Seek>
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<Vector3> positions;
            public Vector3 targetPos;
            public float distance;
            public float weight;

            public void Execute(ref Boid b, ref Seek s)
            {
                Vector3 desired = targetPos - positions[b.boidId];
                desired.Normalize();
                desired *= b.maxSpeed;
                b.seekForce = (desired - b.velocity) * weight;
            }
        }

        [BurstCompile]
        struct CohesionJob : IJobProcessComponentData<Boid, Cohesion>
        {
            [ReadOnly]
            public NativeArray<int> neighbours;

            [NativeDisableParallelForRestriction]
            public NativeArray<Vector3> positions;

            public int maxNeighbours;
            public float weight;

            public void Execute(ref Boid b, ref Cohesion c)
            {
                Vector3 force = Vector3.zero;
                Vector3 centerOfMass = Vector3.zero;
                int neighbourStartIndex = maxNeighbours * b.boidId;
                for (int i = 0; i < b.taggedCount; i++)
                {
                    int neighbourId = neighbours[neighbourStartIndex + i];
                    centerOfMass += positions[neighbourId];
                }
                if (b.taggedCount > 0)
                {
                    centerOfMass /= b.taggedCount;
                    // Generate a seek force
                    Vector3 toTarget = centerOfMass - positions[b.boidId];
                    Vector3 desired = toTarget.normalized * b.maxSpeed;
                    force = (desired - b.velocity).normalized;
                }

                c.force = force * weight;
            }
        }

        [BurstCompile]
        struct AlignmentJob : IJobProcessComponentData<Boid, Alignment>
        {
            [ReadOnly]
            public NativeArray<int> neighbours;

            [NativeDisableParallelForRestriction]
            public NativeArray<Vector3> positions;

            [NativeDisableParallelForRestriction]
            public NativeArray<Quaternion> rotations;
            public float weight;
            public int maxNeighbours;

            public void Execute(ref Boid b, ref Alignment a)
            {
                Vector3 desired = Vector3.zero;
                Vector3 force = Vector3.zero;
                int neighbourStartIndex = maxNeighbours * b.boidId;
                for (int i = 0; i < b.taggedCount; i++)
                {
                    int neighbourId = neighbours[neighbourStartIndex + i];
                    desired += rotations[neighbourId] * Vector3.forward;
                }

                if (b.taggedCount > 0)
                {
                    desired /= b.taggedCount;
                    force = desired - (rotations[b.boidId] * Vector3.forward);
                }

                a.force = force * weight;
            }
        }

        [BurstCompile]
        struct WanderJob : IJobProcessComponentData<Boid, Wander, Position, Rotation>
        {
            public float dT;
            public Unity.Mathematics.Random random;
            public float weight;

            public void Execute(ref Boid b, ref Wander w, ref Position p, ref Rotation r)
            {
                Vector3 disp = w.jitter * random.NextFloat3Direction() * dT;
                w.target += disp;
                w.target.Normalize();
                w.target *= w.radius;

                Vector3 localTarget = (Vector3.forward * w.distance) + w.target;

                Quaternion q = r.Value;
                Vector3 pos = p.Value;
                Vector3 worldTarget = (q * localTarget) + pos;
                w.force = (worldTarget - pos) * weight;
            }
        }


        [BurstCompile]
        struct BoidJob : IJobProcessComponentData<Boid, Seperation, Alignment, Cohesion, Wander, Constrain>
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<Vector3> positions;

            [NativeDisableParallelForRestriction]
            public NativeArray<Quaternion> rotations;

            [NativeDisableParallelForRestriction]
            public NativeArray<float> speeds;

            public float damping;
            public float banking;
            public float limitUpAndDown;

            public float dT;

            public Vector3 AccululateForces(ref Boid b, ref Seperation s, ref Alignment a, ref Cohesion c, ref Wander w, ref Constrain con)
            {
                Vector3 force = Vector3.zero;

                force += b.fleeForce;
                if (force.magnitude >= b.maxForce)
                {
                    force = Vector3.ClampMagnitude(force, b.maxForce);
                    return force;
                }


                force += s.force;
                if (force.magnitude >= b.maxForce)
                {
                    force = Vector3.ClampMagnitude(force, b.maxForce);
                    return force;
                }
                force += a.force;
                if (force.magnitude >= b.maxForce)
                {
                    force = Vector3.ClampMagnitude(force, b.maxForce);
                    return force;
                }

                force += c.force;
                if (force.magnitude >= b.maxForce)
                {
                    force = Vector3.ClampMagnitude(force, b.maxForce);
                    return force;
                }

                force += w.force;
                if (force.magnitude >= b.maxForce)
                {
                    force = Vector3.ClampMagnitude(force, b.maxForce);
                    return force;
                }

                force += con.force;
                if (force.magnitude >= b.maxForce)
                {
                    force = Vector3.ClampMagnitude(force, b.maxForce);
                    return force;
                }

                force += b.seekForce;
                if (force.magnitude >= b.maxForce)
                {
                    force = Vector3.ClampMagnitude(force, b.maxForce);
                    return force;
                }



                //NativeArray<Vector3> forces;

                //forces = new NativeArray<Vector3>(4, Allocator.Temp);

                //forces[0] = s.force;
                //forces[1] = a.force;
                //forces[2] = c.force;
                //forces[3] = w.force;
                //Vector3 force = Vector3.zero;
                /*
                foreach(Vector3 f in forces)
                {
                    force += f;

                    float fm = force.magnitude;
                    if (fm >= b.maxForce)
                    {
                        force = Vector3.ClampMagnitude(force, b.maxForce);
                        break;
                    }
                }
                */
                return force;
            }

            public void Execute(ref Boid b, ref Seperation s, ref Alignment a, ref Cohesion c, ref Wander w, ref Constrain con)
            {
                b.force = AccululateForces(ref b, ref s, ref a, ref c, ref w, ref con) * b.weight;

                b.force = Vector3.ClampMagnitude(b.force, b.maxForce);
                Vector3 newAcceleration = (b.force * b.weight) / b.mass;
                newAcceleration.y *= limitUpAndDown;
                b.acceleration = Vector3.Lerp(b.acceleration, newAcceleration, dT);
                b.velocity += b.acceleration * dT;
                b.velocity = Vector3.ClampMagnitude(b.velocity, b.maxSpeed);
                //b.velocity.y *= 0.8f;
                float speed = b.velocity.magnitude;
                speeds[b.boidId] = speed;
                if (speed > 0)
                {
                    Vector3 tempUp = Vector3.Lerp(b.up, (Vector3.up) + (b.acceleration * banking), dT * 3.0f);
                    rotations[b.boidId] = Quaternion.LookRotation(b.velocity, tempUp);
                    b.up = rotations[b.boidId] * Vector3.up;

                    positions[b.boidId] += b.velocity * dT;
                    b.velocity *= (1.0f - (damping * dT));

                }
                b.force = Vector3.zero;
            }
        }

        [BurstCompile]
        struct CopyTransformsToJob : IJobProcessComponentData<Position, Rotation, Boid>
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
        struct CopyTransformsFromJob : IJobProcessComponentData<Position, Rotation, Boid>
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
        struct CountNeighboursJob : IJobProcessComponentData<Boid>
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<int> neighbours;

            [NativeDisableParallelForRestriction]
            public NativeArray<Vector3> positions;

            [NativeDisableParallelForRestriction]
            public NativeArray<Quaternion> rotations;

            [ReadOnly]
            public NativeMultiHashMap<int, int> cells;
            public float neighbourDistance;
            public int maxNeighbours;

            public int cellSize;
            public int gridSize;
            public bool threedcells;

            public bool usePatritioning;


            public void Execute(ref Boid b)
            {
                int neighbourStartIndex = maxNeighbours * b.boidId;
                int neighbourCount = 0;


                if (usePatritioning)
                {
                    int surroundingCellCount = (int)Mathf.Ceil(neighbourDistance / cellSize);

                    // Are we looking above and below? 
                    int sliceSurrounding = threedcells ? surroundingCellCount : 0;
                    for (int slice = -sliceSurrounding; slice <= sliceSurrounding; slice++)
                    {
                        for (int row = -surroundingCellCount; row <= surroundingCellCount; row++)
                        {
                            for (int col = -surroundingCellCount; col <= surroundingCellCount; col++)
                            {
                                Vector3 pos = positions[b.boidId] + new Vector3(col * cellSize, slice * cellSize, row * cellSize);
                                int cell = PartitionSpaceJob.PositionToCell(pos, cellSize, gridSize);

                                NativeMultiHashMapIterator<int> iterator;
                                int boidId;
                                if (cells.TryGetFirstValue(cell, out boidId, out iterator))
                                {
                                    do
                                    {
                                        if (boidId != b.boidId)
                                        {
                                            if (Vector3.Distance(positions[b.boidId], positions[boidId]) < neighbourDistance)
                                            {
                                                neighbours[neighbourStartIndex + neighbourCount] = boidId;
                                                neighbourCount++;
                                                if (neighbourCount == maxNeighbours)
                                                {
                                                    b.taggedCount = neighbourCount;
                                                    return;
                                                }
                                            }
                                        }
                                    } while (cells.TryGetNextValue(out boidId, ref iterator));
                                }
                            }
                        }

                    }
                }
                else
                {
                    for (int i = 0; i < positions.Length; i++)
                    {
                        if (i != b.boidId)
                        {
                            if (Vector3.Distance(positions[b.boidId], positions[i]) < neighbourDistance)
                            {
                                neighbours[neighbourStartIndex + neighbourCount] = i;
                                neighbourCount++;
                                if (neighbourCount == maxNeighbours)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
                b.taggedCount = neighbourCount;
            }
        }

        NativeArray<int> neighbours;
        public NativeArray<Vector3> positions;
        public NativeArray<Quaternion> rotations;
        public NativeArray<float> speeds;

        public NativeMultiHashMap<int, int> cells;

        int maxNeighbours = 100;

        BoidBootstrap bootstrap;

        protected override void OnCreateManager()
        {
            Instance = this;
            bootstrap = GameObject.FindObjectOfType<BoidBootstrap>();

            // Want to use this but it hangs when I try and access it
            // neighbours = new NativeMultiHashMap<int, int>(10000, Allocator.Persistent);
            neighbours = new NativeArray<int>(bootstrap.numBoids * maxNeighbours, Allocator.Persistent);
            positions = new NativeArray<Vector3>(bootstrap.numBoids, Allocator.Persistent);
            rotations = new NativeArray<Quaternion>(bootstrap.numBoids, Allocator.Persistent);
            speeds = new NativeArray<float>(bootstrap.numBoids, Allocator.Persistent); // Needed for the animations
            cells = new NativeMultiHashMap<int, int>(bootstrap.numBoids, Allocator.Persistent);
        }

        protected override void OnDestroyManager()
        {
            neighbours.Dispose();
            positions.Dispose();
            rotations.Dispose();
            speeds.Dispose();
            cells.Dispose();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {

            Unity.Mathematics.Random ran = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(1, 100000));

            // Copy entities to the native arrays 
            var ctj = new CopyTransformsToJob()
            {
                positions = this.positions,
                rotations = this.rotations
            };
            var ctjHandle = ctj.Schedule(this, inputDeps);


            cells.Clear();
            var partitionJob = new PartitionSpaceJob()
            {
                positions = this.positions,
                cells = this.cells.ToConcurrent(),
                threedcells = bootstrap.threedcells,
                cellSize = bootstrap.cellSize,
                gridSize = bootstrap.gridSize
            };

            var partitionHandle = partitionJob.Schedule(bootstrap.numBoids, 50, ctjHandle);

            // Count Neigthbours

            var cnj = new CountNeighboursJob()
            {
                positions = this.positions,
                rotations = this.rotations,
                neighbours = this.neighbours,
                maxNeighbours = bootstrap.totalNeighbours,
                cells = this.cells,
                cellSize = bootstrap.cellSize,
                gridSize = bootstrap.gridSize,
                usePatritioning = bootstrap.usePartitioning,
                neighbourDistance = bootstrap.neighbourDistance

            };
            var cnjHandle = cnj.Schedule(this, partitionHandle);

            var seperationJob = new SeperationJob()
            {
                positions = SpineSystem.Instance.positions,
                spineLength = bootstrap.spineLength,
                spineOffset = bootstrap.spineLength / 2,
                maxNeighbours = this.maxNeighbours,
                random = ran,
                neighbours = this.neighbours,
                weight = bootstrap.seperationWeight
            };

            var sjHandle = seperationJob.Schedule(this, cnjHandle);

            var alignmentJob = new AlignmentJob()
            {
                positions = this.positions,
                rotations = this.rotations,
                maxNeighbours = this.maxNeighbours,
                neighbours = this.neighbours,
                weight = bootstrap.alignmentWeight
            };

            var ajHandle = alignmentJob.Schedule(this, sjHandle);

            var cohesionJob = new CohesionJob()
            {
                positions = this.positions,
                maxNeighbours = this.maxNeighbours,
                neighbours = this.neighbours,
                weight = bootstrap.cohesionWeight

            };

            var cjHandle = cohesionJob.Schedule(this, ajHandle);

            var wanderJob = new WanderJob()
            {
                dT = Time.deltaTime * bootstrap.speed,
                random = ran,
                weight = bootstrap.wanderWeight

            };

            var wjHandle = wanderJob.Schedule(this, cjHandle);

            var constrainJob = new ConstrainJob()
            {
                positions = this.positions,
                centre = bootstrap.constrainPosition,
                radius = bootstrap.radius,
                weight = bootstrap.constrainWeight
            };

            var constrainHandle = constrainJob.Schedule(this, wjHandle);

            var fleeJob = new FleeJob()
            {
                positions = this.positions,
                enemyPos = Camera.main.transform.position,
                distance = bootstrap.fleeDistance,
                weight = bootstrap.fleeWeight
            };

            var fleeHandle = fleeJob.Schedule(this, constrainHandle);

            var seekJob = new SeekJob()
            {
                positions = this.positions,
                targetPos = Camera.main.transform.position,
                weight = bootstrap.seekWeight
            };

            var seekHandle = seekJob.Schedule(this, fleeHandle);



            // Integrate the forces
            var boidJob = new BoidJob()
            {
                positions = this.positions,
                rotations = this.rotations,
                speeds = this.speeds,
                dT = Time.deltaTime * bootstrap.speed,
                damping = 0.01f,
                limitUpAndDown = bootstrap.limitUpAndDown,
                banking = 0.01f
            };
            var boidHandle = boidJob.Schedule(this, seekHandle);

            // Copy back to the entities 
            var cfj = new CopyTransformsFromJob()
            {
                positions = this.positions,
                rotations = this.rotations
            };
            return cfj.Schedule(this, boidHandle);
        }
    }
}