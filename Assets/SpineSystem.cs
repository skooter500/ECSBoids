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
    public struct Spine : IComponentData
    {
        public int parent;
        public int spineId;
        public Vector3 offset;
    }

    struct SpineJob : IJobProcessComponentData<Spine, Position, Rotation>
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector3> positions;

        [NativeDisableParallelForRestriction]
        public NativeArray<Quaternion> rotations;

        public float dT;
        public float bondDamping;
        public float angularBondDamping;

        public void Execute(ref Spine s, ref Position p, ref Rotation r)
        {
            // Is it the root of a spine?
            if (s.parent == -1)
            {
                return;
            }
            Vector3 wantedPosition = positions[s.parent] + rotations[s.parent] * s.offset;
            //p.Value = Vector3.Lerp(p.Value, wantedPosition, bondDamping * dT);

            // Clamp the distance
            Vector3 lerpedPosition = Vector3.Lerp(p.Value, wantedPosition, bondDamping * dT);
            Vector3 clampedOffset = lerpedPosition - positions[s.parent];
            clampedOffset = Vector3.ClampMagnitude(clampedOffset, s.offset.magnitude);
            //positions[s.spineId] = Vector3.Lerp(positions[s.spineId], wantedPosition, bondDamping * dT);
            positions[s.spineId] = positions[s.parent] + clampedOffset;
            Vector3 myPos = positions[s.spineId];
            Quaternion wantedQuaternion = Quaternion.LookRotation(positions[s.parent] - myPos);
            rotations[s.spineId] = Quaternion.Slerp(rotations[s.spineId], wantedQuaternion, angularBondDamping * dT);
            //r.Value = Quaternion.Slerp(r.Value, wantedQuaternion, angularBondDamping * dT);
        }
    }

    [BurstCompile]
    struct CopyTransformsToSpineJob : IJobProcessComponentData<Position, Rotation, Spine>
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector3> positions;
        [NativeDisableParallelForRestriction]
        public NativeArray<Quaternion> rotations;

        public void Execute(ref Position p, ref Rotation r, ref Spine s)
        {
            positions[s.spineId] = p.Value;
            rotations[s.spineId] = r.Value;
        }

    }

    [BurstCompile]
    struct CopyTransformsFromSpineJob : IJobProcessComponentData<Position, Rotation, Spine>
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector3> positions;

        [NativeDisableParallelForRestriction]
        public NativeArray<Quaternion> rotations;

        public void Execute(ref Position p, ref Rotation r, ref Spine s)
        {
            p.Value = positions[s.spineId];
            r.Value = rotations[s.spineId];
        }
    }

    [UpdateAfter(typeof(BoidJobSystem))]
    public class SpineSystem : JobComponentSystem
    {
        public BoidBootstrap bootstrap;

        public NativeArray<Vector3> positions;
        public NativeArray<Quaternion> rotations;

        public const int MAX_SPINES = 300000;
        public int numSpines = 0;

        public static SpineSystem Instance;

        protected override void OnCreateManager()
        {
            Instance = this;
            base.OnCreateManager();

            bootstrap = GameObject.FindObjectOfType<BoidBootstrap>();

            positions = new NativeArray<Vector3>(MAX_SPINES, Allocator.Persistent);
            rotations = new NativeArray<Quaternion>(MAX_SPINES, Allocator.Persistent);
            numSpines = 0;
        }

        protected override void OnDestroyManager()
        {
            positions.Dispose();
            rotations.Dispose();
        }


        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var ctj = new CopyTransformsToSpineJob()
            {
                positions = this.positions,
                rotations = this.rotations
            };

            var ctjHandle = ctj.Schedule(this, inputDeps);

            var spineJob = new SpineJob()
            {
                positions = this.positions,
                rotations = this.rotations,
                angularBondDamping = bootstrap.angularDamping,
                bondDamping = bootstrap.bondDamping,
                dT = Time.deltaTime
            };
            var spineHandle = spineJob.Schedule(this, ctjHandle);

            var cfj = new CopyTransformsFromSpineJob()
            {
                positions = this.positions,
                rotations = this.rotations
            };

            return cfj.Schedule(this, spineHandle);
            //return spineJob.Schedule(this, ctjHandle);
        }

    }
}
