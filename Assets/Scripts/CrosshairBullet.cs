using UnityEngine;

public class CrosshairBullet : MonoBehaviour
{
    public RectTransform crosshairUI;
    public Camera playerCamera;
    public ParticleSystem partikel1;
    public ParticleSystem partikel2;
    public ParticleSystem partikel3;
    public ParticleSystem partikel4;

    public bool sedangtembak;
    private float nextFireTime = 0f;
    public float delaysenjata;
    public GameObject muzzle;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sedangtembak = false;
        if (playerCamera == null) playerCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time >= nextFireTime) {

            if (sedangtembak)
            {
                Vector2 screenpos = RectTransformUtility.WorldToScreenPoint
                    (null, crosshairUI.position);
                Ray ray = playerCamera.ScreenPointToRay(screenpos);

                if (Physics.Raycast(ray, out RaycastHit hitInfo))
                { 
                //letakkan partikel (hitinfo.point)
                muzzle.GetComponent<peluru>().hadaptembakan(hitInfo.point) ;
                    Debug.DrawRay(ray.origin, ray.direction * 10, Color.red, 2f);
                    nextFireTime = Time.time+delaysenjata;
                
                }
            }
        
        
        }
    }

    public void tembakaktif()
    {
        sedangtembak=true;
    }
    public void tembakoff()
    {
        sedangtembak=false;
    }

    public void letakkanpartikel(Vector3 pos)
    {
        if (partikel1.GetComponent<partikel>().ready == 1)
        {
            partikel1.GetComponent<partikel>().ledakan(pos);
            Vector3 directiontocamera = playerCamera.transform.position - pos;
            partikel1.transform.rotation= 
                Quaternion.LookRotation(playerCamera.transform.forward *-1);
        }
        else
            if (partikel2.GetComponent<partikel>().ready == 1)
            {
                partikel2.GetComponent<partikel>().ledakan(pos);
                Vector3 directiontocamera = playerCamera.transform.position - pos;
                partikel2.transform.rotation =
                    Quaternion.LookRotation(playerCamera.transform.forward * -1);
            }
            else if (partikel3.GetComponent<partikel>().ready == 1)
            {
                partikel3.GetComponent<partikel>().ledakan(pos);
                Vector3 directiontocamera = playerCamera.transform.position - pos;
                partikel3.transform.rotation =
                    Quaternion.LookRotation(playerCamera.transform.forward * -1);
            }
            
    }
}
