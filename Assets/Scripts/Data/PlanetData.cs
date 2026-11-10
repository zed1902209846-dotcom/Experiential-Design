using System.Collections.Generic;
using UnityEngine;

namespace SolarSystemAR
{
    /// 八大行星科普文案（静态数据，全英文）。
    /// 每个行星四段：What is it? / How Special? / Life / Fun Fact。
    public static class PlanetData
    {
        public class Info
        {
            public string id;        // 与 Vuforia 目标名一致（小写）
            public string display;   // 显示名
            public string tagline;   // 副标题
            public string what;      // 1. What is it?
            public string special;   // 2. How Special?
            public string life;      // 3. Life
            public string fun;       // 4. Fun Fact

            // 资料卡「KEY FACTS」统计表（留空则整行隐藏）
            public string order;     // 太阳系顺序
            public string diameter;  // 直径
            public string distance;  // 日距
            public string orbit;     // 轨道周期
            public string rotation;  // 自转周期
            public string moons;     // 卫星数量
        }

        public static readonly List<Info> All = new List<Info>
        {
            new Info { id="mercury", display="Mercury", tagline="The Swift Planet",
                what="Mercury is the smallest planet in the solar system and the closest to the Sun.",
                special="It has the most extreme temperature swings: from -180C at night to 430C during the day.",
                life="No atmosphere and no liquid water - Mercury cannot support life as we know it.",
                fun="A year on Mercury is only 88 Earth days, but a single day lasts 176 Earth days." },
            new Info { id="venus", display="Venus", tagline="The Morning Star",
                what="Venus is the second planet from the Sun and the hottest planet in the solar system.",
                special="Its thick CO2 atmosphere traps heat at 465C - hot enough to melt lead. It also spins backwards.",
                life="Surface pressure is 92 times Earth's - no life could survive on the surface.",
                fun="Venus is the brightest planet in our night sky, often called the Evening Star." },
            new Info { id="earth", display="Earth", tagline="Our Only Home",
                what="Earth is the third planet from the Sun and the only planet currently known to support life.",
                special="71% of Earth's surface is covered by water, with suitable temperature, atmosphere, and magnetic field providing the perfect environment for life.",
                life="Millions of species of animals and plants, as well as humans, live from tiny bacteria to giant blue whales - life is rich and diverse.",
                fun="Earth has existed for about 4.6 billion years, and its rotation speed is about 1670 kilometers per hour.",
                order="3rd planet", diameter="12,742 km", distance="149.6 million km",
                orbit="365.25 days", rotation="23 h 56 m", moons="1 (the Moon)" },
            new Info { id="mars", display="Mars", tagline="The Red Planet",
                what="Mars is the fourth planet from the Sun, known for its red iron-oxide dust surface.",
                special="It hosts Olympus Mons, the tallest volcano in the solar system - nearly 3 times the height of Mount Everest.",
                life="No confirmed life today, but ancient riverbeds suggest liquid water once flowed on the surface.",
                fun="Mars has two tiny moons, Phobos and Deimos, named after the sons of the Greek war god." },
            new Info { id="jupiter", display="Jupiter", tagline="The Giant",
                what="Jupiter is the fifth planet from the Sun and the largest in the solar system.",
                special="The Great Red Spot is a storm bigger than Earth that has been raging for over 300 years.",
                life="No solid surface exists - it is a gas giant, so life as we know it cannot live there.",
                fun="Jupiter has over 90 known moons, including Ganymede - the largest moon in the solar system." },
            new Info { id="saturn", display="Saturn", tagline="The Ringed Beauty",
                what="Saturn is the sixth planet from the Sun, famous for its spectacular ring system.",
                special="Its rings are made of billions of ice and rock particles, spanning 280,000 km wide but only about 10 meters thick.",
                life="Saturn is a gas giant with no solid surface - no life can exist on it directly.",
                fun="Saturn is so light it would float in water - its density is lower than water." },
            new Info { id="uranus", display="Uranus", tagline="The Sideways Planet",
                what="Uranus is the seventh planet from the Sun, an ice giant with a pale blue-green color.",
                special="It rotates on its side at 98 degrees - likely knocked over by an ancient collision.",
                life="Far too cold (-224C) and no solid surface - Uranus cannot support life.",
                fun="Uranus was the first planet discovered with a telescope, by William Herschel in 1781." },
            new Info { id="neptune", display="Neptune", tagline="The Windy World",
                what="Neptune is the eighth and farthest planet from the Sun, a deep blue ice giant.",
                special="It has the fastest winds in the solar system - up to 2,100 km/h, faster than the speed of sound on Earth.",
                life="Extreme cold and no solid surface make Neptune completely uninhabitable.",
                fun="Neptune was predicted by math before it was seen - astronomers found it through Uranus's orbital wobble in 1846." },
            // 以下两条必须追加在末尾：知识卡顶部 Tab 按 PlanetData.All 的下标映射，
            // 而 tabButtons 只有 8 个，追加在末尾才能保证前 8 个 Tab 的标签与顺序不变。
            new Info { id="sun", display="Sun", tagline="Our Vital Star",
                what="The Sun is our star - the center of the Solar System, an incredibly hot, blazing sphere of plasma.",
                special="Its immense energy, generated by nuclear fusion, powers life on Earth and shapes the entire Solar System. The surface is about 5,500C, while the core reaches roughly 15 million C.",
                life="No life can exist on the Sun itself - the surface is 5,500C and it is made of plasma, not solid matter.",
                fun="The Sun holds 99.8% of all the mass in the Solar System, yet a single day on it lasts about 25 Earth days.",
                order="Center star", diameter="1,392,700 km", distance="149.6 million km",
                rotation="25 Earth days" },
            new Info { id="solarsystem", display="SolarSystem", tagline="Our Cosmic Home",
                what="The Solar System is our gravitational home, centered on a single powerful star: the Sun.",
                special="It contains eight distinct planets, their moons, and countless smaller bodies like dwarf planets and asteroids, all interconnected in eternal orbits.",
                life="Earth is the only place in the Solar System known to host life - from inner rocky worlds to outer gas giants, it is a diverse cosmic neighborhood.",
                fun="The Solar System is roughly one light-year across and takes about 230 million years to orbit the Milky Way once." }
        };

        public static Info Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            id = id.Trim();
            // 中文目标名映射（新数据库目标名为中文）
            switch (id)
            {
                case "水星": id = "mercury"; break;
                case "金星": id = "venus"; break;
                case "地球": id = "earth"; break;
                case "火星": id = "mars"; break;
                case "木星": id = "jupiter"; break;
                case "土星": id = "saturn"; break;
                case "天王星": id = "uranus"; break;
                case "海王星": id = "neptune"; break;
                case "太阳": id = "sun"; break;
                case "太阳系": id = "solarsystem"; break;
            }
            id = id.ToLower();
            // Vuforia 目标名可能带数字后缀（earth1 等），去掉尾部数字再匹配
            id = System.Text.RegularExpressions.Regex.Replace(id, @"\d+$", "");
            return All.Find(p => p.id == id);
        }
    }
}
