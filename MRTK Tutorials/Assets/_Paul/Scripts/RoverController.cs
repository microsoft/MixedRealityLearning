using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoverController : MonoBehaviour
{
    [SerializeField] float driveSpeed = 10f;
    [SerializeField] float driveFactor = 1f;
    [SerializeField] float turnFactor = 100f;
    [SerializeField] float movementLimit = .5f;
    [SerializeField] GameObject joystick;
    [SerializeField] Rigidbody rigidBody;
    [SerializeField] Animator animator;

    bool isAtStartingPosition = false;
    Vector3 startJoystickPos;
    Vector3 oldJoystickPos;

    void Start()
    {
        startJoystickPos = joystick.transform.localPosition;
        //rigidBody = GetComponent<Rigidbody>();
        //animator = GetComponent<Animator>();
        animator.SetBool("isDriving", false);
    }
    
    void Update()
    {
        
        //joystick.transform.localPosition = new Vector3(joystick.transform.localPosition.x, 0, joystick.transform.localPosition.z);
        if (!isAtStartingPosition)
        {
            joystick.transform.localPosition = Vector3.Lerp(joystick.transform.localPosition, startJoystickPos, .1f);
            if (joystick.transform.position == startJoystickPos)
            {
                isAtStartingPosition = true;
            }
        }
        if (joystick.transform.localPosition.y != startJoystickPos.y)
        {
            joystick.transform.localPosition = new Vector3(joystick.transform.localPosition.x, startJoystickPos.y, joystick.transform.localPosition.z);
        }
        if (Vector3.Distance(transform.position, joystick.transform.position) > movementLimit)
        {
            joystick.transform.localPosition = oldJoystickPos;
        }

        oldJoystickPos = joystick.transform.localPosition;
        Debug.Log("oldjoystickPos " + oldJoystickPos);
        float translation = oldJoystickPos.z * 10f;
        float rotation = (oldJoystickPos.x * 10f) * turnFactor;
        Debug.Log("translation = " + translation);
        animator.SetFloat("DrivingSpeed", translation * driveSpeed);
        translation *= Time.deltaTime;
        rotation *= Time.deltaTime;
        Quaternion turn = Quaternion.Euler(0, rotation, 0);
        if (translation < 0)
        {
            turn = Quaternion.Euler(0, -rotation, 0);
        }

        rigidBody.MoveRotation(rigidBody.rotation * turn);
        rigidBody.transform.Translate(Vector3.forward * translation);
        animator.SetFloat("TurnFactor", rotation);

        if (translation != 0)
        {
            animator.SetBool("isDriving", true);         
        }
        else
        {
            animator.SetBool("isDriving", false);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, movementLimit);
    }    

    public void EndManipulation()
    {
        isAtStartingPosition = false;        
    }
}
