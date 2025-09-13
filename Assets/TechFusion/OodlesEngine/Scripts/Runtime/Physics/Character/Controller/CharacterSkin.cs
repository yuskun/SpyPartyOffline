using UnityEngine;
using System.Collections;

namespace OodlesEngine
{ 
    public class CharacterSkin : MonoBehaviour
    {
         public  enum SkinColor
        {
            Blue =0,
            Red,
        }
        // Use this for initialization
        public Material[] skin;

        public void SetSkinMat(SkinColor color)
        {
            Renderer[] renders = transform.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renders)
            {
                if (r != null) r.material = skin[(int)color];
            }
        }
    }
}