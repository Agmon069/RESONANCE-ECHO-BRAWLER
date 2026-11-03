using UnityEngine;

public class partikel : MonoBehaviour
{
    
  private ParticleSystem partikel1;
    public int ready;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ready = 1;
        partikel1 = GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        if (ready == 0)
        {
            if (!partikel1.IsAlive())
            {
                ready = 1;
            }
        }
    }

    public void ledakan(Vector3 posisi)
    {
        ready = 0;
       transform.position = posisi;
        partikel1.Play();


    }
}
