using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class CubeSystem : ComponentSystem
{
    private struct Filter
    {
        public Transform t;
        public CubeComponent cc;
    }
    protected override void OnUpdate()
    {
        var deltaTime = Time.deltaTime;
        foreach (var entity in GetEntities<Filter>())
        {
            entity.t.Translate(0, 0, deltaTime * entity.cc.speed * Input.GetAxis("Horizontal"));
            entity.t.localScale = new Vector3(1, entity.cc.scale, 1); 
        }
    }
}

public class CubeScaleSystem : ComponentSystem
{
    private struct Data
    {
        public readonly int Length;
        public ComponentArray<CubeComponent> cubeComponents;
    }

    [Inject] private Data data;

    protected override void OnUpdate()
    {
        float t = Time.deltaTime;
        for (int i = 0; i < data.Length; i++)
        {
            data.cubeComponents[i].theta += t;
            data.cubeComponents[i].scale = 2 + Mathf.Sin(data.cubeComponents[i].theta);
        }
    }
}
