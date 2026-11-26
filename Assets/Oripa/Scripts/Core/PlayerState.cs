using System;
using System.Collections.Generic;

namespace Oripa
{
    /// <summary>팩별 남은 수량과 재고.</summary>
    [Serializable]
    public class PackState
    {
        public int id;
        public int left;
        public int[] stock; // Stock 모드만 사용
    }

    /// <summary>
    /// 플레이어 저장 데이터 (JsonUtility 직렬화용 public 필드).
    /// 출시 버전에서는 포인트, 재고, 추첨을 모두 서버가 관리해야 한다.
    /// </summary>
    [Serializable]
    public class PlayerState
    {
        public const int StartingPoints = 5000;

        public long points = StartingPoints;

        /// <summary>헤더 오른쪽 위에 표시하는 코인 (골드 최대 5자리, 실버·브론즈 최대 2자리). 아직 얻는 방법은 없다.</summary>
        public int gold;
        public int silver;
        public int bronze;
        public int nextUid;
        public int totalPulls;
        public List<PrizeCard> prizes = new List<PrizeCard>();
        public List<PackState> packs = new List<PackState>();

        public PackState GetPack(PackDefinition def)
        {
            foreach (var p in packs)
            {
                if (p.id != def.Id) continue;
                // JsonUtility는 null 배열을 빈 배열(길이 0)로 저장/복원한다.
                // 확률 방식 팩은 stock이 null이어야 하므로 불러온 뒤 다시 맞춘다.
                if (def.Stock == null)
                    p.stock = null;
                else if (p.stock == null || p.stock.Length != GradeInfo.Count)
                    p.stock = (int[])def.Stock.Clone();
                return p;
            }
            var s = new PackState
            {
                id = def.Id,
                left = def.Total,
                stock = def.Stock != null ? (int[])def.Stock.Clone() : null,
            };
            packs.Add(s);
            return s;
        }

        public void Restock(PackDefinition def)
        {
            var s = GetPack(def);
            s.left = def.Total;
            if (def.Stock != null) s.stock = (int[])def.Stock.Clone();
        }
    }
}
