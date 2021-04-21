using System;
using static World;
using static Utility;
using System.Diagnostics;
using System.Windows.Forms;
using DodPlaygroundNetFramework;

struct sprite_data_t
{
    public float posX, posY;
    public float scale;
    public float colR, colB, colG;
    public float sprite;
};

class Program
{
    static int kMaxSpriteCount = 1100000;
    static int kObjectCount = 10000;
    static int kAvoidCount = 20;
    public static sprite_data_t[] sprite_data = new sprite_data_t[kMaxSpriteCount];

    public static void game_initialize()
    {
        // create "world bounds" object
        WorldBoundsComponent bounds;
        {
            GameObject go = new GameObject("bounds");
            go.m_WorldBounds = new WorldBoundsComponent
            {
                xMin = -80.0f,
                xMax = 80.0f,
                yMin = -50.0f,
                yMax = 50.0f
            };
            bounds = go.m_WorldBounds;
            go.m_ComponentFlags |= ComponentFlags.kWorldBounds;
            s_MoveSystem.SetBounds(S_Objects.Count);
            S_Objects.Add(go);
        }

        // create regular objects that move
        for (int i = 0; i < kObjectCount; ++i)
        {
            GameObject go = new GameObject("object");

            // position it within world bounds
            go.m_Position.x = RandomFloat(bounds.xMin, bounds.xMax);
            go.m_Position.y = RandomFloat(bounds.yMin, bounds.yMax);
            go.m_ComponentFlags |= ComponentFlags.kPosition;

            // setup a sprite for it (random sprite index from first 5), and initial white color
            go.m_Sprite.colorR = 1.0f;
            go.m_Sprite.colorG = 1.0f;
            go.m_Sprite.colorB = 1.0f;
            go.m_Sprite.spriteIndex = rnd.Next() % 5;
            go.m_Sprite.scale = 1.0f;
            go.m_ComponentFlags |= ComponentFlags.kSprite;

            // make it move
            go.m_Move.Initialize(0.5f, 0.7f);
            go.m_ComponentFlags |= ComponentFlags.kMove;
            s_MoveSystem.AddObjectToSystem(S_Objects.Count);
            
            // make it avoid the bubble things
            World.s_AvoidanceSystem.AddObjectToSystem(S_Objects.Count);

            S_Objects.Add(go);
        }

        // create objects that should be avoided
        for (int i = 0; i < kAvoidCount; ++i)
        {
            GameObject go = new GameObject("toavoid");

            // position it in small area near center of world bounds
            go.m_Position.x = RandomFloat(bounds.xMin, bounds.xMax) * 0.2f;
            go.m_Position.y = RandomFloat(bounds.yMin, bounds.yMax) * 0.2f;
            go.m_ComponentFlags |= ComponentFlags.kMove;

            // setup a sprite for it (6th one), and a random color
            go.m_Sprite.colorR = RandomFloat(0.5f, 1.0f);
            go.m_Sprite.colorG = RandomFloat(0.5f, 1.0f);
            go.m_Sprite.colorB = RandomFloat(0.5f, 1.0f);
            go.m_Sprite.spriteIndex = 5;
            go.m_Sprite.scale = 2.0f;
            go.m_ComponentFlags |= ComponentFlags.kSprite;

            // make it move, slowly
            go.m_Move.Initialize(0.1f, 0.2f);
            go.m_ComponentFlags |= ComponentFlags.kMove;

            // add to avoidance this as "Avoid This" object
            World.s_AvoidanceSystem.AddAvoidThisObjectToSystem(S_Objects.Count, 1.3f);

            S_Objects.Add(go);
        }
    }

    public static void game_destroy()
    {
        S_Objects.Clear();
    }

    public static int game_update(sprite_data_t[] data, double time, float deltaTime)
    {
        int objectCount = 0;
        // go through all objects
        for (int i = 0, size = S_Objects.Count; i < size; i++)
        {
            GameObject go = S_Objects[i];

            // For objects that have a Position & Sprite on them: write out
            // their data into destination buffer that will be rendered later on.
            //
            // Using a smaller global scale "zooms out" the rendering, so to speak.
            float globalScale = 0.05f;
            if ((go.m_ComponentFlags & ComponentFlags.kPosition & ComponentFlags.kSprite) != 0)
            {
                sprite_data_t spr = data[objectCount++];
                spr.posX = go.m_Position.x * globalScale;
                spr.posY = go.m_Position.y * globalScale;
                spr.scale = go.m_Sprite.scale * globalScale;
                spr.colR = go.m_Sprite.colorR;
                spr.colG = go.m_Sprite.colorG;
                spr.colB = go.m_Sprite.colorB;
                spr.sprite = (float)go.m_Sprite.spriteIndex;
            }
        }
        return objectCount;
    }

    static void Main(string[] args)
    {
        // Debug: This is 10 times faster than C++!!! What!? Why!?
        // Release: About 1.7 times faster than C++
        // Update-Release: About 3 times faster than C++
        Stopwatch sw = new Stopwatch();
        sw.Start();
        game_initialize();
        sw.Stop();
        TimeSpan ts = sw.Elapsed;

        string elapsedTime = $"{ts.TotalMilliseconds}";
        Console.WriteLine($"Initialize Time: {elapsedTime}");

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new DodForm());
    }
}