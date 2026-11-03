using UnityEngine;
using UnityEngine.InputSystem;

public class Crosshair_shooter : MonoBehaviour
{
    public RectTransform crosshairUI;
    public Camera playerCamera;
    public ParticleSystem partikel1;
    public ParticleSystem partikel2;
    public ParticleSystem partikel3;
    public ParticleSystem partikel4;

    public int tembakaktif;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {if (tembakaktif==1)
        {
                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, crosshairUI.position);
            Ray ray = playerCamera.ScreenPointToRay(screenPos);

         if (Physics.Raycast(ray, out RaycastHit hit))
        {
            letakkanpartikel(hit.point);
            //Debug.Log("Kena: " + hit.collider.name);
        }
        }
       
    }


    public void letakkanpartikel(Vector3 pos)
    {
        if (partikel1.GetComponent<partikel>().ready == 1)
        {
            partikel1.GetComponent<partikel>().ledakan(pos);
            partikel1.transform.rotation = Quaternion.LookRotation(-playerCamera.transform.forward);
        }
        else if (partikel2.GetComponent<partikel>().ready == 1)
        {
            partikel2.GetComponent<partikel>().ledakan(pos);
            partikel2.transform.rotation = Quaternion.LookRotation(-playerCamera.transform.forward);
        }
        else if (partikel3.GetComponent<partikel>().ready == 1)
        {
            partikel3.GetComponent<partikel>().ledakan(pos);
            partikel3.transform.rotation = Quaternion.LookRotation(-playerCamera.transform.forward);
        }
        else if (partikel4.GetComponent<partikel>().ready == 1)
        {
            partikel4.GetComponent<partikel>().ledakan(pos);
            partikel4.transform.rotation = Quaternion.LookRotation(-playerCamera.transform.forward);
        }

    }
    
}
