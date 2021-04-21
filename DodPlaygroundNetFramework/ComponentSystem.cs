using System;
using System.Collections.Generic;
using System.Diagnostics;
using GameObjectVector = System.Collections.Generic.List<World.GameObject>;

public class Utility
{
    public static Random rnd = new Random();
    public static float RandomFloat01() { return (float)rnd.NextDouble(); }
    public static float RandomFloat(float from, float to) { return RandomFloat01() * (to - from) + from; }
}

class World
{
    // components we use in our "game". these are all just simple structs with some data.
    public class PositionComponent
    {
        public float x, y;
    }
    public class SpriteComponent
    {
        public float colorR, colorG, colorB;
        public int spriteIndex;
        public float scale;
    }

    public class WorldBoundsComponent
    {
        public float xMin, xMax, yMin, yMax;
    }

    public class MoveComponent
    {
        public float velx, vely;

        public void Initialize(float minSpeed, float maxSpeed)
        {
            // random angle
            float angle = Utility.RandomFloat01() * 3.1415926f * 2;
            // random movement speed between given min & max
            float speed = Utility.RandomFloat(minSpeed, maxSpeed);
            // velocity x & y components
            velx = (float)Math.Cos(angle) * speed;
            vely = (float)Math.Sin(angle) * speed;
        }
    }

    // super simple "game object system". each object has data for all possible components,
    // as well as flags indicating which ones are actually present.
    [Flags]
    public enum ComponentFlags : byte
    {
        kPosition = 0,
        kSprite = 1 << 0,
        kWorldBounds = 1 << 1,
        kMove = 1 << 2
    }


    public struct GameObject
    {
        public string m_Name;
        public PositionComponent m_Position;
        public SpriteComponent m_Sprite;
        public WorldBoundsComponent m_WorldBounds;
        public MoveComponent m_Move;
        // flags for every component, indicating whether this object "has it"
        public ComponentFlags m_ComponentFlags;

        public GameObject(string name)
        {
            m_Name = name;
            m_ComponentFlags = 0;
            m_Position = new PositionComponent();
            m_Sprite = new SpriteComponent();
            m_WorldBounds = new WorldBoundsComponent();
            m_Move = new MoveComponent();
        }
    }

    private static GameObjectVector s_Objects = new GameObjectVector();
    // -------------------------------------------------------------------------------------------------
    // "systems" that we have; they operate on components of game objects

    public class MoveSystem
    {
        int boundsId; // Id of Object with world bounds
        List<int> entities = new List<int>(); // Ids of objects to move

        public void AddObjectToSystem(int entity)
        {
            entities.Add(entity);
        }

        public void SetBounds(int entity)
        {
            boundsId = entity;
        }

        public void UpdateSystem(double time, float deltaTime)
        {
            WorldBoundsComponent bounds = S_Objects[boundsId].m_WorldBounds;

            for(int i = 0, n = entities.Count; i < n; i++)
            {
                PositionComponent pos = S_Objects[i].m_Position;
                MoveComponent move = S_Objects[i].m_Move;

                // update position based on movement velocity & delta time
                pos.x += move.velx * deltaTime;
                pos.y += move.vely * deltaTime;

                // check against world bounds; put back onto bounds and mirror the velocity component to "bounce" back
                if (pos.x < bounds.xMin)
                {
                    move.velx = -move.velx;
                    pos.x = bounds.xMin;
                }
                if (pos.x > bounds.xMax)
                {
                    move.velx = -move.velx;
                    pos.x = bounds.xMax;
                }
                if (pos.y < bounds.yMin)
                {
                    move.vely = -move.vely;
                    pos.y = bounds.yMin;
                }
                if (pos.y > bounds.yMax)
                {
                    move.vely = -move.vely;
                    pos.y = bounds.yMax;
                }
            }
        }
    }//MoveSystem
    public static MoveSystem s_MoveSystem = new MoveSystem();

    // "Avoidance system" works out interactions between objects that "avoid" and "should be avoided".
    // Objects that avoid:
    // - when they get closer to things that should be avoided than the given distance, they bounce back,
    // - also they take sprite color from the object they just bumped into

    public class AvoidanceSystem
    {
        // things to be avoided: distances to them, and their IDs
        private List<float> avoidDistanceList = new List<float>();
        private List<int> avoidList = new List<int>();
        // objects that avoid: their position components
        private List<int> objectList = new List<int>();

        public void AddAvoidThisObjectToSystem(int id, float distance)
        {
            avoidDistanceList.Add(distance);
            avoidList.Add(id);
        }

        public void AddObjectToSystem(int id)
        {
            objectList.Add(id);
        }

        public static float DistanceSq(PositionComponent a, PositionComponent b)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            return dx * dx + dy * dy;
        }

        void ResolveCollision(GameObject obj, float deltaTime)
        {
            PositionComponent pos = obj.m_Position;
            MoveComponent move = obj.m_Move;
            // flip velocity
            move.velx = -move.velx;
            move.vely = -move.vely;

            // move us out of collision, by moving just a tiny bit more than we'd normally move during a frame
            pos.x += move.velx * deltaTime * 1.1f;
            pos.y += move.vely * deltaTime * 1.1f;
        }

        public void UpdateSystem(double time, float deltaTime)
        {
            int avoidCount = avoidList.Count;
            // go through all objects
            for (int i = 0, size = objectList.Count; i < size; i++)
            {
                GameObject obj = S_Objects[avoidList[i]];
                PositionComponent myPosition = obj.m_Position;

                // Check each thing to avoid
                for (int ia = 0; ia < avoidCount; ia++)
                {
                    float avDistance = avoidDistanceList[ia];
                    GameObject objToAvoid = S_Objects[avoidList[ia]];
                    PositionComponent avPosition = objToAvoid.m_Position;

                    if (DistanceSq(myPosition, avPosition) < avDistance * avDistance)
                    {
                        ResolveCollision(objToAvoid, deltaTime);

                        SpriteComponent avoidSprite = objToAvoid.m_Sprite;
                        SpriteComponent mySprite = obj.m_Sprite;
                        mySprite.colorR = avoidSprite.colorR;
                        mySprite.colorG = avoidSprite.colorG;
                        mySprite.colorB = avoidSprite.colorB;
                    }
                }
            }
        }
    }//AvoidanceSystem
    public static AvoidanceSystem s_AvoidanceSystem = new AvoidanceSystem();

    internal static GameObjectVector S_Objects { get => s_Objects; set => s_Objects = value; }
}