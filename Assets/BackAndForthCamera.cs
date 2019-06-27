using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackAndForthCamera : MonoBehaviour
{
    // Start is called before the first frame update
    public float speed = 10;
    public float maxDist = 5000;
    public float target;
    public Vector3 targetPos;
    void Start()
    {
        transform.position = new Vector3(100, 0, maxDist);
        targetPos = transform.position;
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Joystick1Button3))
        {

            targetPos = Random.insideUnitSphere * Random.Range(2000, maxDist);     
        }
        //Vector3 pos = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * speed);

        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, 3.0f, speed) ;

        //transform.position = pos;
        //transform.forward = Vector3.Lerp(transform.forward, -transform.position, Time.deltaTime * 0.2f);
        transform.forward = -transform.position;
    }

    Vector3 velocity = Vector3.zero;

}
