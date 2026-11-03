using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Pengaturan Gerakan")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.3f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private Animator animator;
    private CharacterController controller;
    private Vector2 moveInput;
    private Vector3 velocity;
    private bool isGrounded;
    public GameObject cross;
    private static readonly int aksi = Animator.StringToHash("aksi1");
    [SerializeField] private float jumpHeight = 2f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
       
        if (animator == null)
            animator = GetComponent<Animator>();

    }

    private void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Arah gerak absolut (sumbu dunia), TIDAK ditransformasi oleh rotasi karakter
        Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y);

        controller.Move(move * moveSpeed * Time.deltaTime);

        // Rotasi hanya untuk visual, tidak memengaruhi perhitungan arah gerak di atas
        if (move.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // Method ini di-assign manual ke event "Move" pada PlayerInput (Invoke Unity Events)
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    public void OnAksi(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            bool isAksi = stateInfo.IsName("hit");
            


            if (!isAksi) 
            {
                Debug.Log(" stopaksi");
                animator.SetTrigger(aksi); 
            
            } else { Debug.Log(" sedang aksi "); }
        }
    }

    



    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
           
        }
    }


    public void OnTembak(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            // cross.GetComponent<Crosshair_shooter>().tembakaktif =1;
            cross.GetComponent<CrosshairBullet>().tembakaktif();
        }
        if (!context.performed)
        {
            //  cross.GetComponent<Crosshair_shooter>().tembakaktif = 0;
            cross.GetComponent<CrosshairBullet>().tembakoff();
        }
    }

    
}