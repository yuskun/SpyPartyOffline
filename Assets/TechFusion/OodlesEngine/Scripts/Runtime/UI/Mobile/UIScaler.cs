using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OodlesEngine
{
    [DefaultExecutionOrder(-1200)]
    public class UIScaler : MonoBehaviour
    {
        public CanvasScaler canvasScaler;

        public float referenceDPI;
        public float referenceScaleFactor;
        public float referenceWidth;
        public float referenceHeight;

        public float referenceInches;
        public float referenceDiagonalInches;
        public float preferredScaleFactor;

        public UIScaleMode scaleMode;

        public AnimationCurve scaleByScreenSizeInches;

        public AnimationCurve scaleMultiplierByDpi;
        public AnimationCurve scaleMultiplierByAspectRatio;

        protected virtual void Awake()
        {
            referenceDPI = 458;
            referenceWidth = 2688;
            referenceHeight = 1242;
            referenceInches = referenceWidth / (float)referenceDPI;
            referenceDiagonalInches = 6.465209f;
            referenceScaleFactor = 2.061925f;

            UpdateScale();
        }

        public void UpdateScale()
        {
            switch (scaleMode)
            {
                case UIScaleMode.Variable:
                    preferredScaleFactor = referenceScaleFactor * Screen.width / referenceWidth
                        * scaleMultiplierByDpi.Evaluate(Screen.dpi)
                        * scaleMultiplierByAspectRatio.Evaluate(Screen.width / (float)Screen.height);
                    break;
            }
            canvasScaler.scaleFactor = preferredScaleFactor;
            LogScaleInfo();
        }

        protected void LogScaleInfo()
        {
            Debug.Log(
                "DPI " + Screen.dpi + "\n" +
                "Width " + Screen.width + "\n" +
                "Height " + Screen.height + "\n" +
                "Inches " + GetDiagonalInches(Screen.width, Screen.height, Screen.dpi) + "\n" +
                "ScaleFactor " + canvasScaler.scaleFactor
            );
        }

        public float GetDiagonalPixel(int w, int h)
        {
            return Mathf.Sqrt(Mathf.Pow(w, 2) + Mathf.Pow(h, 2));
        }

        public float GetDiagonalInches(int w, int h, float dpi)
        {
            return GetDiagonalPixel(w, h) / dpi;
        }

        public enum UIScaleMode
        {
            Variable
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(UIScaler))]
    public class UIScalerInspector : UnityEditor.Editor
    {
        protected UIScaler script;

        protected SerializedProperty canvasScaler;
        protected SerializedProperty scaleMode;
        protected SerializedProperty scaleMultiplierByDpi;
        protected SerializedProperty scaleMultiplierByAspectRatio;

        protected float width;
        protected float height;
        protected float dpi;
        protected float aspectRatio;
        protected float tmpFloat;

        protected virtual void OnEnable()
        {
            script = target as UIScaler;
            canvasScaler = serializedObject.FindProperty("canvasScaler");
            scaleMode = serializedObject.FindProperty("scaleMode");
            scaleMultiplierByDpi = serializedObject.FindProperty("scaleMultiplierByDpi");
            scaleMultiplierByAspectRatio = serializedObject.FindProperty("scaleMultiplierByAspectRatio");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(canvasScaler);
            EditorGUILayout.PropertyField(scaleMode);
            EditorGUILayout.PropertyField(scaleMultiplierByDpi);
            EditorGUILayout.PropertyField(scaleMultiplierByAspectRatio);

            if (GUILayout.Button("Update scale"))
            {
                script.referenceDPI = 458;
                script.referenceWidth = 2688;
                script.referenceHeight = 1242;
                script.referenceInches = script.referenceWidth / (float)script.referenceDPI;
                script.referenceDiagonalInches = 6.465209f;
                script.referenceScaleFactor = 2.061925f;
                script.canvasScaler.runInEditMode = true;
                script.UpdateScale();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
