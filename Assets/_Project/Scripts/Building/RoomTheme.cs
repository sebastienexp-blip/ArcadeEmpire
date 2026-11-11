using UnityEngine;

namespace ArcadeEmpire.Building
{
    /// <summary>Une ambiance de salle : couleurs du sol, des murs, des néons et des lumières.</summary>
    public sealed class RoomTheme
    {
        public string id, name;
        public long price;
        public int minLevel;
        public Color floorA, floorB, wall, ceiling, neonA, neonB, neonAccent, lightA, lightB, ambient;
    }

    public static class RoomThemes
    {
        private static Color C(float r, float g, float b) => new Color(r, g, b);

        public static readonly RoomTheme[] All =
        {
            new RoomTheme { id = "retro80", name = "Rétro années 80", price = 0, minLevel = 1,
                floorA = C(.10f, .10f, .14f), floorB = C(.16f, .12f, .22f), wall = C(.12f, .08f, .20f), ceiling = C(.05f, .05f, .08f),
                neonA = C(1f, .1f, .6f), neonB = C(.1f, .9f, 1f), neonAccent = C(1f, .85f, .1f),
                lightA = C(1f, .2f, .65f), lightB = C(.2f, .9f, 1f), ambient = C(.10f, .09f, .16f) },
            new RoomTheme { id = "neon_future", name = "Néon futuriste", price = 400, minLevel = 3,
                floorA = C(.03f, .04f, .07f), floorB = C(.05f, .08f, .12f), wall = C(.04f, .05f, .10f), ceiling = C(.02f, .02f, .04f),
                neonA = C(.2f, .4f, 1f), neonB = C(.1f, 1f, .8f), neonAccent = C(.7f, .2f, 1f),
                lightA = C(.3f, .4f, 1f), lightB = C(.1f, 1f, .8f), ambient = C(.05f, .07f, .14f) },
            new RoomTheme { id = "family_color", name = "Familiale colorée", price = 600, minLevel = 4,
                floorA = C(1f, .8f, .35f), floorB = C(.4f, .75f, .95f), wall = C(.45f, .85f, .65f), ceiling = C(.9f, .9f, .95f),
                neonA = C(1f, .25f, .3f), neonB = C(.2f, .8f, .3f), neonAccent = C(.3f, .5f, 1f),
                lightA = C(1f, .5f, .4f), lightB = C(.4f, .8f, 1f), ambient = C(.30f, .28f, .30f) },
            new RoomTheme { id = "modern_premium", name = "Moderne premium", price = 800, minLevel = 6,
                floorA = C(.55f, .55f, .58f), floorB = C(.40f, .40f, .44f), wall = C(.75f, .72f, .68f), ceiling = C(.60f, .60f, .62f),
                neonA = C(1f, .95f, .85f), neonB = C(1f, .8f, .4f), neonAccent = C(1f, .75f, .25f),
                lightA = C(1f, .9f, .75f), lightB = C(1f, .85f, .6f), ambient = C(.28f, .27f, .30f) },
            new RoomTheme { id = "japan", name = "Game center japonais", price = 1000, minLevel = 8,
                floorA = C(.22f, .13f, .08f), floorB = C(.30f, .19f, .11f), wall = C(.40f, .06f, .08f), ceiling = C(.08f, .05f, .05f),
                neonA = C(1f, .15f, .2f), neonB = C(1f, .95f, .9f), neonAccent = C(1f, .6f, .8f),
                lightA = C(1f, .3f, .25f), lightB = C(1f, .85f, .7f), ambient = C(.14f, .09f, .09f) },
        };

        public static RoomTheme Find(string id)
        {
            foreach (RoomTheme t in All) if (t.id == id) return t;
            return All[0];
        }
    }
}
