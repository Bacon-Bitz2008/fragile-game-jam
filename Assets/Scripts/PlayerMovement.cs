using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{

    // lots of the input system stuff came from last year's tutorial
    private Controls controls;
    private Rigidbody rb;

    [SerializeField] GameObject ball;
    [SerializeField] int launchSpeed;

    [SerializeField] private int speed;

    [SerializeField] private bool isP1;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controls = new Controls();

        if (isP1)
        {
            controls.Player1.Enable();
            controls.Player1.Shoot.performed += Shoot_performed;
        }
        else
        {
            controls.Player2.Enable();
            controls.Player2.Shoot.performed += Shoot_performed;
        }

        rb = GetComponent<Rigidbody>();

    }


    // Update is called once per frame
    void Update()
    {

        Vector2 walkInput;

        if (isP1)
        {
            walkInput = controls.Player1.Move.ReadValue<Vector2>();
        }
        else
        {
            walkInput = controls.Player2.Move.ReadValue<Vector2>();
        }

        Vector3 walkVector = new Vector3(walkInput.x, 0, walkInput.y);
        transform.Translate(walkVector * speed * Time.deltaTime, Space.World);

        if(walkVector != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(walkVector);

        }

    }

    private void Shoot_performed(InputAction.CallbackContext context)
    {
        GameObject newBall = Instantiate(ball);
        newBall.transform.position = transform.position;
        Rigidbody brb = newBall.GetComponent<Rigidbody>();

        brb.AddForce(transform.forward * launchSpeed, ForceMode.Impulse);
    }
}
