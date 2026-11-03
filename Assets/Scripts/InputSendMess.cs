using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(CharacterController))]

public class InputSendMess: MonoBehaviour
{
    [Header("Pengaturan Lompatan")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.3f;
    [SerializeField] private LayerMask groundMask;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    public void OnAksi()
    {
        Debug.Log(" aksi 2 send message ");

    }

    // Method ini dipanggil otomatis oleh PlayerInput (Send Messages)
    // ketika Action dengan nama "Aksi" di-trigger
  //  public void OnAksi( )
   // {
   //     if ( isGrounded)
   //     {
    //        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    //        Debug.Log(" loncat    ");
    //    }
    //    Debug.Log(" aksi 2 ");
   //  }

    public void Invoke_Aksi()
    {
        //if (isGrounded)
      // {
          velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
          Debug.Log(" loncat    ");
    // }
    Debug.Log(" aksi 2 invoke ");
     }



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }


    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // Cek apakah karakter menyentuh tanah
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // sedikit nilai negatif agar tetap menempel di tanah
        }

        // Terapkan gravitasi
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

   
}