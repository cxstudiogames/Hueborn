// Spinner.cs - componenta salvata in scena (numele fisierului = numele clasei, cerinta Unity)
using UnityEngine;

namespace ColorRPG
{
    // ------------------------------------------------------------------
    //  Mici componente ajutatoare
    // ------------------------------------------------------------------
    public class Spinner : MonoBehaviour
    {
        public Vector3 rot;
        public float bobAmp, bobSpeed;
        Vector3 basePos;
        void Start() { basePos = transform.localPosition; }
        void Update()
        {
            transform.Rotate(rot * Time.deltaTime);
            if (bobAmp > 0f)
                transform.localPosition = basePos + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobAmp;
        }
    }
}
