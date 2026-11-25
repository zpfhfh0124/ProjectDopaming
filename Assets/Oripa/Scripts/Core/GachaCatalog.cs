using System.Collections.Generic;

namespace Oripa
{
    /// <summary>
    /// 팩 마스터 데이터. HTML 프로토타입(카드 뽑기 프로토타입.html)의 PACKS 값을 그대로 옮겼다.
    /// 나중에는 서버나 ScriptableObject에서 읽어 오면 된다.
    /// </summary>
    public sealed class GachaCatalog
    {
        public readonly List<PackDefinition> Packs = new List<PackDefinition>();

        public PackDefinition GetPack(int id)
        {
            foreach (var p in Packs) if (p.Id == id) return p;
            return null;
        }

        static int[] Grades(int s = -1, int a = -1, int b = -1, int c = -1, int d = -1) => new[] { d, c, b, a, s };

        public static GachaCatalog CreateDefault()
        {
            var cat = new GachaCatalog();

            cat.Packs.Add(new PackDefinition
            {
                Id = 1, Category = "포켓몬", Name = "오리지널 팩", Art = "pack1",
                Price = 100, Total = 2084, ThemeColor = "#2f5bff",
                Stock = Grades(s: 100, d: 1984, a: 0, b: 0, c: 0),
                Value = Grades(s: 1000, d: 50),
            });

            cat.Packs.Add(new PackDefinition
            {
                Id = 2, Category = "포켓몬", Name = "마일드 오리파", Art = "pack2",
                Price = 10000, Total = 667, ThemeColor = "#6b3fd6",
                Stock = Grades(s: 100, d: 567, a: 0, b: 0, c: 0),
                Value = Grades(s: 50000, d: 2400),
            });

            cat.Packs.Add(new PackDefinition
            {
                Id = 3, Category = "포켓몬", Name = "하이클래스 오리파", Art = "pack3",
                Price = 300, Total = 500, ThemeColor = "#0f8a6a",
                Odds = new double[] { 0, 60, 30, 8, 2 }, // D, C, B, A, S
                Value = Grades(s: 5000, a: 900, b: 250, c: 60),
            });

            cat.Packs.Add(new PackDefinition
            {
                Id = 4, Category = "포켓몬", Name = "전설의 포켓몬", Art = "pack4",
                Price = 250000, Total = 90, ThemeColor = "#b4291f",
                Stock = Grades(s: 1, a: 5, b: 24, d: 60, c: 0),
                Value = Grades(s: 1800000, a: 1000000, b: 500000, d: 6000),
            });

            return cat;
        }
    }
}
