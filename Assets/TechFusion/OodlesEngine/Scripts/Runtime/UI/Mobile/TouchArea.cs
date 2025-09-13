using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using TMPro;

namespace OodlesEngine
{
    public class TouchArea : MonoBehaviour,
    IPointerDownHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IPointerUpHandler
    {
        public bool debugLog = true;
        public CanvasScaler scaler;

        public UniversalButton.ButtonState state;
        public bool isActive;
        public float buttonRadius;
        public float aimerRadius;
        public bool isFingerDown = false;
        public bool isPointerUpOutOfBound;

        public Vector3 initialFingerPosition;
        protected Vector3 lastKnownFingerPosition;
        public Vector3 fingerPosition;
        public Vector3 deltaFingerPositionRaw;
        public Vector3 deltaFingerPositionInches;

        public Vector3 deltaFingerPositionRawYX;
        public Vector3 deltaFingerPositionInchesYX;

        public int fingerId = -99;
        public float totalDragDistance;

        public AnimationCurve deadzoneCurve;

        public int buttonIndex;
        public UnityEventInt onPointerDown;
        public UnityEventInt onBeginDrag;
        public UnityEventInt onDrag;
        public UnityEventInt onPointerUp;
        public UnityEventInt onEndDrag;
        public UnityEventInt onActivateSkill;
        public UnityEventInt onCancelSkill;

        protected virtual void Awake()
        {
            scaler = gameObject.GetComponentInParent<CanvasScaler>();
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (state == UniversalButton.ButtonState.Active)
            {
                if (debugLog)
                {
                    Debug.Log("[" + gameObject.name + "] " + "OnPointerDown - FingerID: " + eventData.pointerId);
                }

                isFingerDown = true;
                fingerId = eventData.pointerId;
                isPointerUpOutOfBound = false;

                initialFingerPosition = eventData.position;
                fingerPosition = initialFingerPosition;
                lastKnownFingerPosition = fingerPosition;
                deltaFingerPositionRaw = Vector3.zero;

                state = UniversalButton.ButtonState.Pressed;

                if (onPointerDown != null)
                {
                    onPointerDown.Invoke(buttonIndex);
                }
            }
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == fingerId && state == UniversalButton.ButtonState.Pressed)
            {
                if (debugLog)
                {
                    Debug.Log("[" + gameObject.name + "] " + "OnBeginDrag - FingerID: " + eventData.pointerId);
                }

                totalDragDistance = 0f;
                this.UpdateDrag(eventData);

                if (onBeginDrag != null)
                {
                    onBeginDrag.Invoke(buttonIndex);
                }
            }
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == fingerId && state == UniversalButton.ButtonState.Pressed)
            {
                if (debugLog)
                {
                    Debug.Log("[" + gameObject.name + "] " + "OnDrag - FingerID: " + eventData.pointerId);
                }

                this.UpdateDrag(eventData);
                totalDragDistance += deltaFingerPositionInches.magnitude;

                if (debugLog)
                {
                    Debug.Log("OnDrag: " + this.deltaFingerPositionInches.ToString("F7"));
                }

                if (onDrag != null)
                {
                    onDrag.Invoke(buttonIndex);
                }
                LogDrag();
            }
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == fingerId && state == UniversalButton.ButtonState.Pressed)
            {
                if (debugLog)
                {
                    Debug.Log("[" + gameObject.name + "] " + "OnPointerUp - FingerID: " + eventData.pointerId);
                }

                isFingerDown = false;
                fingerId = -99;
                this.UpdateDrag(eventData);

                state = UniversalButton.ButtonState.Active;

                if (onPointerUp != null)
                {
                    onPointerUp.Invoke(buttonIndex);
                }
            }
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == fingerId)
            {
                if (debugLog)
                {
                    Debug.Log("[" + gameObject.name + "] " + "OnEndDrag - FingerID: " + eventData.pointerId);
                }

                this.UpdateDrag(eventData);

                if (onEndDrag != null)
                {
                    onEndDrag.Invoke(buttonIndex);
                }
            }
        }

        protected virtual void UpdateDrag(PointerEventData eventData)
        {
            fingerPosition = eventData.position;

            deltaFingerPositionRaw = fingerPosition - lastKnownFingerPosition;
            deltaFingerPositionInches = deltaFingerPositionRaw / Screen.dpi;
            deltaFingerPositionInches = deltaFingerPositionInches * deadzoneCurve.Evaluate(deltaFingerPositionInches.magnitude);

            lastKnownFingerPosition = fingerPosition;

            deltaFingerPositionRawYX.x = deltaFingerPositionRaw.y;
            deltaFingerPositionRawYX.y = deltaFingerPositionRaw.x;

            deltaFingerPositionInchesYX.x = deltaFingerPositionInches.y;
            deltaFingerPositionInchesYX.y = deltaFingerPositionInches.x;
        }

        protected string tmpString;

        protected virtual void LogDrag()
        {
            tmpString = "initialFingerPosition: " + initialFingerPosition + " | ";
            tmpString += "fingerPosition: " + fingerPosition + "\n";
            tmpString += "deltaFingerPosition: " + deltaFingerPositionRaw.x + " " + deltaFingerPositionRaw.y + " ";
            tmpString += "totalDragDistance: " + totalDragDistance;

            Debug.Log(tmpString);
        }
    }
}
