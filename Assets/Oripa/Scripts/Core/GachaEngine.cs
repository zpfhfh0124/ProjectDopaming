using System;
using System.Collections.Generic;

namespace Oripa
{
    /// <summary>
    /// 추첨 로직 (HTML 프로토타입의 roll/draw/oddsOf/rtp 와 같은 규칙).
    /// UnityEngine에 의존하지 않아 서버 코드로 옮기기 쉽다.
    /// </summary>
    public sealed class GachaEngine
    {
        readonly Random rng;

        public GachaEngine(Random rng = null)
        {
            this.rng = rng ?? new Random();
        }

        public static bool CanDraw(PackDefinition pack, PlayerState state, int count) =>
            state.GetPack(pack).left >= count && state.points >= (long)pack.Price * count;

        public DrawStatus TryDraw(PackDefinition pack, PlayerState state, int count, out List<PrizeCard> results)
        {
            results = new List<PrizeCard>();
            var ps = state.GetPack(pack);
            if (ps.left < count) return DrawStatus.SoldOut;
            if (state.points < (long)pack.Price * count) return DrawStatus.NotEnoughPoints;

            state.points -= (long)pack.Price * count;
            for (int i = 0; i < count; i++)
            {
                var g = Roll(pack, ps);
                if (ps.stock != null) ps.stock[(int)g]--;
                ps.left--;
                var card = new PrizeCard
                {
                    uid = ++state.nextUid,
                    packId = pack.Id,
                    packName = pack.Name,
                    grade = g,
                    value = Math.Max(0, pack.Value[(int)g]),
                };
                state.prizes.Add(card);
                results.Add(card);
            }
            state.totalPulls += count;
            return DrawStatus.Success;
        }

        /// <summary>등급 하나를 뽑는다. 재고 방식은 남은 재고 비율, 확률 방식은 확률표를 쓴다.</summary>
        public Grade Roll(PackDefinition pack, PackState ps)
        {
            if (ps.stock != null)
            {
                int total = 0;
                foreach (var n in ps.stock) total += n;
                int y = rng.Next(Math.Max(1, total));
                foreach (var g in GradeInfo.DisplayOrder)
                {
                    y -= ps.stock[(int)g];
                    if (y < 0) return g;
                }
                foreach (var g in GradeInfo.DisplayOrder)
                    if (ps.stock[(int)g] > 0) return g;
                return Grade.D;
            }

            double x = rng.NextDouble() * 100.0;
            foreach (var g in GradeInfo.DisplayOrder)
            {
                x -= pack.Odds[(int)g];
                if (x < 0) return g;
            }
            return Grade.C; // HTML과 동일한 기본값
        }

        /// <summary>등급별 현재 확률(%). 재고 방식은 남은 수량 기준.</summary>
        public static double[] OddsOf(PackDefinition pack, PlayerState state)
        {
            var result = new double[GradeInfo.Count];
            var ps = state.GetPack(pack);
            if (ps.stock == null)
            {
                Array.Copy(pack.Odds, result, GradeInfo.Count);
                return result;
            }
            int t = Math.Max(1, ps.left);
            for (int i = 0; i < GradeInfo.Count; i++) result[i] = ps.stock[i] * 100.0 / t;
            return result;
        }

        /// <summary>환원율(%) = 기대 포인트 가치 ÷ 1회 가격.</summary>
        public static double Rtp(PackDefinition pack, PlayerState state)
        {
            var odds = OddsOf(pack, state);
            double expected = 0;
            for (int i = 0; i < GradeInfo.Count; i++)
                expected += odds[i] / 100.0 * Math.Max(0, pack.Value[i]);
            return expected / pack.Price * 100.0;
        }

        /// <summary>보유 카드를 포인트로 전환한다.</summary>
        public static bool Sell(PlayerState state, int uid)
        {
            int i = state.prizes.FindIndex(c => c.uid == uid);
            if (i < 0) return false;
            state.points += state.prizes[i].value;
            state.prizes.RemoveAt(i);
            return true;
        }
    }
}
