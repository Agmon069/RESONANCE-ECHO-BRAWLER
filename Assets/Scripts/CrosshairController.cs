using UnityEngine;
using UnityEngine.InputSystem;

public class CrosshairController : MonoBehaviour
{
    [Header("Referensi")]
    [SerializeField] private RectTransform crosshair;
    [SerializeField] private RectTransform area;      // Panel/Canvas pembatas (opsional)

    [Header("Pengaturan")]
    [SerializeField] private float speed = 80f;      // piksel per detik
    [SerializeField] private float deadzone = 0.1f;
    [SerializeField] private Vector2 padding = new Vector2(20f, 20f);

    private Vector2 lookInput;
    float z, zr;
    Vector2 v;
    // public Vector2 combined => new Vector2(z, zr);


    void Reset()
    {
        crosshair = GetComponent<RectTransform>();
    }


    
    public void OnZAxis(InputAction.CallbackContext ctx)
    {
        z = ctx.canceled ? 0f : ctx.ReadValue<float>();

        v.y = z;
      //  Debug.Log($"z phase={ctx.phase} value={z} control={ctx.control?.path}");
      //  Debug.Log($"z phase={ctx.phase} value={z} control={ctx.control?.path}");
       // Debug.Log("VY :" + v.y);
    }

    public void OnZrAxis(InputAction.CallbackContext ctx)
    {
        zr = ctx.canceled ? 0f : ctx.ReadValue<float>();
        v.x = zr;
        //Debug.Log($"zr phase={ctx.phase} value={zr} control={ctx.control?.path}");
      //  Debug.Log($"zr phase={ctx.phase} value={zr} control={ctx.control?.path}");
      //  Debug.Log("VX :" + v.x);
        // Dipanggil oleh Unity Event dari PlayerInput (action: Look)

    }
    public void analogCrosshair(InputAction.CallbackContext context)
    {
       
      //  Debug.Log(" X : "+lookInput.x + "  Y:  "+ lookInput.y);
    }

    void Update()
    {
 lookInput = v;
       
        
      Debug.Log(" X : " + lookInput.x + "  Y:  " + lookInput.y);


        if (crosshair == null) return;

        // Deadzone sederhana
        Vector2 input = lookInput.magnitude < deadzone ? Vector2.zero : lookInput;

        Vector2 pos = crosshair.anchoredPosition + input * speed * Time.deltaTime;

        // Batasi posisi
        RectTransform bounds = area != null ? area : crosshair.parent as RectTransform;
        if (bounds != null)
        {
            Vector2 half = bounds.rect.size * 0.5f - padding;
            pos.x = Mathf.Clamp(pos.x, -half.x, half.x);
            pos.y = Mathf.Clamp(pos.y, -half.y, half.y);
        }

        crosshair.anchoredPosition = pos;
    }
}