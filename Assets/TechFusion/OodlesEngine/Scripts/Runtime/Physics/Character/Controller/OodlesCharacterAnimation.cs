using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OodlesEngine
{
    public partial class OodlesCharacter : MonoBehaviour
    {
        [Range(0.1f, 5f)]
        [HideInInspector] public float animationSpeed = 1;
    }
}