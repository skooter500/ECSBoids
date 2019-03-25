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
    public float maxSpeed;
    public float maxForce;
    public float weight;
    public int taggedCount;
}

public struct Seperation:IComponentData
{
    public Vector3 force;
    public float weight;
}

public struct Constrain : IComponentData
{
    public Vector3 force;
    public float weight;
}

public struct Cohesion : IComponentData
{
    public Vector3 force;
    public float weight;
}

public struct Alignment : IComponentData
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

    Entity CreateBoid(Vector3 pos, Quaternion q, int i)
    {
        Entity entity = entityManager.CreateEntity(cubeArchitype);

        Position p = new Position();
        p.Value = pos;

        Rotation r = new Rotation();
        r.Value = q;

        entityManager.SetComponentData(entity, p);
        entityManager.SetComponentData(entity, r);

        Scale s = new Scale();
        s.Value = new Vector3(2, 2, 6);

        entityManager.SetComponentData(entity, s);


        entityManager.SetComponentData(entity, new Boid() {boidId = i, mass = 1, maxSpeed = 100, maxForce = 400, weight = 200});
        entityManager.SetComponentData(entity, new Seperation() { weight = 1 });
        entityManager.SetComponentData(entity, new Alignment() { weight = 1 });
        entityManager.SetComponentData(entity, new Cohesion() { weight = 2 });
        entityManager.SetComponentData(entity, new Constrain() { weight = 1 });
        entityManager.SetComponentData(entity, new Wander() { weight = 1, distance =2
            , radius = 1.2f, jitter = 80, target = Random.insideUnitSphere * 1.2f });

        entityManager.AddSharedComponentData(entity, renderMesh);

        return entity;
    }

    public int numBoids = 100;
    public float radius = 500;
    public float neighbourDistance = 20;

    [Range(0.0f, 2.0f)]
    public float speed = 1.0f;

    // Start is called before the first frame update
    void Start()
    {
        entityManager = World.Active.GetOrCreateManager<EntityManager>();

        cubeArchitype = entityManager.CreateArchetype(
            typeof(Position),
            typeof(Rotation),
            typeof(Scale),
            typeof(Boid),
            typeof(Seperation),
            typeof(Cohesion),
            typeof(Alignment),
            typeof(Wander),
            typeof(Constrain)
        );

        renderMesh = new RenderMesh();
        renderMesh.mesh = mesh;
        renderMesh.material = material;

        for (int i = 0; i < numBoids; i++)
        {
            Vector3 pos = Random.insideUnitSphere * radius;
            CreateBoid(transform.position + pos, Quaternion.identity, i);
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
