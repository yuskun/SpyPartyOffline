using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OodlesEngine
{
    public partial class OodlesCharacter : MonoBehaviour
    {
        public State GetState()
        {
            return curState;
        }
    }
}