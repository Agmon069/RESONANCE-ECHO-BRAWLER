using UnityEngine;

public class penerimaeventanimasi : MonoBehaviour
{
    public int pukul;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pukul = 0;
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void hitmulai()
    {
        pukul = 1;
    }
    public void hselesai()
    {
        pukul = 0;
    }

}
