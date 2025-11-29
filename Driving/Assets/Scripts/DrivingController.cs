using System.ComponentModel;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;

public class DrivingController : MonoBehaviour
{

    [Header("MainComponents")]
    [SerializeField] CharacterController characterController;

    [Header("Variables")]   
    [SerializeField] float acceleration = 20f;
    [SerializeField] float break_multiplier = 3.0f;
    [SerializeField] float speed = 0f;
    [SerializeField] float max_speed = 35f;
    [SerializeField] float rotate_speed = 5.0f;
    [SerializeField] Vector2 Movement;
    [SerializeField] bool is_moving => (Movement.y != 0);

    [Header("Gravity Variables")]
    [SerializeField] Transform groundCheck;
    [SerializeField] bool is_grounded;
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        is_grounded = true;
    }

    // Update is called once per frame
    void Update()
    {
        GroundCheck();  
        ApplyGravity();

        updateMove();
        updateRotate();
    }

    private void updateMove()
    {
        //if triggers held
        if (is_moving)
        {
            //if current speed is maxxed out
            if (Mathf.Abs(speed) >= max_speed)
            {
                speed = sign * max_speed;
            }
            else
            {
                speed += acceleration * Time.deltaTime * sign * ((Mathf.Sign(speed) != sign) ? break_multiplier : 1);
            }
        }
        //if triggers not held
        else
        {
            //if speed is around 0 then stop
            if (Mathf.Abs(speed) <= acceleration * Time.deltaTime * break_multiplier)
            {
                speed = 0;

                Movement.x = 0;
            }
            //otherwise decelerate
            else
            {
                speed -= acceleration * Time.deltaTime * sign * break_multiplier;
            }
        }

        characterController.Move(transform.forward * speed * Time.deltaTime);
    }

    private void updateRotate()
    {

        //if the player isn't moving or trying to turn then return
        if(speed == 0 || Movement.x == 0)
        {
            body.transform.localRotation = new(0,0,0,0);
            body.transform.localPosition = new(0,0,0);

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
        if(manualDriftAnim)
        {
            body.transform.RotateAround(
                body.transform.position + body.transform.forward * body.transform.localScale.z / 2f,
                Vector3.up,
                Movement.x* manualAnimationSpeed);
        }
        else
        {
            if(driftAnimation != null)
            {
                driftAnimation.Play();
            }

            body.transform.RotateAround(
                body.transform.position + body.transform.forward * body.transform.localScale.z / 2f,
                Vector3.up,
                Movement.x * maxRotation);
        }
    }

    public void OnMove(InputValue value)
    {
        var inputvalue = value.Get<Vector2>();
        if (is_grounded)
        {
            Movement.y = inputvalue.y;

            if(Movement.y != 0)
            {
                sign = Mathf.Sign(Movement.y);
            }

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

    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.lightBlue;

        Gizmos.DrawSphere(transform.position + transform.forward * body.transform.localScale.z / 2f, 0.5f);
    }

    public void ApplyGravity()
    {
        if (is_grounded == false)
        {
            downwardVelocity += downwardVelocity < terminalVelocity ? gravity * Time.deltaTime : 0f;
            characterController.Move(Vector3.up * downwardVelocity * vehicleMass * Time.deltaTime);
        }
        else
        {
            transform.Translate(0, wheelRadius + groundDistance,0);

            downwardVelocity = 0f;
        }
    }
    public void GroundCheck()
    {
        RaycastHit hit; 
        float rayLength = groundDistance + wheelRadius;
        if(Physics.Raycast(groundCheck.position, -groundCheck.up, out hit, rayLength, groundMask))
        {
            Debug.DrawRay(groundCheck.position, -groundCheck.up * rayLength, Color.green);
            is_grounded = true;
        }
        else
        {
            Debug.DrawRay(groundCheck.position, -groundCheck.up * rayLength, Color.red);
            is_grounded = false;
        }
    }
}
