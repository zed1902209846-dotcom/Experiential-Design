using System.IO;
using SolarSystemAR;
using UnityEditor;
using UnityEngine;

namespace SolarSystemAR.EditorBuildScripts
{
    /// 生成 10 个天体科普数据资产。菜单：Solar System > Setup > Create Celestial Body Data (10)
    /// 数据参考：NASA Planetary Fact Sheet。
    public static class CelestialDataBuilder
    {
        const string Dir = "Assets/Data/CelestialBodies";

        [MenuItem("Solar System/Setup/Create Celestial Body Data (10)")]
        public static void CreateAll()
        {
            if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);

            Create("sun", "太阳", "The Sun",
                "太阳是太阳系的中心恒星，占系统总质量的 99.86%，其核聚变为整个系统提供光与热。",
                "The Sun is the central star of the Solar System, holding 99.86% of its total mass and powering everything with nuclear fusion.",
                "太阳是一颗 G 型主序星（黄矮星），核心温度约 1500 万摄氏度，每秒将约 6 亿吨氢聚变为氦。它诞生于约 46 亿年前的分子云坍缩，目前正处于主序星阶段的壮年，预计还能稳定燃烧约 50 亿年。",
                "The Sun is a G-type main-sequence star with a core temperature of about 15 million °C, fusing roughly 600 million tons of hydrogen into helium every second. Born about 4.6 billion years ago, it will remain stable for roughly another 5 billion years.",
                "1,392,700 km", "~25.4 days (equator)", "~230 million years around the galaxy", "~4.6 billion years",
                "G-type main-sequence star; surface ~5,500°C; photosphere/chromosphere/corona layers; periodic sunspots and flares");

            Create("mercury", "水星", "Mercury",
                "水星是距太阳最近、体积最小的行星，表面布满陨石坑，昼夜温差超过 600°C。",
                "Mercury is the smallest planet closest to the Sun, crater-covered, with day-night temperature swings over 600°C.",
                "水星几乎没有大气层，白天可达 430°C，夜晚降至 -180°C。它拥有巨大的铁核（约占半径 85%）和微弱的磁场，表面外观类似月球。由于轨道离心率大，一个太阳日长达 176 个地球日。",
                "Mercury has almost no atmosphere; days reach 430°C while nights drop to -180°C. A huge iron core (about 85% of its radius) and a weak magnetic field define it. Its eccentric orbit makes one solar day last 176 Earth days.",
                "4,879 km", "58.6 days", "88 days", "~4.6 billion years",
                "No atmosphere; huge iron core; extreme day-night temperature swings; fastest orbital speed");

            Create("venus", "金星", "Venus",
                "金星被浓密的二氧化碳大气包裹，是太阳系最热的行星，表面约 465°C。",
                "Venus is the hottest planet, wrapped in dense CO2 atmosphere with a surface near 465°C.",
                "金星的极端温室效应使其表面温度高于水星，气压约为地球的 92 倍。浓硫酸云层覆盖全球。它自转方向与众不同（逆向），且自转一周比公转一周还慢，一“天”比一“年”更长。",
                "Venus suffers a runaway greenhouse effect hotter than Mercury, with surface pressure 92x Earth's. Global sulfuric-acid clouds cover it. It rotates backwards and slower than it orbits - a Venusian day outlasts its year.",
                "12,104 km", "243 days (retrograde)", "225 days", "~4.6 billion years",
                "Runaway greenhouse effect; sulfuric-acid clouds; retrograde rotation; surface pressure ~92x Earth's");

            Create("earth", "地球", "Earth",
                "地球是目前已知唯一存在生命的行星，表面 71% 被液态水覆盖。",
                "Earth is the only known life-bearing planet, with liquid water covering 71% of its surface.",
                "地球拥有氮氧为主的大气层、全球磁场与活跃的板块运动，三者共同保护并塑造了生命环境。月球作为天然卫星稳定了地轴倾角，使气候长期保持相对稳定。",
                "Earth combines a nitrogen-oxygen atmosphere, a global magnetic field and active plate tectonics that protect and shape life. Its moon stabilizes axial tilt, keeping climate relatively stable over long timescales.",
                "12,756 km", "23 h 56 min", "365.25 days", "~4.6 billion years",
                "Liquid-water oceans; oxygen-rich atmosphere; global magnetic field; only confirmed life-bearing world");

            Create("moon", "月球", "The Moon",
                "月球是地球唯一的天然卫星，因潮汐锁定永远以同一面朝向地球。",
                "The Moon is Earth's only natural satellite, tidally locked so the same face always points at us.",
                "月球表面覆盖月壤，正面的月海由远古火山活动形成的玄武岩平原构成。它以每年约 3.8 厘米的速度缓慢远离地球；月相变化源于日、地、月三者的几何关系。",
                "The Moon's surface is blanketed by regolith; its near-side maria are ancient volcanic basalt plains. It drifts about 3.8 cm farther from Earth each year, and its phases come from the Sun-Earth-Moon geometry.",
                "3,475 km", "27.3 days (synchronous)", "27.3 days around Earth", "~4.5 billion years",
                "Tidally locked; almost no atmosphere; maria and highlands; slowly drifting away from Earth");

