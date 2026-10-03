// WorldRoot.cs - componenta salvata in scena (numele fisierului = numele clasei, cerinta Unity)
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    /// <summary>Radacina lumii salvate in scena. Daca exista, jocul NU mai construieste lumea din cod.</summary>
    public class WorldRoot : MonoBehaviour
    {
        [Tooltip("Versiunea lumii (GameData.WorldVersion) cu care a fost construita scena.")]
        public int version;
        public static WorldRoot Find()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindAnyObjectByType<WorldRoot>();
#else
            return Object.FindObjectOfType<WorldRoot>();
#endif
        }
    }
}
