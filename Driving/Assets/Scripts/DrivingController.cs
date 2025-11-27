using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;

public class DrivingController : MonoBehaviour
{

    [Header("MainComponents")]
    [SerializeField] CharacterController characterController;

    [Header("Variables")]
    [SerializeField] float gravity = -9.81f;
    [SerializeField] bool is_grounded;
    [SerializeField] float acceleration = 20f;
    [SerializeField] float break_multiplier = 3.0f;
    [SerializeField] float speed = 0f;
    [SerializeField] float max_speed = 35f;
    [SerializeField] float rotate_speed = 5.0f;
    [SerializeField] Vector2 Movement;
    [SerializeField] bool is_moving => (Movement.y != 0);

    float sign = 1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        is_grounded = true;
    }

    // Update is called once per frame
    void Update()
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
            }
            //otherwise decelerate
            else
            {
                speed -= acceleration * Time.deltaTime * sign * break_multiplier;
            }
        }

        if(speed != 0)
        {
            transform.Rotate(0, Movement.x * rotate_speed, 0);
        }

        characterController.Move(transform.forward * speed * Time.deltaTime);
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
}
