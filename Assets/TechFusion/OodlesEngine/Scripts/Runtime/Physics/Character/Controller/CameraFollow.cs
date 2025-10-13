using TMPro;
using UnityEngine;

namespace OodlesEngine
{
    public class CameraFollow : MonoBehaviour
    {
        [HideInInspector]
        public Transform player;

        public Vector3 positionOffset;

        public LayerMask layerMask = ~0;
        public float spherecastRadius = 0.2f;

        public float distance = 10.0f, rotateSpeed = 5.0f, minAngle = -45.0f, maxAngle = -30.0f;

        [HideInInspector]
        public bool enable = false;

        private Camera mainCamera;
        private float currentX = 0.0f, currentY = 0.0f;

        private static CameraFollow instance = null;

        public static CameraFollow Get()
        {
            return instance;
        }

        private void Awake()
        {
            if (instance)
            {
                return;
            }

            instance = this;
        }

        void Start()
        {
            mainCamera = Camera.main;
        }

        void Update()
        {
            if (!enable) return;

            ProcessInput(Time.deltaTime);
            UpdateControl();
        }

        public void ProcessInput(float deltaTime)
        {
            currentX = currentX + InputManager.Get().GetTouchMoveX() * rotateSpeed * deltaTime;
            currentY = currentY + InputManager.Get().GetTouchMoveY() * rotateSpeed * deltaTime;

            currentY = Mathf.Clamp(currentY, minAngle, maxAngle);
        }

        // 加在 class CameraFollow 裡
        private Vector3 smoothedTarget;   // 平滑位置
        [SerializeField] private float smoothSpeed = 10f; // 越大越快靠近

        public void UpdateControl()
        {
            if (!player)
                return;

            Vector3 direction = new Vector3(0, 0, distance);
            Quaternion rotation = Quaternion.Euler(-currentY, -currentX, 0);

            // 玩家實際位置
            Vector3 rawTarget = player.position + positionOffset;

            // 平滑過渡 (防止相機跟隨 jitter)
            smoothedTarget = Vector3.Lerp(smoothedTarget, rawTarget, Time.deltaTime * smoothSpeed);

            // 計算相機「理想站位」
            Vector3 standPosition = smoothedTarget + rotation * direction;

            // 碰撞檢查，避免穿牆
            float computedDistance = GetDistanceByObstacle(standPosition, smoothedTarget);
            direction = direction.normalized * computedDistance;

            // 更新相機位置與朝向
            mainCamera.transform.position = smoothedTarget + rotation * direction;
            mainCamera.transform.LookAt(smoothedTarget);

            ApplyOccluderFade(mainCamera.transform.position, smoothedTarget);
        }


        float GetDistanceByObstacle(Vector3 position, Vector3 target)
        {
            RaycastHit hit;

            Vector3 castDirection = position - target;

            if (Physics.SphereCast(new Ray(target, castDirection), spherecastRadius, out hit, castDirection.magnitude, layerMask, QueryTriggerInteraction.Ignore))
            {
                return hit.distance;
            }

            return distance;
        }

        void ApplyOccluderFade(Vector3 position, Vector3 target)
        {
            RaycastHit hit;

            Vector3 castDirection = target - position;

            if (Physics.Raycast(position, castDirection, out hit, castDirection.magnitude))
            {
                FadeObject fade = hit.collider.gameObject.GetComponent<FadeObject>();
                if (fade != null)
                {
                    fade.ApplyFadeThisFrame();
                }
            }

            //if (Physics.SphereCast(new Ray(target, castDirection), spherecastRadius, out hit, castDirection.magnitude))
            //{
            //    FadeObject fade = hit.collider.gameObject.GetComponent<FadeObject>();
            //    if (fade != null)
            //    {
            //        fade.ApplyFadeThisFrame();
            //    }
            //}
        }
    }
}
