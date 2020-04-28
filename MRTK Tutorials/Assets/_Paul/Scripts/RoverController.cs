using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoverController : MonoBehaviour
{
    [Header("Joystick refrence")]
    [SerializeField] GameObject joystick;
    [Header("Rover refrence")]
    [SerializeField] Rigidbody rigidBody;
    [Header("RoverModule refrence")]
    [SerializeField] Animator animator;
    [Header("Movement settings")]
    [Tooltip("The speed that the wheels spin")] [SerializeField] float driveSpeed = 10f;
    [Tooltip("The speed that the rover moves")] [SerializeField] float driveFactor = 10f;
    [Tooltip("The speed that the rover turns")] [SerializeField] float turnFactor = 100f;
    [Header("Parts To Assemble")]
    [Tooltip("The Rover can't move until all of these parts have been assembled")][SerializeField] List<GameObject> roverParts;
    
    Vector3 startJoystickPos;
    Vector3 oldJoystickPos;

    float movementLimit = .1f;
    bool isAtStartingPosition = false;

    PartAssemblyDemo partAssemblyDemo;
    List<Transform> transforms;

    bool canMove;

    void Start()
    {
        // get the starting position for the joystick        
        startJoystickPos = joystick.transform.localPosition;

        // insure the driving animation is not playing
        animator.SetBool("isDriving", false);
        
        StartCoroutine(CheckIfCanMove());
    }

    void Update()
    {
        // Check if parts are assembled before let the user move the rover
        if (!canMove) return;
               
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

        // if the rover is driving backwards reverse the turn direction
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

    IEnumerator CheckIfCanMove()
    {
        while (true)
        {
            yield return new WaitForSeconds(.1f);
            foreach (var part in roverParts)
            {
                if (part.transform.position != part.GetComponent<PartAssemblyDemo>().locationToPlace.position)
                {
                    continue;
                }
            }
            canMove = true;
        }
    }

    public void EndManipulation()
    {
        isAtStartingPosition = false;
    }
}
