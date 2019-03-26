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

    public Vector3 fleeForce; // Have to put this here because there is a limit to the number of components in IJobProcessComponentData
}

public struct Flee : IComponentData
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

public class Bootstrap : MonoBehaviour
{
    private EntityArchetype cubeArchitype;
    private EntityManager entityManager;
    
    private RenderMesh renderMesh;
    public Mesh mesh;
    public Material material;

    public float seperationWeight = 1.0f;
    public float cohesionWeight = 2.0f;
    public float alignmentWeight = 1.0f;
    public float wanderWeight = 1.0f;
    public float constrainWeight = 1.0f;

    public float fleeWeight = 1.0f;
    public float fleeDistance = 50;

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
        s.Value = new Vector3(2, 4, 10);

        entityManager.SetComponentData(entity, s);


        entityManager.SetComponentData(entity, new Boid() {boidId = i, mass = 1, maxSpeed = 100, maxForce = 400, weight = 200});
        entityManager.SetComponentData(entity, new Seperation() );
        entityManager.SetComponentData(entity, new Alignment() );
        entityManager.SetComponentData(entity, new Cohesion() );
        entityManager.SetComponentData(entity, new Constrain());
        entityManager.SetComponentData(entity, new Flee());
        entityManager.SetComponentData(entity, new Wander() { distance =2
            , radius = 1.2f, jitter = 80, target = Random.insideUnitSphere * 1.2f });

        entityManager.AddSharedComponentData(entity, renderMesh);

        return entity;
    }

    public int numBoids = 100;
    public float radius = 500;
    public float neighbourDistance = 20;

    [Range(0.0f, 10.0f)]
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
            typeof(Constrain),
            typeof(Flee)
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
    public void Update()
    {
        if (Input.GetKey(KeyCode.Joystick1Button2))
        {
            speed -= Time.deltaTime;
            if (speed < 0)
            {
                speed = 0;
            }
        }

        if (Input.GetKey(KeyCode.Joystick1Button1))
        {
            speed += Time.deltaTime;
            if (speed > 2)
            {
                speed = 2;
            }
        }
        if (Input.GetKey(KeyCode.Joystick1Button0))
        {
            StartCoroutine(Explosion());
        }
    }

    IEnumerator Explosion()
    {
        radius = 10;
        yield return new WaitForSeconds(10);
        radius = 1000;
        cohesionWeight = 0;
        neighbourDistance = 0;
        yield return new WaitForSeconds(10);
        cohesionWeight = 2;
        neighbourDistance = 50;
    }

}
