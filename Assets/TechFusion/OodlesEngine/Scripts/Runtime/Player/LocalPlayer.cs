using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OodlesEngine
{
    public class LocalPlayer : SingletonMono<LocalPlayer>
    {
        OodlesCharacter characterController;
        public bool AllowCamerafollow=true;
        // Start is called before the first frame update
        void Start()
        {
            characterController = GetComponent<OodlesCharacter>();
            if (AllowCamerafollow)
            {
                CameraFollow.Get().player = characterController.GetPhysicsBody().transform;
                CameraFollow.Get().enable = true;
            }
     
            
        }

        // Update is called once per frame
        void Update()
        {
            OodlesCharacterInput pci = new OodlesCharacterInput(
                    InputManager.Get().GetVertical(),
                    InputManager.Get().GetHorizontal(),
                    InputManager.Get().GetJump(),
                    InputManager.Get().GetTouchMoveY(),
                    InputManager.Get().GetLeftHandUse(),
                    InputManager.Get().GetRightHandUse(),
                    InputManager.Get().GetDoAction1(),
                    InputManager.Get().GetCameraLook(),
                    Time.deltaTime, 0);

            characterController.ProcessInput(pci);
        }

        public Vector3 GetPosition()
        {
            return characterController.GetCharacterPosition();
        }

        public OodlesCharacter.State GetState() 
        {
            return characterController.GetState();
        }
    }
}
