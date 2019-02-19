using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Transforms;
using Unity.Rendering;

public struct Boid:IComponentData
{
    public int boidId;
    public Vector3  force;
    public Vector3  velocity;
    public Vector3  up;
    
    public Vector3  acceleration;
    public float mass;
    public int neighbourCount;
}

public struct Seperation:IComponentData
{
    public Vector3 force;
    public float weight;
}

public struct Wander : IComponentData
{
    public Vector3 force;
    public float weight;

    public float distance;
    public float radius;
    public float jitter;
    public Vector3 target;
}

public class Bootstrap : MonoBehaviour
{
    private EntityArchetype cubeArchitype;
    private EntityManager entityManager;
    
    private RenderMesh renderMesh;
    public Mesh mesh;
    public Material material;

    public Vector3 seekTarget = Vector3.zero;

    Entity CreateCube(Vector3 pos, Quaternion q, int i)
    {
        Entity entity = entityManager.CreateEntity(cubeArchitype);

        Position p = new Position();
        p.Value = pos;

        Rotation r = new Rotation();
        r.Value = q;

        entityManager.SetComponentData(entity, p);
        entityManager.SetComponentData(entity, r);

        Scale s = new Scale();
        s.Value = new Vector3(1, 1, 3);

        entityManager.SetComponentData(entity, s);


        entityManager.SetComponentData(entity, new Boid() {boidId = i, mass = 1});
        entityManager.SetComponentData(entity, new Seperation() { weight = 1 });
        entityManager.SetComponentData(entity, new Wander() { weight = 1, distance = 15
            , radius = 10, jitter = 100, target = Random.insideUnitSphere * 10 });

        entityManager.AddSharedComponentData(entity, renderMesh);
        return entity;
    }

    public int numBoids = 100;

    // Start is called before the first frame update
    void Start()
    {
        entityManager = World.Active.GetOrCreateManager<EntityManager>();

        cubeArchitype = entityManager.CreateArchetype(
            typeof(Position),
            typeof(Rotation),
            typeof(Scale),
            typeof(Boid),
            typeof(Seperation)
        );

        renderMesh = new RenderMesh();
        renderMesh.mesh = mesh;
        renderMesh.material = material;

        for (int i = 0; i < numBoids; i++)
        {
            Vector3 pos = Random.insideUnitSphere * 20;
            CreateCube(transform.position + pos, Quaternion.identity, i);
        }
    
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            seekTarget = Camera.main.transform.position
               + Camera.main.transform.forward * 200;
        }
    }
}
