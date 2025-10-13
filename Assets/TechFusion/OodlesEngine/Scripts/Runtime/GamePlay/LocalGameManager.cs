using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OodlesEngine
{
    public class LocalGameManager : SingletonMono<LocalGameManager>
    {
        override protected void Awake()
        {
            base.Awake();

            EventBetter.Listen<LocalGameManager, SearchGrabTargetMessage>(this, OnSearchGrabTarget);
            EventBetter.Listen<LocalGameManager, CheckGrabableMessage>(this, OnCheckGrabable);
            EventBetter.Listen<LocalGameManager, GrabObjectMessage>(this, OnGrabObject);
            EventBetter.Listen<LocalGameManager, ReleaseObjectMessage>(this, OnReleaseObject);
            EventBetter.Listen<LocalGameManager, ThrowObjectMessage>(this, OnThrowObject);
            //EventBetter.Listen<LocalGameManager, TouchObjectMessage>(this, OnTouchObject);
            EventBetter.Listen<LocalGameManager, HandAttackMessage>(this, OnHandAttack);
            EventBetter.Listen<LocalGameManager, WeaponAttackMessage>(this, OnWeaponAttack);
        }

        // Start is called before the first frame update
        void Start()
        {

        }
        void Update()
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
#endif
        }

        private void OnSearchGrabTarget(SearchGrabTargetMessage msg)
        {
            Collider[] hitColliders = Physics.OverlapSphere(msg.hc.transform.position, msg.radius);
            Transform target = null;
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.attachedRigidbody != null &&
                    !hitCollider.attachedRigidbody.isKinematic &&
                    !hitCollider.transform.IsChildOf(msg.hc.oodlesCharacter.transform) && //it's me
                    (hitCollider.gameObject.tag == "CanBeGrabbed" || hitCollider.gameObject.layer == LayerMask.NameToLayer("Ragdoll")))
                {
                    //is weapon handle
                    var handle = hitCollider.GetComponent<WeaponHandler>();
                    if (handle != null)
                    {
                        if (msg.hc.handSide == handle.handSide)
                        {
                            target = hitCollider.transform;
                            break;
                        }
                    }
                    else
                    {
                        target = hitCollider.transform;
                        break;
                    }
                }

            }

            if (target != null)
            {
                msg.callback(target);
            }
        }

        private void OnCheckGrabable(CheckGrabableMessage msg)
        {
            bool res = false;

            if (msg.obj != null)
            {
                if (msg.obj.GetComponent<JointMatch>() != null ||
                    msg.obj.tag == "CanBeGrabbed" ||
                    msg.obj.layer == LayerMask.NameToLayer("Player") ||
                    msg.obj.layer == LayerMask.NameToLayer("Ragdoll") ||
                    msg.obj.layer == LayerMask.NameToLayer("RagdollHands"))
                {
                    res = true;
                }
            }

            msg.callback(res);
        }

        private void OnGrabObject(GrabObjectMessage msg)
        {
            if (msg.obj == null) return;

            WeaponHandler wh = msg.obj.GetComponent<WeaponHandler>();
            if (wh != null)
            {
                wh.SetOwner(msg.pc);

                //attach weapon
                // Physics.IgnoreCollision(wh.GetComponent<Collider>(), msg.hf.GetComponent<Collider>(), false);
                wh.GetComponent<Collider>().isTrigger = true;
                wh.GetComponent<Rigidbody>().isKinematic = true;
                wh.transform.SetParent(msg.hf.transform);
                wh.transform.localPosition = Vector3.zero;
                //wh.GetComponent<Rigidbody>().velocity = Vector3.zero;
                //wh.GetComponent<Rigidbody>().Sleep();
                //wh.transform.localRotation = Quaternion.Euler(45, 45, 45);
                //wh.GetComponent<Rigidbody>().isKinematic = false;
                //wh.GetComponent<Collider>().isTrigger = false;
            }
        }

        private void OnReleaseObject(ReleaseObjectMessage msg)
        {
            if (msg.obj == null) return;

            WeaponHandler wh = msg.obj.GetComponent<WeaponHandler>();
            if (wh != null)
            {
                wh.SetOwner(null);
            }
        }

        private void OnThrowObject(ThrowObjectMessage msg)
        {
            Rigidbody rb = msg.obj.GetComponent<Rigidbody>();
            if (rb == null) return;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            float twoHandsForce = 0;
            float singleHandForce = 0;

            if (msg.obj.GetComponent<Toy>() != null)
            {
                twoHandsForce = 4000;
                singleHandForce = 2000;
            }
            else if (msg.obj.GetComponent<WeaponHandler>() != null)
            {

            }
            else if (msg.obj.GetComponent<JointMatch>() != null)
            {
                twoHandsForce = 8000;
                singleHandForce = 4000;
            }

            if (msg.twoHands)
            {
                rb.AddForce(msg.dir * twoHandsForce);
            }
            else
            {
                rb.AddForce(msg.dir * singleHandForce);
            }
        }

        //private void OnTouchObject(TouchObjectMessage msg)
        //{
        //    if (msg.obj == null) return;

        //    WeaponHandler wh = msg.obj.GetComponent<WeaponHandler>();

        //    if (wh != null)
        //    {
        //        wh.SetOwner(msg.pc);

        //        Weapon wp = wh.wepon;

        //        wp.GetComponent<Rigidbody>().Sleep();

        //        wp.GetComponent<Collider>().isTrigger = true;
        //        wp.GetComponent<Rigidbody>().isKinematic = true;
        //        wp.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.None;
        //        wp.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        //        wp.transform.SetParent(msg.hf.transform);
        //        wp.transform.localPosition = Vector3.zero;
        //        wp.transform.localEulerAngles = new Vector3(0, 0, -90);

        //        wp.GetComponent<Collider>().isTrigger = false;
        //        wp.GetComponent<Rigidbody>().isKinematic = false;
        //        wp.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;
        //        wp.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
        //        wp.transform.SetParent(null);
        //        var catchJoint = wp.gameObject.AddComponent<FixedJoint>();
        //        catchJoint.breakForce = Mathf.Infinity;
        //        catchJoint.connectedBody = msg.hf.gameObject.GetComponent<Rigidbody>();
        //        catchJoint.connectedBody.velocity = Vector3.zero;
        //        catchJoint.connectedBody.angularVelocity = Vector3.zero;
        //    }
        //}

        private void OnHandAttack(HandAttackMessage msg)
        {
            JointMatch animMagnet = msg.col.gameObject.GetComponent<JointMatch>();
            if (animMagnet != null)//hit someone
            {
                animMagnet.oodlesCharacter.KnockDown();
            }

            if (msg.col.rigidbody != null)
                msg.col.rigidbody.AddForce(-msg.col.contacts[0].normal * 1000);
        }

        private void OnWeaponAttack(WeaponAttackMessage msg)
        {
            Rigidbody rb = msg.obj.GetComponent<Rigidbody>();

            if (rb == null || rb.isKinematic) return;

            if (rb != null)
            {
                rb.AddForce(msg.dir * msg.wp.hitForce);
            }

            string player = OodlesSetting.Instance.PlayerLayerName;
            if (msg.obj.layer == LayerMask.NameToLayer(player))
            {
                OodlesCharacter targetPC = msg.obj.GetComponentInParent<OodlesCharacter>();
                if (targetPC != null)
                {
                    targetPC.KnockDown();
                }
            }
        }
    }
}

