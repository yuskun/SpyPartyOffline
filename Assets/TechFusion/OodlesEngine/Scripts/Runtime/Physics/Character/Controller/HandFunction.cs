using System.Collections;
using System.Collections.Generic;
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
        private Rigidbody hiddenWeapon;

        [HideInInspector]
        public Joint catchJoint;
        [HideInInspector] public bool isWeaponHidden;
        // ✅ 新增：用來儲存所有武器的列表
        private List<Rigidbody> weaponList = new List<Rigidbody>();

        // ✅ 紀錄目前顯示中的武器
        private int currentWeaponIndex = -1;

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
            // 🔹 如果手上沒有東西就不用處理
            if (GrabbedObject == null && catchJoint == null)
                return;

            // 1️⃣ 通知事件系統（如果有人監聽）
            if (GrabbedObject != null)
            {
                EventBetter.Raise(new ReleaseObjectMessage()
                {
                    pc = oodlesCharacter,
                    obj = GrabbedObject.gameObject
                });
            }

            // 2️⃣ 拆除 Joint（解除物理綁定）
            if (catchJoint != null)
            {
                catchJoint.breakForce = 0;
                catchJoint.connectedBody = null;
                Destroy(catchJoint);
                catchJoint = null;
            }

            // 3️⃣ 如果武器存在 → 清除所有屬性後刪除
            if (GrabbedObject != null)
            {
                GameObject weaponObj = GrabbedObject.gameObject;

                // 移除 owner 狀態（防止殘留）
                Weapon wp = weaponObj.GetComponentInChildren<Weapon>(true);
                if (wp != null)
                {
                    wp.owner = null;
                }

                WeaponHandler wh = weaponObj.GetComponentInChildren<WeaponHandler>(true);
                if (wh != null)
                {
                    wh.SetOwner(null);
                }

                // ⚠️ 直接刪除武器
                Destroy(weaponObj);
            }

            // 4️⃣ 重設狀態
            hasJoint = false;
            GrabbedObject = null;

            Debug.Log($"🗑️ [ReleaseHand] {handSide} hand released and destroyed weapon");
        }

        public void AddWeapon(GameObject weaponPrefab)
        {
            // 如果手上已經有東西，先釋放
            if (hasJoint || GrabbedObject != null)
            {
                ReleaseHand();
            }

            // 1️⃣ 生成武器
            GameObject weapon = Instantiate(weaponPrefab);

            WeaponHandler wh = weapon.GetComponentInChildren<WeaponHandler>(true);
            if (wh == null)
            {
                Debug.LogError($"[AddWeapon] {weaponPrefab.name} 沒有 WeaponHandler");
                Destroy(weapon);
                return;
            }

            // 2️⃣ 設定擁有者
            wh.SetOwner(oodlesCharacter);
            Weapon wp = wh.wepon;
            Rigidbody rb = wp.GetComponent<Rigidbody>();

            // 3️⃣ 啟用渲染與碰撞（直接顯示）
            foreach (var r in rb.GetComponentsInChildren<Renderer>(true))
                r.enabled = true;
            foreach (var c in rb.GetComponentsInChildren<Collider>(true))
                c.enabled = true;

            // 4️⃣ 對齊手的位置
            rb.transform.position = transform.position;
            rb.transform.rotation = transform.rotation * Quaternion.Euler(0, 0, -90);
            Physics.SyncTransforms();

            // 5️⃣ 設定物理
            rb.isKinematic = false;
            rb.useGravity = true;

            // 6️⃣ 綁 FixedJoint
            FixedJoint fj = gameObject.AddComponent<FixedJoint>();
            fj.breakForce = Mathf.Infinity;
            fj.connectedBody = rb;

            // 7️⃣ 設定狀態
            catchJoint = fj;
            hasJoint = true;
            GrabbedObject = rb;

            // 8️⃣ 通知事件系統
            EventBetter.Raise(new GrabObjectMessage()
            {
                pc = oodlesCharacter,
                hf = this,
                obj = rb.gameObject
            });

            Debug.Log($"🖐️ [AddWeapon] {handSide} 直接拿起武器 {weapon.name}");
        }

        // ===========================================================
        // 👁️ 顯示指定武器（index），會自動隱藏上一把
        // ===========================================================
        public void ShowWeapon(int index)
        {
            if (index < 0 || index >= weaponList.Count)
            {
                Debug.LogWarning($"⚠️ [ShowWeapon] index {index} 超出範圍");
                return;
            }

            // 1️⃣ 若目前有顯示中的武器 → 先隱藏
            if (currentWeaponIndex >= 0 && currentWeaponIndex < weaponList.Count)
            {
                HideWeapon();
            }

            // 2️⃣ 顯示新武器
            Rigidbody rb = weaponList[index];
            if (rb == null)
            {
                Debug.LogWarning($"⚠️ [ShowWeapon] Weapon[{index}] 為 null");
                return;
            }

            hiddenWeapon = rb;
            currentWeaponIndex = index;

            // 啟用渲染與碰撞
            foreach (var r in rb.GetComponentsInChildren<Renderer>(true))
                r.enabled = true;
            foreach (var c in rb.GetComponentsInChildren<Collider>(true))
                c.enabled = true;

            // 對齊位置與旋轉
            rb.transform.position = transform.position;
            rb.transform.rotation = transform.rotation * Quaternion.Euler(0, 0, -90);
            Physics.SyncTransforms();

            // ✅ 延遲一幀再重綁 Joint（確保物理穩定）
            StartCoroutine(RebindAfterFixed(rb));
        }

        private IEnumerator RebindAfterFixed(Rigidbody rb)
        {
            yield return new WaitForFixedUpdate();
            if (this == null || rb == null) yield break;

            // 3️⃣ 重建 Joint
            var fj = gameObject.AddComponent<FixedJoint>();
            fj.breakForce = Mathf.Infinity;
            fj.connectedBody = rb;

            catchJoint = fj;
            hasJoint = true;
            GrabbedObject = rb;
            hiddenWeapon = rb;

            // ✅ 恢復物理（這是你要求的部分）
            rb.isKinematic = false;
            rb.useGravity = true;
            isWeaponHidden = false;

            // 4️⃣ 通知外部系統
            EventBetter.Raise(new GrabObjectMessage()
            {
                pc = oodlesCharacter,
                hf = this,
                obj = rb.gameObject
            });

            Debug.Log($"🔙 [ShowWeapon] {handSide} re-attached and physics re-enabled for {rb.name}");
        }


        // ===========================================================
        // 🙈 隱藏目前武器（保留在列表中）
        // ===========================================================
        public void HideWeapon()
        {
            if (hiddenWeapon == null || isWeaponHidden) return;

            // 拆 Joint
            if (catchJoint != null)
            {
                catchJoint.breakForce = 0;
                catchJoint.connectedBody = null;
                Destroy(catchJoint);
                catchJoint = null;
            }

            var rb = hiddenWeapon;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;

            // 關閉渲染與碰撞
            foreach (var r in rb.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;
            foreach (var c in rb.GetComponentsInChildren<Collider>(true))
                c.enabled = false;

            // 清除抓取狀態
            GrabbedObject = null;
            hasJoint = false;
            isWeaponHidden = true;




            Debug.Log($"🙈 [HideWeapon] {handSide} 隱藏武器 {rb.name}");
        }

        // ===========================================================
        // 🗑️ 釋放所有武器（可選）
        // ===========================================================
        public void ReleaseAllWeapons()
        {
            foreach (var rb in weaponList)
            {
                if (rb == null) continue;
                Destroy(rb.gameObject);
            }
            weaponList.Clear();
            currentWeaponIndex = -1;
            GrabbedObject = null;
            hasJoint = false;

            Debug.Log($"🗑️ [ReleaseAllWeapons] {handSide} 所有武器已清空。");
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
            catchJoint.breakForce = Mathf.Infinity;
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
                    callback = (bool b) =>
                    {
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