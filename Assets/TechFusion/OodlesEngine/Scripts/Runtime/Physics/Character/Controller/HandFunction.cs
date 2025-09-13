using System.Collections;
using System.Drawing;
using UnityEngine;

namespace OodlesEngine
{ 
    public enum HandSide
    {
        HandLeft = 0,
        HandRight = 1,
    }

    public class HandFunction : MonoBehaviour
    {
        [SerializeField]
        [HideInInspector]
        public OodlesCharacter oodlesCharacter;

        public HandSide handSide;

        [HideInInspector]
        public bool hasJoint;
        [HideInInspector]
        public Rigidbody GrabbedObject;
        [HideInInspector]
        public Joint catchJoint;

        public void ProcessInput()
        {
            //if (oodlesCharacter.useControls)
            {
                if (hasJoint && catchJoint == null)
                {
                    hasJoint = false;
                    GrabbedObject = null;
                }
            }
        }

        public void ReleaseHand()
        {
            if (GrabbedObject != null)
            {
                EventBetter.Raise(new ReleaseObjectMessage()
                {
                    pc = oodlesCharacter,
                    obj = GrabbedObject.gameObject
                });
            }

            if (catchJoint != null)
            {
                catchJoint.breakForce = 0;
                catchJoint.connectedBody = null;
                Destroy(catchJoint);
            }

            //weapon
            if (GrabbedObject != null)
            {
                Weapon wp = GrabbedObject.GetComponent<Weapon>();
                if (wp != null)
                {
                    wp.owner = null;
                }
            }
            
            hasJoint = false;
            GrabbedObject = null;
        }

        public bool HoldWeapon()
        {
            if (GrabbedObject == null) return false;

            Weapon wp = GrabbedObject.GetComponent<Weapon>();
            if (wp == null) return false;

            return true;
        }

        void SetFixedJoint(GameObject obj)
        {
            //catchJoint = this.gameObject.AddComponent<FixedJoint>();
            //catchJoint.breakForce = Mathf.Infinity;
            //catchJoint.connectedBody = obj.GetComponent<Rigidbody>();
            //catchJoint.connectedBody.velocity = Vector3.zero;
            //catchJoint.connectedBody.angularVelocity = Vector3.zero;
            //this.gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
            //this.gameObject.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

            catchJoint = obj.AddComponent<FixedJoint>();
            catchJoint.breakForce = Mathf.Infinity;
            catchJoint.connectedBody = gameObject.GetComponent<Rigidbody>();
            catchJoint.connectedBody.linearVelocity = Vector3.zero;
            catchJoint.connectedBody.angularVelocity = Vector3.zero;
        }

        void SetConfigurableJoint(GameObject obj)
        {
            ConfigurableJoint cJoint = this.gameObject.AddComponent<ConfigurableJoint>();
            cJoint.xMotion = ConfigurableJointMotion.Locked;
            cJoint.yMotion = ConfigurableJointMotion.Locked;
            cJoint.zMotion = ConfigurableJointMotion.Locked;

            catchJoint = cJoint;
            catchJoint.breakForce =  Mathf.Infinity;
            catchJoint.connectedBody = obj.GetComponent<Rigidbody>();
        }

        void OnGrabSomething(Collision col)
        {
            if (hasJoint) return;

            GrabbedObject = col.gameObject.GetComponent<Rigidbody>();

            //character body and props
            if (GrabbedObject != null)
            {
                bool Grabable = false;

                EventBetter.Raise(new CheckGrabableMessage()
                {
                    pc = null,//not used
                    obj = col.gameObject,
                    callback = (bool b) => {
                        Grabable = b;
                    }
                });

                if (Grabable)
                {
                    SetConfigurableJoint(col.gameObject);

                    hasJoint = true;

                    EventBetter.Raise(new GrabObjectMessage()
                    {
                        pc = oodlesCharacter,
                        hf = this,
                        obj = col.gameObject
                    });
                }
            }
        }

        void OnAttackSomething(Collision col)
        {
            EventBetter.Raise(new HandAttackMessage()
            {
                pc = oodlesCharacter,
                col = col
            });
        }

        void OnTouchSomething(Collider other)
        {
            if (hasJoint) return;

            WeaponHandler wh = other.gameObject.GetComponent<WeaponHandler>();

            if (wh != null)
            {
                if (wh.wepon.owner != null) return;

                wh.SetOwner(oodlesCharacter);

                Weapon wp = wh.wepon;

                wp.GetComponent<Rigidbody>().Sleep();

                wp.GetComponent<Collider>().isTrigger = true;
                wp.GetComponent<Rigidbody>().isKinematic = true;
                wp.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.None;
                wp.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                wp.transform.SetParent(transform);
                wp.transform.localPosition = Vector3.zero;
                wp.transform.localEulerAngles = new Vector3(0, 0, -90);

                wp.GetComponent<Collider>().isTrigger = false;
                wp.GetComponent<Rigidbody>().isKinematic = false;
                wp.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;
                wp.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                wp.transform.SetParent(null);

                //var catchJoint = gameObject.AddComponent<FixedJoint>();
                //catchJoint.breakForce = Mathf.Infinity;
                //catchJoint.connectedBody = wp.GetComponent<Rigidbody>();
                //catchJoint.connectedBody.velocity = Vector3.zero;
                //catchJoint.connectedBody.angularVelocity = Vector3.zero;

                SetFixedJoint(wp.gameObject);

                hasJoint = true;
                GrabbedObject = wp.GetComponent<Rigidbody>();
            }
        }

        //Grab on collision
        void OnCollisionEnter(Collision col)
        {
            if (oodlesCharacter == null) return;

            //not me
            if (col.gameObject.transform.IsChildOf(oodlesCharacter.transform))
            {
                return;
            }

            //Debug.Log(col.collider.gameObject.name);


            if ((handSide == HandSide.HandLeft ? oodlesCharacter.IsLeftArmWorking() : oodlesCharacter.IsRightArmWorking()) &&
                !hasJoint)
            {
                OnGrabSomething(col);
            }

            if (!hasJoint && oodlesCharacter.isAttacking)
            {
                OnAttackSomething(col);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            OnTouchSomething(other);
        }

        //public void GrabSomething(GameObject go)
        //{
        //    if (hasJoint) return;

        //    bool Grabable = false;

        //    EventBetter.Raise(new CheckGrabableMessage()
        //    {
        //        pc = null,//not used
        //        obj = go,
        //        callback = (bool b) => {
        //            Grabable = b;
        //        }
        //    });

        //    if (Grabable)
        //    {
        //        OnGrabSomething(go);
        //    }
        //}
    }
}