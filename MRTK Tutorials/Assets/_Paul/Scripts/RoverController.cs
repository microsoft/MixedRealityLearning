using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoverController : MonoBehaviour
{
    [Header("Joystick refrence")]
    [SerializeField] GameObject joystick;
    [Header("Rover refrences")]
    [SerializeField] Rigidbody rigidBody;
    [SerializeField] Animator animator;
    [Header("Movement settings")]
    [Tooltip("The speed that the wheels spin")] [SerializeField] float driveSpeed = 10f;
    [Tooltip("The speed that the rover moves")] [SerializeField] float driveFactor = 10f;
    [Tooltip("The speed that the rover turns")] [SerializeField] float turnFactor = 100f;
    
    Vector3 startJoystickPos;
    Vector3 oldJoystickPos;

    float movementLimit = .1f;
    bool isAtStartingPosition = false;

    void Start()
    {
        // get the starting position for the joystick
        startJoystickPos = joystick.transform.localPosition;
        animator.SetBool("isDriving", false);
    }

    void Update()
    {
        // move joystick back to original position after manipulation has ended
        if (!isAtStartingPosition)
        {
            joystick.transform.localPosition = Vector3.Lerp(joystick.transform.localPosition, startJoystickPos, .1f);
            if (joystick.transform.position == startJoystickPos)
            {
                isAtStartingPosition = true;
            }
        }
        // lock the position of the joystick on the y axis
        if (joystick.transform.localPosition.y != startJoystickPos.y)
        {
            joystick.transform.localPosition = new Vector3(joystick.transform.localPosition.x, startJoystickPos.y, joystick.transform.localPosition.z);
        }
        // keep the joystick from moving further than the movementLimit
        if (Vector3.Distance(transform.position, joystick.transform.position) > movementLimit)
        {
            joystick.transform.localPosition = oldJoystickPos;
        }
        // track the last position of the joystick
        oldJoystickPos = joystick.transform.localPosition;

        // move the rover based of the position of the joystick
        float translation = oldJoystickPos.z * 10f;
        float rotation = (oldJoystickPos.x * 10f) * turnFactor;
        translation *= Time.deltaTime;
        rotation *= Time.deltaTime;
        Quaternion turn = Quaternion.Euler(0, rotation, 0);
        animator.SetFloat("TurnFactor", rotation);

        // if the rover is driving in backwards reverse the turn direction
        if (translation < 0)
        {
            turn = Quaternion.Euler(0, -rotation, 0);
        }

        rigidBody.MoveRotation(rigidBody.rotation * turn);
        rigidBody.transform.Translate(Vector3.forward * translation);
        animator.SetFloat("DrivingSpeed", translation * driveSpeed);

        // start the driving animation if we are moving
        if (translation != 0)
        {
            animator.SetBool("isDriving", true);
        }
        else
        {
            animator.SetBool("isDriving", false);
        }
    }

    public void EndManipulation()
    {
        isAtStartingPosition = false;
    }
}
