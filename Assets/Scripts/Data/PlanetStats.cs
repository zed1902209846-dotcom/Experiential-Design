using System.Collections.Generic;

namespace SolarSystemAR
{
    /// 知识卡「KEY FACTS」六项数据表。
    /// 顺序固定：ORDER / DIAMETER / DISTANCE / ORBIT / ROTATION / MOONS。
    /// 空字符串表示该行不适用，卡片会自动整行隐藏。
    public static class PlanetStats
    {
        private static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
        {
            { "mercury", new[] { "1st planet", "4,879 km", "57.9 million km", "88 days", "58.6 days", "0" } },
            { "venus",   new[] { "2nd planet", "12,104 km", "108.2 million km", "224.7 days", "243 days (retrograde)", "0" } },
            { "earth",   new[] { "3rd planet", "12,742 km", "149.6 million km", "365.25 days", "23 h 56 m", "1 (the Moon)" } },
            { "mars",    new[] { "4th planet", "6,779 km", "227.9 million km", "687 days", "24 h 37 m", "2 (Phobos & Deimos)" } },
            { "jupiter", new[] { "5th planet", "139,820 km", "778.5 million km", "11.9 years", "9 h 56 m", "95 known" } },
            { "saturn",  new[] { "6th planet", "116,460 km", "1.43 billion km", "29.4 years", "10 h 42 m", "146 known" } },
            { "uranus",  new[] { "7th planet", "50,724 km", "2.87 billion km", "84 years", "17 h 14 m", "28 known" } },
            { "neptune", new[] { "8th planet", "49,244 km", "4.50 billion km", "164.8 years", "16 h 6 m", "16 known" } },
            { "sun",     new[] { "Center star", "1,392,700 km", "", "", "25 Earth days (equator)", "" } },
            { "solarsystem", new[] { "", "~2 light-years (Oort cloud)", "", "230 million years (galactic)", "", "" } }
        };

        /// <summary>返回六项数据；没有该目标时返回 null，卡片回退到 PlanetData 自带字段。</summary>
        public static string[] Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            string[] rows;
            return Table.TryGetValue(id, out rows) ? rows : null;
        }
    }
}