            Create("mars", "火星", "Mars",
                "火星因表面氧化铁呈红色，拥有太阳系最高的火山与最长的峡谷，是人类探索重点。",
                "Mars looks red from iron oxide, hosts the tallest volcano and longest canyon, and is a prime exploration target.",
                "火星拥有稀薄的二氧化碳大气与季节性极冠。奥林帕斯山高约 22 公里，水手号峡谷长约 4000 公里。大量证据显示远古火星曾有液态水，多个探测器正在持续寻找生命痕迹。",
                "Mars has a thin CO2 atmosphere and seasonal polar caps. Olympus Mons rises about 22 km and Valles Marineris stretches about 4,000 km. Strong evidence of ancient liquid water drives the ongoing search for life.",
                "6,792 km", "24 h 37 min", "687 days", "~4.6 billion years",
                "Red iron-oxide surface; Olympus Mons volcano; Valles Marineris canyon; water ice present");

            Create("jupiter", "木星", "Jupiter",
                "木星是太阳系最大的行星，质量是其他行星总和的 2.5 倍，大红斑风暴已持续数百年。",
                "Jupiter is the largest planet - 2.5x the mass of all others combined - with a centuries-old Great Red Spot.",
                "木星是气态巨行星，主要由氢和氦组成，没有固体表面。它拥有 90 多颗卫星，四颗伽利略卫星各具特色。强大的磁场与带状气流驱动着持续数百年的大红斑风暴。",
                "Jupiter is a gas giant of mostly hydrogen and helium with no solid surface. It has 90+ moons, the four Galilean moons each unique. Its powerful magnetic field and banded jets drive the centuries-old Great Red Spot.",
                "142,984 km", "9 h 56 min", "11.86 years", "~4.6 billion years",
                "Gas giant; Great Red Spot storm; 90+ moons; gravitational 'vacuum cleaner' of the Solar System");

            Create("saturn", "土星", "Saturn",
                "土星以壮观的冰质环系闻名，环直径约 28 万公里，平均厚度却仅约 10 米。",
                "Saturn is famous for its icy rings - about 280,000 km wide yet only ~10 m thick on average.",
                "土星平均密度低于水，是太阳系中最“轻”的行星。其环系由无数冰粒与岩屑组成。最大的卫星土卫六（泰坦）拥有浓密大气与液态甲烷湖泊，被视为生命候选地之一。",
                "Saturn's average density is lower than water - the least dense planet. Its rings are countless ice grains and rock debris. Its largest moon Titan has a thick atmosphere and methane lakes, a candidate for life.",
                "120,536 km", "10 h 42 min", "29.4 years", "~4.6 billion years",
                "Icy ring system; less dense than water; Titan has methane lakes; hexagonal polar storm");

            Create("uranus", "天王星", "Uranus",
                "天王星自转轴倾角约 98°，几乎是“躺着”绕太阳滚动，呈淡蓝色。",
                "Uranus spins on its side with a 98° axial tilt, rolling around the Sun in pale blue.",
                "天王星是冰巨行星，大气中的甲烷吸收红光使其呈青蓝色。极端的轴倾角使其两极各有约 42 年的极昼与极夜。它拥有 13 条暗淡环带和 20 多颗以文学人物命名的卫星。",
                "Uranus is an ice giant; methane in its atmosphere absorbs red light, giving a cyan hue. Its extreme tilt gives each pole ~42 years of daylight and night. It has 13 faint rings and 20+ moons named after literary characters.",
                "51,118 km", "17 h 14 min (retrograde)", "84 years", "~4.6 billion years",
                "98° axial tilt; methane-blue color; ice giant; 13 faint rings");

            Create("neptune", "海王星", "Neptune",
                "海王星是距太阳最远的行星，拥有太阳系最强的风暴，风速可达约 2100 km/h。",
                "Neptune is the farthest planet, with the fastest winds in the Solar System - up to ~2,100 km/h.",
                "海王星由数学计算预言后观测证实，被称为“笔尖上发现的行星”。它呈深蓝色，风暴系统不断生成与消散。最大的卫星海卫一逆行绕转，可能是被捕获的柯伊伯带天体。",
                "Neptune was predicted mathematically before observation - discovered 'on the tip of a pen'. Deep blue with ever-changing storm systems, its largest moon Triton orbits backwards, likely a captured Kuiper Belt object.",
                "49,528 km", "16 h 6 min", "164.8 years", "~4.6 billion years",
                "Fastest winds; deep blue color; predicted mathematically; Triton's retrograde orbit");

            AssetDatabase.SaveAssets();
            Debug.Log("CelestialDataBuilder: 10 个天体数据已生成于 " + Dir);
        }

        static void Create(string id, string cn, string en, string shortCn, string shortEn,
            string fullCn, string fullEn, string diameter, string rotation, string orbital,
            string age, string characteristics)
        {
            var path = Dir + "/" + id + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<CelestialBodyData>(path);
            bool isNew = asset == null;
            if (isNew) asset = ScriptableObject.CreateInstance<CelestialBodyData>();
            asset.id = id;
            asset.displayName = cn;
            asset.englishName = en;
            asset.shortDescription = shortCn;
            asset.shortDescriptionEn = shortEn;
            asset.fullDescription = fullCn;
            asset.fullDescriptionEn = fullEn;
            asset.diameter = diameter;
            asset.rotationPeriod = rotation;
            asset.orbitalPeriod = orbital;
            asset.age = age;
            asset.characteristics = characteristics;
            if (isNew) AssetDatabase.CreateAsset(asset, path);
            else EditorUtility.SetDirty(asset);
        }
    }
}
