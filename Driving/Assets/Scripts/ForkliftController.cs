using System;
using System.Collections.Specialized;
using System.Security.AccessControl;
using System.Security.Cryptography;
using UnityEngine;
public class ForkliftController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody forkRB;
    [SerializeField] private Transform[] rayPoints;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform accelerationPoint;

    [Header("Suspension Settings")]
    [SerializeField] private float restLength;
    [SerializeField] private float damperStiffness;
    [SerializeField] private float springStiffness;
    [SerializeField] private float springTravel;
    [SerializeField] private float wheelRadius;

    private int[] wheelsIsGrounded = new int[4];
    private bool isGrounded = false;

    [Header("Input")]
    private float moveInput = 0;
    private float turnInput = 0;

    [Header("Car Settings")]
    [SerializeField] public float maxSpeed = 10f;
    [SerializeField] public float acceleration = 5f;
    [SerializeField] public float deceleration = 5f;

    private Vector3 currentLocalVelocity = Vector3.zero;
    private float carVelocityRatio = 0f;    

    #region Unity functions
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Start()
    {
        forkRB = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Suspension();
        GroundCheck();
        CalcuclateVelocity();
        Movement();
    }

    private void Update()
    {
        GetInput();
    }
    #endregion

    #region Movement

    private void Movement()
    {
        if(isGrounded)
        {
            Acceleration();
            Deceleration();
        }
    }

    private void Acceleration()
    {
        forkRB.AddForceAtPosition(acceleration * moveInput * transform.forward, accelerationPoint.position, ForceMode.Acceleration);
    }

    private void Deceleration()
    {
        forkRB.AddForceAtPosition(deceleration * moveInput * -transform.forward, accelerationPoint.position, ForceMode.Acceleration);
    }
    #endregion
    #region Forklift Status Checks

    private void GroundCheck()
    {
        int tempGroundedWheels = 0;

        for (int i = 0; i < wheelsIsGrounded.Length; i++)
        {
            tempGroundedWheels += wheelsIsGrounded[i];
        }

        if (tempGroundedWheels > 1)
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    private void CalcuclateVelocity()
    {
        currentLocalVelocity = transform.InverseTransformDirection(forkRB.linearVelocity);
        carVelocityRatio = currentLocalVelocity.z / maxSpeed;
    }
    #endregion

    #region Input Handling

    private void GetInput()
    {
        Debug.Log("Getting Input");
        moveInput = Input.GetAxis("Vertical");
        turnInput = Input.GetAxis("Horizontal");
    }

    #endregion

    #region Suspension System

    private void Suspension()
    {
        for (int i = 0; i < rayPoints.Length; i++)
        {
            RaycastHit hit;
            float maxLength = restLength + springTravel;

            if (Physics.Raycast(rayPoints[i].position, -rayPoints[i].up, out hit, maxLength + wheelRadius, groundLayer))
            {
                wheelsIsGrounded[i] = 1;

                float currentSpringLength = hit.distance - wheelRadius;
                float springCompression = (restLength - currentSpringLength) / springTravel;

                float springVelocity = Vector3.Dot(forkRB.GetPointVelocity(rayPoints[i].position), rayPoints[i].up);
                float damperForce = damperStiffness * springVelocity;

                float springForce = springStiffness * springCompression;

                float netForce = springForce - damperForce; 

                forkRB.AddForceAtPosition(netForce * rayPoints[i].up, rayPoints[i].position);

                UnityEngine.Debug.DrawLine(rayPoints[i].position, hit.point, Color.red);
                Debug.Log("Grounded");
            }
            else
            {
                wheelsIsGrounded[i] = 0;

                UnityEngine.Debug.DrawLine(rayPoints[i].position, rayPoints[i].position + (wheelRadius + maxLength) * -rayPoints[i].up, Color.green);
            }
        }
    }
    #endregion  
}
