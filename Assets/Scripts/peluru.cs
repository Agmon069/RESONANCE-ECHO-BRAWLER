using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class peluru : MonoBehaviour
{
    public GameObject crosshair;
    ParticleSystem parbullet;
    bool partikelready = true;

    List<ParticleCollisionEvent>events = new List<ParticleCollisionEvent>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        parbullet = GetComponent<ParticleSystem>();
        parbullet.Stop();
    }

    private void Awake()
    {
        parbullet = GetComponent<ParticleSystem>();
    }
    private void OnParticleCollision(GameObject other)
    {
        int n =parbullet.GetCollisionEvents(other, events);
        for(int i =0; i<n; i++)
        {
            efekpeluru(events[i].intersection);
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (!parbullet.IsAlive()) { partikelready = true; }
    }

    public void efekpeluru(Vector3 lokasicollision)
    {
        crosshair.GetComponent<CrosshairBullet>().letakkanpartikel(lokasicollision);
    }

    public void hadaptembakan(Vector3 target)
    {
        transform.LookAt(target);
        parbullet.Play();
        partikelready=false; 
    }
}
