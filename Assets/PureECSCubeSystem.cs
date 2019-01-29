using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct PureECSCube : IComponentData
{
}

public class PureECSCubeSystem : ComponentDataWrapper<PureECSCube>
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
