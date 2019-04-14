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

public struct Head : IComponentData
{
    public float theta;
    public int boidId;
}

public struct Tail : IComponentData
{
    public float theta;
    public int boidId;
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
    private EntityArchetype boidArchitype;
    private EntityArchetype headArchitype;
    private EntityArchetype tailArchitype;
    private EntityManager entityManager;
    
    private RenderMesh bodyMesh;
    public Mesh mesh;
    public Material material;

    public float seperationWeight = 1.0f;
    public float cohesionWeight = 2.0f;
    public float alignmentWeight = 1.0f;
    public float wanderWeight = 1.0f;
    public float constrainWeight = 1.0f;

    public float fleeWeight = 1.0f;
    public float fleeDistance = 50;

    public float headAmplitude = 20;
    public float tailAmplitude = 30;
    public float animationFrequency = 1;

    public int totalNeighbours = 50;

    Entity CreateBoid(Vector3 pos, Quaternion q, int i, float size)
    {
        Entity boidEntity = entityManager.CreateEntity(boidArchitype);

        Position p = new Position();
        p.Value = pos;

        Rotation r = new Rotation();
        r.Value = q;

        entityManager.SetComponentData(boidEntity, p);
        entityManager.SetComponentData(boidEntity, r);

        Scale s = new Scale();
        s.Value = new Vector3(size * 0.5f, size, size);
        //s.Value = new Vector3(2, 4, 10);

        entityManager.SetComponentData(boidEntity, s);


        entityManager.SetComponentData(boidEntity, new Boid() {boidId = i, mass = 1, maxSpeed = 100, maxForce = 400, weight = 200});
        entityManager.SetComponentData(boidEntity, new Seperation() );
        entityManager.SetComponentData(boidEntity, new Alignment() );
        entityManager.SetComponentData(boidEntity, new Cohesion() );
        entityManager.SetComponentData(boidEntity, new Constrain());
        entityManager.SetComponentData(boidEntity, new Flee());
        entityManager.SetComponentData(boidEntity, new Wander() { distance =2
            , radius = 1.2f, jitter = 80, target = Random.insideUnitSphere * 1.2f });

        entityManager.AddSharedComponentData(boidEntity, bodyMesh);

        // Make the head
        Entity headEntity = entityManager.CreateEntity(headArchitype);

        Position headPosition = new Position();
        headPosition.Value = pos + (q * Vector3.forward) * size;
        entityManager.SetComponentData(headEntity, headPosition);
        Rotation headRotation = new Rotation();
        headRotation.Value = q;
        entityManager.SetComponentData(headEntity, headRotation);
        entityManager.AddSharedComponentData(headEntity, bodyMesh);
        entityManager.SetComponentData(headEntity, s);

        entityManager.SetComponentData(headEntity, new Head() { boidId = i});
        // End head

        // Make the tail
        Entity tailEntity = entityManager.CreateEntity(tailArchitype);
        Position tailPosition = new Position();
        tailPosition.Value = pos - (q * Vector3.forward) * size;
        entityManager.SetComponentData(tailEntity, tailPosition);
        Rotation tailRotation = new Rotation();
        tailRotation.Value = q;
        entityManager.SetComponentData(tailEntity, tailRotation);
        entityManager.AddSharedComponentData(tailEntity, bodyMesh);
        entityManager.SetComponentData(tailEntity, s);
        entityManager.SetComponentData(tailEntity, new Tail() { boidId = i });
        // End tail

        return boidEntity;
    }

    public int numBoids = 100;
    public float radius = 2000;
    public float neighbourDistance = 20;

    [Range(0.0f, 10.0f)]
    public float speed = 1.0f;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.visible = false;
        entityManager = World.Active.GetOrCreateManager<EntityManager>();

        boidArchitype = entityManager.CreateArchetype(
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

        headArchitype = entityManager.CreateArchetype(
            typeof(Position),
            typeof(Rotation),
            typeof(Scale),
            typeof(Head)
            );

        tailArchitype = entityManager.CreateArchetype(
                    typeof(Position),
                    typeof(Rotation),
                    typeof(Scale),
                    typeof(Tail)
                    );


        bodyMesh = new RenderMesh();
        bodyMesh.mesh = mesh;
        bodyMesh.material = material;

        for (int i = 0; i < numBoids; i++)
        {
            Vector3 pos = Random.insideUnitSphere * radius;
            CreateBoid(transform.position + pos, Quaternion.identity, i, size);
        }

        //Cursor.visible = false;
        //Cursor.lockState = CursorLockMode.Locked;
    }

    public float size = 3.0f;

    public int cellSize = 50;
    public int gridSize = 10000;
    public bool usePartitioning = true;

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
        Explosion();
    }

    float ellapsed = 1000;
    public float toPass = 0.3f;
    public int clickCount = 0;


    void Explosion()
    {
        if (Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.J))
        {
            clickCount = (clickCount + 1) % 4;
            ellapsed = 0;
        }
        ellapsed += Time.deltaTime;
        
        if (ellapsed > toPass && clickCount > 0)
        {
            Debug.Log(clickCount);
            switch (clickCount)
            {
                case 1:
                    radius = 10;
                    neighbourDistance = 100;
                    break;
                case 2:
                    radius = 4000;
                    cohesionWeight = 0;
                    neighbourDistance = 100;
                    break;
                case 3:
                    radius = 4000;
                    neighbourDistance = 150;
                    cohesionWeight = 2;
                    break;
            }
            clickCount = 0;
        }
    }    
}
