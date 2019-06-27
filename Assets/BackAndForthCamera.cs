using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackAndForthCamera : MonoBehaviour
{
    // Start is called before the first frame update
    public float speed = 10;
    public float maxDist = 5000;
    public float target;
    void Start()
    {
        target = -maxDist;
        transform.position = new Vector3(0, 0, target);
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Joystick1Button2))
        {
            target = -target;
        }
        Vector3 targetPos = new Vector3(0, 0, target);
        Vector3 pos = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * speed);
        transform.position = pos;
        if (transform.position.magnitude > 5)
        {
            transform.forward = Vector3.Lerp(transform.forward, -transform.position, Time.deltaTime * 0.2f);
        }
    }
}
