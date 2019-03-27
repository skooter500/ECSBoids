using UnityEngine;
using System.Collections;

public class ForceController : MonoBehaviour {
    public Camera headCamera;
    public float power = 10.0f;

    public bool lookEnabled = true;
    public bool moveEnabled = true;
     
    Rigidbody rigidBody;

    [HideInInspector]
    public bool rotating = false;

    [HideInInspector]
    public bool attachedToCreature = false;

    public enum CameraType { free, forward };


    public float angularSpeed = 30.0f;

    public bool addFlow = true;
    public float flowForce = 1000;
    public float flowScale = 0.01f;

    public static float Map(float value, float r1, float r2, float m1, float m2)
    {
        float dist = value - r1;
        float range1 = r2 - r1;
        float range2 = m2 - m1;
        return m1 + ((dist / range1) * range2);
    }

    // Use this for initialization
    void Start()
    {
        //Cursor.lockState = CursorLockMode.Locked;
        rigidBody = GetComponent<Rigidbody>();
        rigidBody.freezeRotation = true;        

        desiredRotation = transform.rotation;
        headCamera = Camera.main;
    }

    public Quaternion desiredRotation;

    void Yaw(float angle)
    {

        Quaternion rot = Quaternion.AngleAxis(angle, Vector3.up);
        desiredRotation = rot * desiredRotation;
        rotating = true;
    }

    void Roll(float angle)
    {
        Quaternion rot = Quaternion.AngleAxis(angle, transform.forward);
        desiredRotation = rot * desiredRotation;

    }


    void Pitch(float angle)
    {

        // A pitch is a rotation around the right vector

        Vector3 right = desiredRotation* Vector3.right;
        Quaternion rot = Quaternion.AngleAxis(angle, right);
        desiredRotation = rot * desiredRotation;
        rotating = true;
    }

    void Walk(float units)
    {
        if (headCamera != null)
        {
            rigidBody.AddForce(headCamera.transform.forward* units);
        }
        else
        {
            rigidBody.AddForce(transform.forward* units);
        }
    }

    void Fly(float units)
    {
        rigidBody.AddForce(transform.up * units);     
    }

    void Strafe(float units)
    {
        if (headCamera != null)
        {
            rigidBody.AddForce(headCamera.transform.right* units);
        }
        else
        {
            rigidBody.AddForce(transform.right * units);
        }
    }
    
    void FixedUpdate()
    {

        rotating = false;
        float mouseX, mouseY;
        float contSpeed = this.power;
        float contAngularSpeed = this.angularSpeed;

        float runAxis = Input.GetAxis("Fire1");

        if (Input.GetKey(KeyCode.LeftShift) || runAxis != 0)
        {
            contSpeed *= 3f;
            contAngularSpeed *= 2.0f;
        }

        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");

        if (mouseX != 0)
        {
            Yaw(mouseX * Time.deltaTime * contAngularSpeed);
        }
        if (mouseY != 0 && !UnityEngine.XR.XRDevice.isPresent)
        {
            Pitch(-mouseY * Time.deltaTime * contAngularSpeed);
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime);

        float contWalk = Input.GetAxis("Vertical");
        float contStrafe = Input.GetAxis("Horizontal");
        if (moveEnabled)
        {
            Walk(contWalk * contSpeed * Time.deltaTime);
        }
        if (moveEnabled)
        {
            Strafe(contStrafe * contSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.Joystick1Button5))
        {
            Fly(contSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.Joystick1Button4))
        {
            Fly(-contSpeed * Time.deltaTime);
        }

        if (addFlow)
        {
            Vector3 p = transform.position;
            float n = Mathf.PerlinNoise(p.x * flowScale, p.z * flowScale);
            Vector3 force = Quaternion.AngleAxis(
                Map(n, 0, 1, -180, 180)
                , Vector3.up)
                * Vector3.forward
                * flowForce
                ;
            force += Quaternion.AngleAxis(
                Map(n, 0, 1, -180, 180)
                , Vector3.right)
                * Vector3.forward
                * flowForce
                ;

            rigidBody.AddForce(force);                
        }
    }          
}