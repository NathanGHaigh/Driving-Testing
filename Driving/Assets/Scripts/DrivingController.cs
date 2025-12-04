using Unity.Burst.Intrinsics;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;


public class DrivingController : MonoBehaviour
{

    [Header("MainComponents")]
    [SerializeField] CharacterController characterController;

    [Header("Variables")]
    [SerializeField] float acceleration = 20f;
    [SerializeField] float brake_multiplier = 3.0f;
    [SerializeField] float speed = 0f;
    [SerializeField] float max_speed = 35f;
    [SerializeField] float rotate_speed = 5.0f;
    [SerializeField] Vector2 Movement;
    [SerializeField] bool is_moving => (Movement.y != 0);

    [SerializeField] bool grounded;

    [Header("Gravity Variables")]
    [SerializeField] Transform groundCheckFront;
    [SerializeField] Transform groundCheckBack;
    [SerializeField] Transform groundCheckLeft;
    [SerializeField] Transform groundCheckRight;
    [SerializeField] LayerMask groundMask;
    [SerializeField] float groundDistance = 0.4f;
    [SerializeField] float wheelRadius = 0.5f;
    [SerializeField] float gravity = -9.81f;
    [SerializeField] float vehicleMass = 500f;
    [SerializeField] float terminalVelocity = 10f;

    float downwardVelocity = 0f;

    [Header("Drifting Variables")]
    [SerializeField] GameObject body;
    [SerializeField] float maxRotation = 30;
    [SerializeField] Animation driftAnimation;

    float sign = 1f;

    [Header("Manual drift variables")]
    [SerializeField] bool manualDriftAnim = true;
    [SerializeField] float manualAnimationSpeed = 1f;

    // Update is called once per frame
    void Update()
    {

        ApplyGravity();

        updateMove();
        updateRotate();

        grounded = characterController.isGrounded;
    }

    private void brake(float multiplier = 1f)
    {
        float speed_recuction = acceleration * Time.deltaTime * multiplier;

        //if speed is around 0 then stop
        if (Mathf.Abs(speed) <= speed_recuction)
        {
            speed = 0;

            Movement.x = 0;
        }
        //otherwise decelerate
        else
        {
            speed -= speed_recuction * sign;
        }
    }

    private void updateMove()
    {
        //if triggers held and the vehicle is on the floor, then move
        if (is_moving)
        {
            //if current speed is maxxed out
            if (Mathf.Abs(speed) >= max_speed)
            {
                speed = sign * max_speed;
            }
            else
            {
                speed += acceleration * Time.deltaTime * sign;
            }

            Debug.Log(speed);
        }
        //if triggers not held, decelerate
        else
        {
            brake(brake_multiplier);
        }

        characterController.Move(transform.forward * speed * Time.deltaTime);
    }

    private void updateRotate()
    {

        //if the player isn't moving or trying to turn then return
        if (speed == 0 || Movement.x == 0)
        {
            body.transform.localRotation = new(0, 0, 0, 0);
            body.transform.localPosition = new(0, 0, 0);

            return;
        }

        transform.Rotate(0, Movement.x * rotate_speed * Time.deltaTime, 0);

        float bodyAngle = Mathf.Ceil(body.transform.localEulerAngles.y - 360f * Mathf.Floor(body.transform.localEulerAngles.y / 180f));

        //If the body is fully rotated, then return
        if (Mathf.Abs(bodyAngle) >= maxRotation)
        {
            return;
        }

        //If there is no animation added for the drift, then do a basic, manual animation
        if (manualDriftAnim)
        {
            body.transform.RotateAround(
                body.transform.position + body.transform.forward * body.transform.localScale.z / 2f,
                transform.up,
                Movement.x * manualAnimationSpeed);
        }
        else
        {
            if (driftAnimation != null)
            {
                driftAnimation.Play();
            }

            body.transform.RotateAround(
                body.transform.position + body.transform.forward * body.transform.localScale.z / 2f,
                transform.up,
                Movement.x * maxRotation);
        }
    }

    public void OnMove(InputValue value)
    {
        var inputvalue = value.Get<Vector2>();

        Movement.y = inputvalue.y;

        if (Movement.y != 0)
        {
            sign = Mathf.Sign(Movement.y);
        }
    }

    public void OnTurn(InputValue value)
    {
        var inputvalue = value.Get<Vector2>();
        if (is_moving)
        {
            Movement.x = inputvalue.x;
        }
    }

    public void ApplyGravity()
    {
        if (!characterController.isGrounded)
        {
            downwardVelocity += downwardVelocity < terminalVelocity ? gravity * Time.deltaTime : 0f;
            characterController.Move(Vector3.up * downwardVelocity * vehicleMass * Time.deltaTime);
        }
        else
        {
            downwardVelocity = 0f;
        }
    }

}
