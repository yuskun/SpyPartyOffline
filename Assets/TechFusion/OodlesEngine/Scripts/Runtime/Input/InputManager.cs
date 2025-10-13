using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace OodlesEngine
{
    public class InputManager : MonoBehaviour
    {
        private static InputManager _instance;

        //mobile controller
        public GameObject MobileControllerPrefab;
        private GameObject mobileController;
        private AnalogStick moveStick;
        //private Vector2 moveStickDir;
        private float range1d16, range3d16;

        private Button jumpButton;
        private Image tjumpoff;
        private Image tjumpon;
        private float jumpDown = 0;

        private Button leftHandButton;
        private Button rightHandButton;
        private Image tleftoff;
        private Image tlefton;
        private Image trightoff;
        private Image trighton;
        private float useLeftHand = 0, useRightHand = 0;

        private Button action1Button;
        private Image taction1off;
        private Image taction1on;
        private float action1Down = 0;

        private TouchArea aimArea;
        private Vector3 cachedInputAim;
        public float aimSensitive = 3.0f;

        public static InputManager Get()
        {
            return _instance;
        }

        void Awake()
        {
            _instance = this;
        }

        private void Start()
        {
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
                //if (true)
                InitMobileController();

        }

        void InitMobileController()
        {
            mobileController = Instantiate(MobileControllerPrefab, transform);
            moveStick = mobileController.transform.Find("DPad Outer").GetComponent<AnalogStick>();

            range1d16 = Mathf.Cos((1 / 16.0f) * Mathf.PI * 2);
            range3d16 = Mathf.Cos((3 / 16.0f) * Mathf.PI * 2);

            //jump
            jumpButton = mobileController.transform.Find("ButJump").GetComponent<Button>();
            {
                EventTrigger trigger = jumpButton.gameObject.AddComponent<EventTrigger>();
                tjumpoff = jumpButton.transform.Find("Image").GetComponent<Image>();
                tjumpon = jumpButton.transform.Find("ImageOn").GetComponent<Image>();
                if (tjumpon) tjumpon.enabled = false;

                EventTrigger.Entry entryDown = new EventTrigger.Entry();
                entryDown.eventID = EventTriggerType.PointerDown;
                entryDown.callback = new EventTrigger.TriggerEvent();
                entryDown.callback.AddListener((BaseEventData pointData) => { OnJump(true); });
                trigger.triggers.Add(entryDown);

                EventTrigger.Entry entryUp = new EventTrigger.Entry();
                entryUp.eventID = EventTriggerType.PointerUp;
                entryUp.callback = new EventTrigger.TriggerEvent();
                entryUp.callback.AddListener((BaseEventData pointData) => { OnJump(false); });
                trigger.triggers.Add(entryUp);
            }

            //action1
            action1Button = mobileController.transform.Find("ButThrow").GetComponent<Button>();
            {
                EventTrigger trigger = action1Button.gameObject.AddComponent<EventTrigger>();
                taction1off = action1Button.transform.Find("Image").GetComponent<Image>();
                taction1on = action1Button.transform.Find("ImageOn").GetComponent<Image>();
                if (taction1on) taction1on.enabled = false;

                EventTrigger.Entry entryDown = new EventTrigger.Entry();
                entryDown.eventID = EventTriggerType.PointerDown;
                entryDown.callback = new EventTrigger.TriggerEvent();
                entryDown.callback.AddListener((BaseEventData pointData) =>
                {
                    OnAction1(true);
                });
                trigger.triggers.Add(entryDown);

                EventTrigger.Entry entryUp = new EventTrigger.Entry();
                entryUp.eventID = EventTriggerType.PointerUp;
                entryUp.callback = new EventTrigger.TriggerEvent();
                entryUp.callback.AddListener((BaseEventData pointData) =>
                {
                    OnAction1(false);
                });
                trigger.triggers.Add(entryUp);
            }

            //left
            leftHandButton = mobileController.transform.Find("ButAttack").GetComponent<Button>();
            {
                EventTrigger trigger = leftHandButton.gameObject.AddComponent<EventTrigger>();
                tleftoff = leftHandButton.transform.Find("Image").GetComponent<Image>();
                tlefton = leftHandButton.transform.Find("ImageOn").GetComponent<Image>();
                if (tlefton) tlefton.enabled = false;
                EventTrigger.Entry entryDown = new EventTrigger.Entry();
                entryDown.eventID = EventTriggerType.PointerDown;
                entryDown.callback = new EventTrigger.TriggerEvent();
                entryDown.callback.AddListener((BaseEventData pointData) =>
                {
                    OnUseLeftHand(true);
                });
                trigger.triggers.Add(entryDown);

                EventTrigger.Entry entryUp = new EventTrigger.Entry();
                entryUp.eventID = EventTriggerType.PointerUp;
                entryUp.callback = new EventTrigger.TriggerEvent();
                entryUp.callback.AddListener((BaseEventData pointData) =>
                {
                    OnUseLeftHand(false);
                });
                trigger.triggers.Add(entryUp);

            }

            //right
            rightHandButton = mobileController.transform.Find("ButPick").GetComponent<Button>();
            {
                EventTrigger trigger = rightHandButton.gameObject.AddComponent<EventTrigger>();
                trightoff = rightHandButton.transform.Find("Image").GetComponent<Image>();
                trighton = rightHandButton.transform.Find("ImageOn").GetComponent<Image>();
                if (trighton) trighton.enabled = false;
                EventTrigger.Entry entryDown = new EventTrigger.Entry();
                entryDown.eventID = EventTriggerType.PointerDown;
                entryDown.callback = new EventTrigger.TriggerEvent();
                entryDown.callback.AddListener((BaseEventData pointData) =>
                {
                    OnUseRightHand(true);
                });
                trigger.triggers.Add(entryDown);

                EventTrigger.Entry entryUp = new EventTrigger.Entry();
                entryUp.eventID = EventTriggerType.PointerUp;
                entryUp.callback = new EventTrigger.TriggerEvent();
                entryUp.callback.AddListener((BaseEventData pointData) =>
                {
                    OnUseRightHand(false);
                });
                trigger.triggers.Add(entryUp);
            }

            //aim area
            aimArea = mobileController.transform.Find("TouchArea").GetComponent<TouchArea>();
            aimArea.onDrag.AddListener(OnAimDrag);
            aimArea.onPointerUp.AddListener(OnAimPointerUp);
        }

        void OnUseLeftHand(bool enable)
        {
            if (enable)
            {
                this.useLeftHand = 1;
                if (tleftoff) tleftoff.enabled = false;
                if (tlefton) tlefton.enabled = true;
            }
            else
            {
                this.useLeftHand = 0;
                if (tleftoff) tleftoff.enabled = true;
                if (tlefton) tlefton.enabled = false;
            }
        }

        void OnUseRightHand(bool enable)
        {
            if (enable)
            {
                this.useRightHand = 1;
                if (trightoff) trightoff.enabled = false;
                if (trighton) trighton.enabled = true;
            }
            else
            {
                this.useRightHand = 0;
                if (trightoff) trightoff.enabled = true;
                if (trighton) trighton.enabled = false;
            }
        }

        void OnJump(bool enable)
        {
            if (enable)
            {
                this.jumpDown = 1;
                if (tjumpoff) tjumpoff.enabled = false;
                if (tjumpon) tjumpon.enabled = true;
            }
            else
            {
                this.jumpDown = 0;
                if (tjumpoff) tjumpoff.enabled = true;
                if (tjumpon) tjumpon.enabled = false;
            }
        }

        void OnAction1(bool enable)
        {
            if (enable)
            {
                this.action1Down = 1;
                if (taction1off) taction1off.enabled = false;
                if (taction1on) taction1on.enabled = true;
            }
            else
            {
                this.action1Down = 0;
                if (taction1off) taction1off.enabled = true;
                if (taction1on) taction1on.enabled = false;
            }
        }

        void OnAimDrag(int btnId)
        {
            if (btnId == 0)
            {
                cachedInputAim = Vector3.zero;

                Vector3 delta = aimArea.deltaFingerPositionInchesYX * aimSensitive;

                cachedInputAim = delta;
                cachedInputAim.z = 0f;
            }
        }

        void OnAimPointerUp(int btnId)
        {
            cachedInputAim = Vector3.zero;
        }

        public float GetVertical()
        {
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            //if (true)
            {
                if (moveStick.state != UniversalButton.ButtonState.Pressed)
                    return 0;

                if (moveStick.vertical > range3d16)
                {
                    return 1;
                }
                else if (moveStick.vertical > -range3d16 && moveStick.vertical <= range3d16)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                return Input.GetAxisRaw("Vertical");
            }
        }

        public float GetHorizontal()
        {
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            //if (true)
            {
                if (moveStick.state != UniversalButton.ButtonState.Pressed)
                    return 0;

                if (moveStick.horizontal > range3d16)
                {
                    return 1;
                }
                else if (moveStick.horizontal > -range3d16 && moveStick.horizontal <= range3d16)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                return Input.GetAxisRaw("Horizontal");
            }
        }

        public float GetJump()
        {
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            //if (true)
            {
                return jumpDown;
            }
            else
            {
                return Input.GetAxisRaw("Jump");
            }
        }

        public float GetTouchMoveX()
        {
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                return cachedInputAim.y;
            }
            else
            {
                // 若滑鼠游標不是鎖定狀態，則不接收滑鼠輸入
                if (Cursor.lockState != CursorLockMode.Locked)
                    return 0f;

                return Input.GetAxisRaw("Mouse X") * 2;
            }
        }


        public float GetTouchMoveY()
        {
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                return cachedInputAim.x;
            }
            else
            {
                // 若滑鼠游標不是鎖定狀態，則不接收滑鼠輸入
                if (Cursor.lockState != CursorLockMode.Locked)
                    return 0f;

                return Input.GetAxisRaw("Mouse Y") * 2;
            }
        }

        public float GetLeftHandUse()
        {
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            //if (true)
            {
                return useLeftHand;
            }
            else
            {
                return Input.GetAxisRaw("Fire1");
            }
        }

        public float GetRightHandUse()
        {
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            //if (true)
            {
                return useRightHand;
            }
            else
            {
                return Input.GetAxisRaw("Fire2");
            }
        }

        public float GetDoAction1()
        {
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            //if (true)
            {
                return action1Down;
            }
            else
            {
                if (Input.GetKey(KeyCode.T))
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        public Vector3 GetCameraLook()
        {
            return Camera.main.transform.forward;
        }
    }
}