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

    [SerializeField] bool moving;

    [Header("Gravity Variables")]
    [SerializeField] Transform groundCheckFront;
    [SerializeField] Transform groundCheckBack;
    [SerializeField] Transform groundCheckLeft;
    [SerializeField] Transform groundCheckRight;
    [SerializeField] bool is_grounded;
    [SerializeField] bool frontGrounded;
    [SerializeField] bool backGrounded;
    [SerializeField] LayerMask groundMask;
    [SerializeField] float groundDistance = 0.4f;
    [SerializeField] float wheelRadius = 0.5f;
    [SerializeField] float gravity = -9.81f;
    [SerializeField] float vehicleMass = 500f;
    [SerializeField] float terminalVelocity = 10f;

    [Header("Suspension Variables")]
    [SerializeField] float suspensionStrength = 5000f;

    float downwardVelocity = 0f;

    [Header("Drifting Variables")]
    [SerializeField] GameObject body;
    [SerializeField] float maxRotation = 30;
    [SerializeField] Animation driftAnimation;

    float sign = 1f;

    [Header("Manual drift variables")]
    [SerializeField] bool manualDriftAnim = true;
    [SerializeField] float manualAnimationSpeed = 1f;

    private Vector3 hitPosition;

    private Quaternion currentTargetRotation;
    private float rotationTimeAcc = 0f; //rotation time accumulator
    [Tooltip("time it takes for a rotation of the vehicle to occur")]
    [SerializeField] private float maxRotationTime = 1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        is_grounded = false;
        frontGrounded = false;
        backGrounded = false;
    }

    // Update is called once per frame
    void Update()
    {
        GroundCheck(groundCheckFront, ref frontGrounded);
        GroundCheck(groundCheckBack, ref backGrounded);

        is_grounded = frontGrounded || backGrounded;

        ApplyGravity();

        updateMove();
        updateRotate();

        RotateToFloor();

        moving = is_moving;
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
        if (is_moving && is_grounded)
        {
            //if current speed is maxxed out
            if (Mathf.Abs(speed) >= max_speed)
            {
                speed = sign * max_speed;
            }
            else
            {
                speed += acceleration * Time.deltaTime * sign * ((Mathf.Sign(speed) != sign) ? brake_multiplier : 1);
            }
        }
        //if triggers not held, decelerate
        else if (is_grounded)
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
        if (is_grounded)
        {
            Movement.y = inputvalue.y;

            if (Movement.y != 0)
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

        Gizmos.DrawSphere(groundCheckFront.transform.position - new Vector3(0, groundDistance + wheelRadius, 0), 0.1f);
        Gizmos.DrawSphere(groundCheckBack.transform.position - new Vector3(0, groundDistance + wheelRadius, 0), 0.1f);
    }

    public void ApplyGravity()
    {
        if (is_grounded == false)
        {
            downwardVelocity += downwardVelocity < terminalVelocity ? gravity * Time.deltaTime : 0f;
            characterController.Move(Vector3.up * downwardVelocity * vehicleMass * Time.deltaTime);

            brake();
        }
        else
        {
            //characterController.Move(new Vector3(0, (groundDistance + wheelRadius) - Vector3.Distance(groundCheckFront.position, hitPosition), 0));

            downwardVelocity = 0f;
        }
    }

    public void GroundCheck(Transform currentPosition, ref bool groundCheck)
    {
        RaycastHit hit;

        float rayLength = groundDistance + wheelRadius;

        if (Physics.Raycast(currentPosition.position, -currentPosition.up, out hit, rayLength, groundMask))
        {
            Debug.DrawRay(currentPosition.position, -currentPosition.up * rayLength, Color.green);
            groundCheck = true;
        }
        else
        {
            Debug.DrawRay(currentPosition.position, -currentPosition.up * rayLength, Color.red);
            groundCheck = false;
        }
    }

    struct rotationVariables
    {
        public rotationVariables(Transform groundCheck, RaycastHit hit, Vector3 axis, float rayLength)
        {
            this.groundCheck = groundCheck;
            this.hit = hit;
            this.axis = axis;
            this.rayLength = rayLength;
        }

        public Transform groundCheck { get; private set; }
        public RaycastHit hit { get; private set; }
        public Vector3 axis { get; private set; }
        public float rayLength { get; private set; }
    }

    public void RotateToFloor()
    {
        RaycastHit hit;

        hitPosition = new();

        float rayLength = groundDistance + wheelRadius;

        rotationVariables[] rotationQueue = new rotationVariables[4];

        //push the front up
        if (Physics.Raycast(groundCheckFront.position, -groundCheckFront.up, out hit, rayLength, groundMask))
        {
            hitPosition = hit.point;

            rotationQueue[0] = new(groundCheckBack, hit, -transform.right, rayLength);
        }
        else
        {
            transform.RotateAround(groundCheckBack.transform.position - new Vector3(0, rayLength, 0), transform.right, 0.3f);
        }

        //push the back up
        if (Physics.Raycast(groundCheckBack.position, -groundCheckBack.up, out hit, rayLength, groundMask))
        {
            hitPosition = hit.point;

            rotationQueue[1] = new(groundCheckFront, hit, transform.right, rayLength);
        }
        else
        {
            transform.RotateAround(groundCheckFront.transform.position - new Vector3(0, rayLength, 0), -transform.right, 0.3f);
        }

        //push the left up
        if (Physics.Raycast(groundCheckLeft.position, -groundCheckLeft.up, out hit, rayLength, groundMask))
        {
            hitPosition = hit.point;

            rotationQueue[2] = new(groundCheckBack, hit, -transform.right, rayLength);
        }
        else
        {
            transform.RotateAround(groundCheckRight.transform.position - new Vector3(0, rayLength, 0), transform.forward, 0.3f);
        }

        //push the right up
        if (Physics.Raycast(groundCheckRight.position, -groundCheckRight.up, out hit, rayLength, groundMask))
        {
            hitPosition = hit.point;

            rotationQueue[1] = new(groundCheckLeft, hit, transform.right, rayLength);
        }
        else
        {
            transform.RotateAround(groundCheckLeft.transform.position - new Vector3(0, rayLength, 0), -transform.forward, 0.3f);
        }

        foreach (rotationVariables args in rotationQueue)
        {
            if (args.hit.transform == null) continue;
            rotateVehicleAround(args);
        }
    }

    private void rotateVehicleAround(rotationVariables vars)
    {
        rotateVehicleAround(vars.groundCheck, vars.hit, vars.axis, vars.rayLength);
    }

    //Rotate the vehicle to the correct rotation of the surface
    private void rotateVehicleAround(Transform groundCheck, RaycastHit hit, Vector3 axis, float rayLength)
    {
        //amount of the ray that is under the surface
        float hiddenRayHeight = rayLength - hit.distance;
        float vehicleLength = transform.lossyScale.z;
        float rotationAngle = Mathf.Atan(hiddenRayHeight / vehicleLength);

        transform.RotateAround(groundCheck.transform.position - new Vector3(0, rayLength, 0), axis, rotationAngle);
    }

    void OnCollisionEnter(Collision collision) { }
}
