using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class Mouseshoot : MonoBehaviour
{
    public Camera cam;
    public ParticleSystem partikel1;
    public ParticleSystem partikel2;
    public ParticleSystem partikel3; 
    public ParticleSystem partikel4;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (cam == null) cam = Camera.main;
      
    }

    // Update is called once per frame
    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // skip clicks on UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                letakkanpartikel(hit.point);
             //   Debug.Log("Kena objek: " + hit.collider.name + " di posisi " + hit.point);
               // Debug.DrawRay(ray.origin, ray.direction * 100, Color.red, 2f);
            }
        }
    }

    void letakkanpartikel(Vector3 pos)
    {
        if (partikel1.GetComponent<partikel>().ready == 1)
        {
            partikel1.GetComponent<partikel>().ledakan(pos);
            partikel1.transform.rotation = Quaternion.LookRotation(-cam.transform.forward);
        }
        else if (partikel2.GetComponent<partikel>().ready == 1)
        {
            partikel2.GetComponent<partikel>().ledakan(pos);
            partikel2.transform.rotation = Quaternion.LookRotation(-cam.transform.forward);
        }
        else if (partikel3.GetComponent<partikel>().ready == 1)
        {
            partikel3.GetComponent<partikel>().ledakan(pos);
            partikel3.transform.rotation = Quaternion.LookRotation(-cam.transform.forward);
        }
        else if (partikel4.GetComponent<partikel>().ready == 1)
        {
            partikel4.GetComponent<partikel>().ledakan(pos);
            partikel4.transform.rotation = Quaternion.LookRotation(-cam.transform.forward);
        }

    }


}
