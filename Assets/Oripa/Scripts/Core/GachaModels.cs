using System;

namespace Oripa
{
    /// <summary>상 등급. 숫자가 클수록 좋은 상이다. 배열 인덱스로도 쓴다.</summary>
    public enum Grade
    {
        D = 0,
        C = 1,
        B = 2,
        A = 3,
        S = 4,
    }

    public static class GradeInfo
    {
        public const int Count = 5;

        /// <summary>표시 순서 (S상부터).</summary>
        public static readonly Grade[] DisplayOrder = { Grade.S, Grade.A, Grade.B, Grade.C, Grade.D };

        public static string Label(Grade g) => g + "상";
    }

    public enum PackMode
    {
        /// <summary>오리파 방식. 등급별 재고가 정해져 있고, 뽑힌 것은 빠진다.</summary>
        Stock,

        /// <summary>등급별 확률(%)로 매번 추첨. 전체 수량만 줄어든다.</summary>
        Odds,
    }

    /// <summary>판매되는 팩(오리파) 하나의 정의.</summary>
    public sealed class PackDefinition
    {
        public int Id;
        public string Category;
        public string Name;
        public int Price;
        public int Total;
        public string ThemeColor = "#2f5bff";

        /// <summary>Resources/PackArt/ 아래 이미지 이름. 없으면 단색 배경에 카테고리 글자.</summary>
        public string Art;

        /// <summary>Stock 모드의 등급별 초기 재고 (인덱스 = (int)Grade). Odds 모드면 null.</summary>
        public int[] Stock;

        /// <summary>Odds 모드의 등급별 확률(%). Stock 모드면 null.</summary>
        public double[] Odds;

        /// <summary>등급별 포인트 가치. 해당 등급이 없으면 -1.</summary>
        public int[] Value = { -1, -1, -1, -1, -1 };

        public PackMode Mode => Stock != null ? PackMode.Stock : PackMode.Odds;

        public bool HasGrade(Grade g) => Value[(int)g] >= 0;
    }

    /// <summary>뽑아서 보관 중인 카드 한 장.</summary>
    [Serializable]
    public class PrizeCard
    {
        public int uid;
        public int packId;
        public string packName;
        public Grade grade;
        public int value;
    }

    public enum DrawStatus
    {
        Success,
        NotEnoughPoints,
        SoldOut,
    }
}
