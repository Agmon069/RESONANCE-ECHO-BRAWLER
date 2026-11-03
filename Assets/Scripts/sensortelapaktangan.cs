using System.Xml.Serialization;
using UnityEngine;

public class sensortelapaktangan : MonoBehaviour
{
    public GameObject ledakan;
    public GameObject ledakan2;
    public AudioSource suara1;
    public AudioSource suara2;
    public AudioSource suara3;

    public GameObject animationeventreceiver;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        if (animationeventreceiver.GetComponent<penerimaeventanimasi>().pukul == 1)
        {

//  ledakan.GetComponent<partikel>().ledakan(other.transform.position);
        ledakan.transform.position = other.transform.position;
        ledakan2.transform.position = other.transform.position;
        audiopukul();
        ledakan.GetComponent<partikel>().ledakan(other.transform.position);
      
        ledakan2.GetComponent<partikel>().ledakan(other.transform.position);
        Debug.Log("Kena trigger: " + other.name);


        }
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (animationeventreceiver.GetComponent<penerimaeventanimasi>().pukul == 1)
        {
 Debug.Log("Nabrak: " + collision.gameObject.name);
        Debug.Log("Kecepatan relatif: " + collision.relativeVelocity);
        Debug.Log("Rigidbody lawan: " + collision.rigidbody);
        Debug.Log("Jumlah titik kontak: " + collision.contactCount);
        Debug.Log("Kena collision: " + collision.collider.name);
        ledakan.transform.position = collision.transform.position;
        ledakan2.transform.position = collision.transform.position;
        audiopukul();
        ledakan.GetComponent<partikel>().ledakan(collision.transform.position);
     
        ledakan2.GetComponent<partikel>().ledakan(collision.transform.position);

        }
           
    }

    public void audiopukul()
    {
        if (!suara1.isPlaying) { suara1.Play(); } else 
            if(!suara2.isPlaying) { suara2.Play(); }else { suara3.Play(); }
    }
}
