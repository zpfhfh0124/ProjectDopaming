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
        public int nextUid;
        public int totalPulls;
        public List<PrizeCard> prizes = new List<PrizeCard>();
        public List<PackState> packs = new List<PackState>();

        public PackState GetPack(PackDefinition def)
        {
            foreach (var p in packs)
            {
                if (p.id != def.Id) continue;
                if (def.Stock != null && (p.stock == null || p.stock.Length != GradeInfo.Count))
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
