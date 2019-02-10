using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    public Mesh boidMesh;
    public Material boidMaterial;

    public int count = 100;

    // Start is called before the first frame update
    void Start()
    {
        var entityManager = World.Active.GetOrCreateManager<EntityManager>();
        
        var boidArchitype = entityManager.CreateArchetype(
            typeof(PositionComponent)
            , typeof(RotationComponent)
            , typeof(RenderMeshComponent)
            );


        for (int i = 0; i < count; i ++)
        {
            var boid = entityManager.CreateEntity(boidArchitype);
            entityManager.SetComponentData(boid, new PositionComponent()
            {
                Value = new Position()
            }
            );

        // Update is called once per frame
            void Update()
    {
        
    }
}
