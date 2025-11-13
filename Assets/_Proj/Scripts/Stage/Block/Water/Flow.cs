using System.Collections;
using UnityEngine;
namespace Water
{
    public class Flow : MonoBehaviour
    {
        private Material waterMat;
        //[SerializeField] float flowTime = 10f;
        public float flowInterval; // 오브젝트 밀어내는 간격
        // Boar에서 참조해서 flow water 속도보다 돌진 재사용 대기시간이 좀 더 있도록 조정해야 함.

        private IFlowStrategy flowStrategy = new FlowWaterStrategy();

        private Vector3 flowDir = Vector3.forward;

        [Tooltip("밀려날 수 있는 오브젝트 레이어")]
        public LayerMask pushableMask;

        private Coroutine flowCoroutine;

        void Awake()
        {
            // FlowWater 블록은 Water 레이어로 설정. 레이어 설정 실수 방지용.
            if (gameObject.layer != LayerMask.NameToLayer("Water"))
            {
                Debug.LogWarning($"{gameObject.name}'s Layer is not 'Water'");
            }
            Debug.Log($"[{gameObject.name}] Awake() 시점 PushableMask 값: {pushableMask.value}");
            MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
            if (renderer != null)
            {
                waterMat = renderer.material;
            }
        }

        void Start()
        {
            SetFlowDir();
            Debug.Log($"[{gameObject.name}] Start() 시점 PushableMask 값: {pushableMask.value}");
            if (flowCoroutine != null) StopCoroutine(flowCoroutine);
            flowCoroutine = StartCoroutine(FlowObjsCoroutine());
        }

        //public float GetFlowTime()
        //{
        //    return flowTime;
        //}


        public void SetFlowDir()
        {
            // 부모의 Y축 회전값으로 흐름 방향을 계산
            Quaternion parentRot = transform.rotation;
            flowDir = parentRot * Vector3.forward;
            flowDir.y = 0f;
            flowDir.Normalize();
            // KHJ NOTE : 컴포넌트가 root에 붙으므로 transform.rotation으로 변경
            if (waterMat != null)
            {
                waterMat.SetVector("_FlowDir", new(parentRot.x, parentRot.y, parentRot.z, parentRot.w));
            }
        }

        IEnumerator FlowObjsCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(flowInterval);

                Vector3 centre = transform.position;

                // 물타일 중앙에서 PushableMask 레이어를 가진 물체를 감지
                float checkSize = 0.45f;

                // 물 타일 높이의 중앙보다 살짝 위에서 검사
                // 검사 영역은 물 타일 높이를 포함. Y좌표 검증을 통해 걸러내기
                Collider[] hits = Physics.OverlapBox(centre + Vector3.up * 0.5f, Vector3.one * checkSize, Quaternion.identity, pushableMask);

                // 물 타일의 y 좌표를 기준 높이로 설정
                float flowY = centre.y;

                foreach (var hit in hits)
                {
                    if (hit.TryGetComponent<PushableObjects>(out var pushable))
                    {
                        // 가장 아래층 물체의 y좌표가 물 타일의 y좌표와 일치하는지 확인
                        if (Mathf.Abs(pushable.transform.position.y - flowY) > 0.01f) continue;

                        // 물체가 낙하 중이거나 이미 이동 중이라면 제외
                        if (pushable.IsFalling || pushable.IsMoving) continue;

                        // PushableObjects.cs에서 탑승 로직을 처리하도록
                        // flowDir를 Vector2Int로 변환하여 ImmediatePush를 호출
                        Vector2Int flowDir2D = new Vector2Int(Mathf.RoundToInt(flowDir.x), Mathf.RoundToInt(flowDir.z));

                        flowStrategy.ExecuteFlow(pushable, flowDir2D);
                    }
                }
            }
        }
    }
}