using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Inventory
{
    /// <summary>
    /// 카테고리 공통 인벤토리 수량 서비스.
    /// - 키: (PlaceableCategory, id)
    /// - 데코: 수량 운영 (기존 DecoInventoryRuntime 대체)
    /// - 동물/집/배경: 기본 0(관리 안 함). 필요해지면 동일 API로 확장 가능.
    /// - 구키 마이그레이션: "DecoInv_{id}" → "Inv::{cat}::{id}"
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class InventoryService : MonoBehaviour
    {
        public static InventoryService I { get; private set; }

        /// <summary>(cat,id,newCount)</summary>
        // id, newCount 만 통지
        public event Action<int, int> OnChanged;


        //private readonly Dictionary<(PlaceableCategory cat, int id), int> _counts = new();
        private UserData.Inventory inventory => UserData.Local.inventory;
        //private const string NEW_PREFIX = "Inv::";     // 최종: Inv::{cat}::{id}
        //private const string OLD_DECO_PREFIX = "DecoInv_";

        private void Awake()
        {
            if (I && I != this) { Destroy(gameObject); return; }
            I = this;

            
            //_counts.Clear();
            //LoadAllNew();
            //MigrateOldDecoIfAny();
        }

        // ───────────────────────────────────────
        // Public API
        // ───────────────────────────────────────
        public int GetCount(int id)
        {
            return inventory[id];
            //return _counts.TryGetValue((cat, id), out var c) ? c : 0;
        }

        //추가!!! 아이템 들어올 때에 해금 처리도 같이 합니다.
        public void Add(int id, int n = 1)
        {

            if (id <= 0 || n <= 0) return;

            Func<int, int, int, bool> rangeFunc = new((min, max, value) => min < value && value < max);
            int cur = inventory[id];
            int next = cur + n;

            inventory[id] = next;
            OnChanged?.Invoke(id, next);
            inventory.Save();

            //UserData.Local에 직접 추가.
            if (DataManager.Instance.Codex.Value.codexList.Find(x=>x.item_id==id) != null)
            {
                CodexType? type = rangeFunc(10000, 20000, id) ? CodexType.deco :
                    rangeFunc(30000, 40000, id) ? CodexType.animal :
                    rangeFunc(20000, 30000, id) ? CodexType.costume :
                    rangeFunc(40000, 50000, id) ? CodexType.home : null;
                //rangeFunc(40000, 50000, id) ? CodexType. :
                UserData.Local.codex[type, id] = true;
            }

            CodexRedDotManager.Recalculate();
        }

        ////_counts[(cat, id)] = next;
        //OnChanged?.Invoke(cat, id, next);


        //SaveOne(cat, id, next);




        /// <summary>
        /// 인벤토리에서 아이템을 '사용'하는 처리를 시도하는 메서드
        /// </summary>
        /// <param name="cat"></param>
        /// <param name="id"></param>
        /// <param name="n"></param>
        /// <returns>true: 인벤토리에서 아이템을 사용하는 데 성공함. false: 인벤토리에서 아이템을 사용하는 데 실패함.</returns>
        public bool TryConsume(int id, int n = 1)
        {
            if (id <= 0 || n <= 0) return false;

            int cur = inventory[id];
            if (cur < n) return false;

            int next = cur - n;
            inventory[id] = next;

            OnChanged?.Invoke(id, next);
            inventory.Save();
            return true;
        }

        public void Set(int id, int count)
        {
            if (id <= 0 || count < 0) return;

            inventory[id] = count;

            OnChanged?.Invoke(id, count);
            inventory.Save();
        }


        // ───────────────────────────────────────
        // Storage (PlayerPrefs)
        // ───────────────────────────────────────

        //private void LoadAllNew()
        //{
        //    // PlayerPrefs는 전체 열람이 불가 → 주로 런타임에서 Set한 것만 메모리에 유지.
        //    // 새 장치/처음 실행이면 빈 상태에서 시작(데코는 보상 시 Add로 들어옴).
        //    // 필요하면 DB 목록을 순회하여 존재하는 키를 조회하는 방식을 각 패널에서 호출해도 OK.
        //}

        /// <summary>기존 "DecoInv_{id}" 키를 새 키로 이관.</summary>
        //private void MigrateOldDecoIfAny()
        //{
        //    // 안전: DB가 없어도 키만 있으면 이관
        //    // 현실적으로는 불러올 id 범위를 모름 → 대표적으로 1~10000 정도 점검?
        //    // 너무 크면 비효율이라, 실제 프로젝트에선 DB를 순회 권장.
        //    // 여기선 소규모 가정으로 1~4096 범위만 한 번 훑는다.
        //    for (int id = 1; id <= 4096; id++)
        //    {
        //        string oldKey = OLD_DECO_PREFIX + id;
        //        if (!PlayerPrefs.HasKey(oldKey)) continue;

        //        int count = PlayerPrefs.GetInt(oldKey, 0);
        //        _counts[(PlaceableCategory.Deco, id)] = count;
        //        PlayerPrefs.SetInt(Key(PlaceableCategory.Deco, id), count);
        //        PlayerPrefs.DeleteKey(oldKey);
        //    }
        //    PlayerPrefs.Save();
        //}
    }
}
