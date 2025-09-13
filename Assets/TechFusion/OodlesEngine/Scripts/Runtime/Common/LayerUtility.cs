using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OodlesEngine
{
    public class LayerUtility
    {
        static public void SetLayerRecursively(Transform obj, string layerName)
        {
            obj.gameObject.layer = LayerMask.NameToLayer(layerName);

            foreach (Transform child in obj)
            {
                SetLayerRecursively(child, layerName);
            }
        }
    }
}

