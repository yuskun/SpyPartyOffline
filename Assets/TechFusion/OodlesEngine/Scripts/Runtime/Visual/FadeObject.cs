using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OodlesEngine
{
    public class FadeObject : MonoBehaviour
    {
        private const int MATERIAL_OPAQUE = 0;
        private const int MATERIAL_TRANSPARENT = 1;

        List<Material> childMaterialsList = new List<Material>();

        bool fading = false;
        int fadeCommand = 0;

        void Start()
        {
            GetAllChildMaterialsOf(gameObject);
        }

        void GetAllChildMaterialsOf(GameObject parent)
        {
            Transform[] children = parent.GetComponentsInChildren<Transform>();

            foreach (Transform child in children)
            {
                if (child.parent == parent.transform)
                {
                    Renderer[] renderers = child.GetComponents<Renderer>();

                    foreach (Renderer renderer in renderers)
                    {
                        Material[] materials = renderer.materials;

                        foreach (Material mat in materials)
                        {
                            if (mat != null && mat.GetFloat("_Surface") == MATERIAL_OPAQUE)
                            {
                                childMaterialsList.Add(mat);
                            }
                        }
                    }

                    GetAllChildMaterialsOf(child.gameObject);
                }
            }
        }

        private void SetMaterialTransparent(Material material, bool enabled)
        {
            material.SetFloat("_Surface", enabled ? MATERIAL_TRANSPARENT : MATERIAL_OPAQUE);
            material.SetShaderPassEnabled("SHADOWCASTER", !enabled);
            material.renderQueue = enabled ? 3000 : 2000;
            material.SetFloat("_DstBlend", enabled ? 10 : 0);
            material.SetFloat("_SrcBlend", enabled ? 5 : 1);
            material.SetFloat("_ZWrite", enabled ? 0 : 1);

            Color col = material.color; col = new Color(col.r, col.g, col.b, 0.0f);
            material.color = col;
        }

        public void ApplyFadeThisFrame()
        {
            fadeCommand += 1;
            fadeCommand = Mathf.Max(fadeCommand, 2);

            if (fading) return;

            foreach (Material mat in childMaterialsList)
            {
                SetMaterialTransparent(mat, true);
            }

            fading = true;
        }

        public void UnFade()
        {
            fading = false;

            foreach (Material mat in childMaterialsList)
            {
                SetMaterialTransparent(mat, false);
            }
        }

        private void LateUpdate()
        {
            fadeCommand--;

            if (fadeCommand < 0)
            {
                fadeCommand = 0;
                UnFade();
            }
        }
    }
}