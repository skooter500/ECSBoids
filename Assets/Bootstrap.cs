using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Transforms;
using Unity.Rendering;

public class Bootstrap : MonoBehaviour
{
    private EntityArchetype cubeArchitype;
    private EntityManager entityManager;
    
    private RenderMesh renderMesh;
    public Mesh mesh;
    public Material material;

    Entity CreateCube(Vector3 pos, Quaternion q)
    {
        Entity entity = entityManager.CreateEntity(cubeArchitype);

        Position p = new Position();
        p.Value = pos;

        Rotation r = new Rotation();
        r.Value = q;

        entityManager.SetComponentData(entity, p);
        entityManager.SetComponentData(entity, r);

        entityManager.AddSharedComponentData(entity, renderMesh);
        return entity;
    }

    int numCubes = 100;

    // Start is called before the first frame update
    void Start()
    {
        entityManager = World.Active.GetOrCreateManager<EntityManager>();

        cubeArchitype = entityManager.CreateArchetype(
            typeof(Position),
            typeof(Rotation)
        );

        renderMesh = new RenderMesh();
        renderMesh.mesh = mesh;
        renderMesh.material = material;

        for (int i = 0; i < numCubes; i++)
        {
            Vector3 pos = Random.insideUnitSphere * 100;
            CreateCube(transform.position + pos, Quaternion.identity);
        }

    }
}
