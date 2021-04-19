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
    static int kObjectCount = 1000000;
    static int kAvoidCount = 20;
    public static sprite_data_t[] sprite_data = new sprite_data_t[kMaxSpriteCount];

    public static void game_initialize()
    {
        // create "world bounds" object
        {
            GameObject go = new GameObject("bounds");
            go.AddComponent(new WorldBoundsComponent
            {
                xMin = -80.0f,
                xMax = 80.0f,
                yMin = -50.0f,
                yMax = 50.0f
            });
            s_Objects.Add(go);
        }
        WorldBoundsComponent bounds = FindOfType<WorldBoundsComponent>();

        // create regular objects that move
        for (int i = 0; i < kObjectCount; ++i)
        {
            GameObject go = new GameObject("object");

            // position it within world bounds
            PositionComponent pos = new PositionComponent();
            pos.x = RandomFloat(bounds.xMin, bounds.xMax);
            pos.y = RandomFloat(bounds.yMin, bounds.yMax);
            go.AddComponent(pos);

            // setup a sprite for it (random sprite index from first 5), and initial white color
            SpriteComponent sprite = new SpriteComponent();
            sprite.colorR = 1.0f;
            sprite.colorG = 1.0f;
            sprite.colorB = 1.0f;
            sprite.spriteIndex = rnd.Next() % 5;
            sprite.scale = 1.0f;
            go.AddComponent(sprite);

            // make it move
            MoveComponent move = new MoveComponent(0.5f, 0.7f);
            go.AddComponent(move);

            // make it avoid the bubble things
            AvoidComponent avoid = new AvoidComponent();
            go.AddComponent(avoid);

            s_Objects.Add(go);
        }

        // create objects that should be avoided
        for (int i = 0; i < kAvoidCount; ++i)
        {
            GameObject go = new GameObject("toavoid");

            // position it in small area near center of world bounds
            PositionComponent pos = new PositionComponent();
            pos.x = RandomFloat(bounds.xMin, bounds.xMax) * 0.2f;
            pos.y = RandomFloat(bounds.yMin, bounds.yMax) * 0.2f;
            go.AddComponent(pos);

            // setup a sprite for it (6th one), and a random color
            SpriteComponent sprite = new SpriteComponent();
            sprite.colorR = RandomFloat(0.5f, 1.0f);
            sprite.colorG = RandomFloat(0.5f, 1.0f);
            sprite.colorB = RandomFloat(0.5f, 1.0f);
            sprite.spriteIndex = 5;
            sprite.scale = 2.0f;
            go.AddComponent(sprite);

            // make it move, slowly
            MoveComponent move = new MoveComponent(0.1f, 0.2f);
            go.AddComponent(move);

            // setup an "avoid this" component
            AvoidThisComponent avoid = new AvoidThisComponent();
            avoid.distance = 1.3f;
            go.AddComponent(avoid);

            s_Objects.Add(go);
        }

        // call Start on all objects/components once they are all created
        for (int i = 0, size = s_Objects.Count; i < size; i++)
        {
            s_Objects[i].Start();
        }
    }

    public static void game_destroy()
    {
        s_Objects.Clear();
    }

    public static int game_update(sprite_data_t[] data, double time, float deltaTime)
    {
        int objectCount = 0;
        // go through all objects
        for (int i = 0, size = s_Objects.Count; i < size; i++)
        {
            GameObject go = s_Objects[i];
            // Update all their components
            go.Update(time, deltaTime);

            // For objects that have a Position & Sprite on them: write out
            // their data into destination buffer that will be rendered later on.
            //
            // Using a smaller global scale "zooms out" the rendering, so to speak.
            float globalScale = 0.05f;
            PositionComponent pos = go.GetComponent<PositionComponent>();
            SpriteComponent sprite = go.GetComponent<SpriteComponent>();
            if (pos != null && sprite != null)
            {
                sprite_data_t spr = data[objectCount++];
                spr.posX = pos.x * globalScale;
                spr.posY = pos.y * globalScale;
                spr.scale = sprite.scale * globalScale;
                spr.colR = sprite.colorR;
                spr.colG = sprite.colorG;
                spr.colB = sprite.colorB;
                spr.sprite = (float)sprite.spriteIndex;
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