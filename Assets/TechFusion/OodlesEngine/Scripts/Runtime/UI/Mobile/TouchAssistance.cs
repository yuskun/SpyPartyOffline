using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OodlesEngine
{
    public class TouchAssistance : MonoBehaviour
    {
        public ButtonSettings mainButton;
        public ButtonSettings subButton1;
        public ButtonSettings subButton2;

        protected virtual void Start()
        {
            mainButton.Initialize();
            subButton1.Initialize();
            subButton2.Initialize();
        }

        protected virtual void Update()
        {
            mainButton.UpdateState();
            subButton1.UpdateState();
            subButton2.UpdateState();
        }

        public void MainButtonPressed()
        {
            mainButton.SetToFocusedScale();
            subButton1.SetToUnfocusedScale();
            subButton2.SetToUnfocusedScale();
        }

        public void SubButton1Pressed()
        {
            mainButton.SetToUnfocusedScale();
            subButton1.SetToFocusedScale();
            subButton2.SetToUnfocusedScale();
        }

        public void SubButton2Pressed()
        {
            mainButton.SetToUnfocusedScale();
            subButton1.SetToUnfocusedScale();
            subButton2.SetToFocusedScale();
        }

        [System.Serializable]
        public class ButtonSettings
        {
            public RectTransform button;
            protected Vector3 referenceScale;
            public float focusedMultiplier = 1.2f;
            public float unfocusedMultiplier = 0.8f;
            protected Vector3 targetScale;
            public float scaleSpeed = 1.6f;

            public void Initialize()
            {
                referenceScale = button.localScale;
                targetScale = referenceScale;
            }

            public void UpdateState()
            {
                button.localScale = Vector3.Lerp(button.localScale, targetScale, scaleSpeed * Time.deltaTime);
            }

            public void SetToFocusedScale()
            {
                targetScale = referenceScale * focusedMultiplier;
            }

            public void SetToUnfocusedScale()
            {
                targetScale = referenceScale * unfocusedMultiplier;
            }
        }
    }
}
