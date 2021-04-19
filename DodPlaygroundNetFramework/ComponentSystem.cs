

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ComponentVector = System.Collections.Generic.List<Component>;
using GameObjectVector = System.Collections.Generic.List<GameObject>;

public class Utility
{
    public static Random rnd = new Random();
    public static float RandomFloat01() { return (float)rnd.NextDouble(); }
    public static float RandomFloat(float from, float to) { return RandomFloat01() * (to - from) + from; }
}

// C++ dynamic_cast is fairly slow, C#'s is better
// But let's try rolling our own and see what happens
// Each component stores an enum for "what type am I?"
public enum ComponentType
{
    kCompPosition,
    kCompSprite,
    kCompWorldBounds,
    kCompMove,
    kCompAvoid,
    kCompAvoidThis
};

public class Component
{
    private GameObject m_GameObject;
    private ComponentType m_Type;

    protected Component(ComponentType type)
    {
        m_GameObject = null;
        m_Type = type;
    }

    public ComponentType Type => m_Type;

    public virtual void Start() { }
    public virtual void Update(double time, float deltaTime) { }

    public GameObject GetGameObject() { return m_GameObject; }
    public void SetGameObject(GameObject go) { m_GameObject = go; }
    public bool HasGameObject() { return m_GameObject != null; }

}

public class GameObject
{
    private string m_Name;
    private ComponentVector m_Components;

    public GameObject(string name)
    {
        m_Name = name;
        m_Components = new ComponentVector();
    }

    public T GetComponent<T>() where T : Component
    {
        for (int i = 0, size = m_Components.Count; i < size; i++)
        {
            if (m_Components[i] is T c )
            {
                return c;
            }
        }
        return null;
    }

    public T GetComponent<T>(ComponentType type) where T : Component
    {
        for (int i = 0, size = m_Components.Count; i < size; i++)
        {
            Component c = m_Components[i];
            if (c.Type == type)
            {
                return (T)c;
            }
        }
        return null;
    }

    public void AddComponent(Component c)
    {
        Debug.Assert(!c.HasGameObject(), "Component has no GameObject");
        c.SetGameObject(this);
        m_Components.Add(c);
    }

    public void Start()
    {
        for (int i = 0, size = m_Components.Count; i < size; i++)
        {
            m_Components[i].Start();
        }
    }

    public void Update(double time, float deltaTime)
    {
        for (int i = 0, size = m_Components.Count; i < size; i++)
        {
            m_Components[i].Update(time, deltaTime);
        }
    }
} // GameObject

class World
{
    public static GameObjectVector s_Objects = new GameObjectVector();
    public static ComponentVector FindAllComponentsOfType<T>() where T : Component
    {
        ComponentVector res = new ComponentVector();
        for (int i = 0, size = s_Objects.Count; i < size; i++)
        {
            T c = s_Objects[i].GetComponent<T>();
            if (c != null)
                res.Add(c);
        }
        return res;
    }

    public static ComponentVector FindAllComponentsOfType<T>(ComponentType type) where T : Component
    {
        ComponentVector res = new ComponentVector();
        for (int i = 0, size = s_Objects.Count; i < size; i++)
        {
            T c = s_Objects[i].GetComponent<T>(type);
            if (c != null)
                res.Add(c);
        }
        return res;
    }

    public static T FindOfType<T>() where T : Component
    {
        for (int i = 0; i < s_Objects.Count; i++)
        {
            T c = s_Objects[i].GetComponent<T>();
            if (c != null)
                return c;
        }
        return null;
    }

    public static T FindOfType<T>(ComponentType type) where T : Component
    {
        for (int i = 0; i < s_Objects.Count; i++)
        {
            T c = s_Objects[i].GetComponent<T>(type);
            if (c != null)
                return c;
        }
        return null;
    }

    public class PositionComponent : Component
    {
        public PositionComponent() : base(ComponentType.kCompPosition) { }
        public float x, y;
    }

    public class SpriteComponent : Component
    {
        public SpriteComponent() : base(ComponentType.kCompSprite) { }
        public float colorR, colorG, colorB;
        public int spriteIndex;
        public float scale;
    }

    public class WorldBoundsComponent : Component
    {
        public WorldBoundsComponent() : base(ComponentType.kCompWorldBounds) { }
        public float xMin, xMax, yMin, yMax;
    }

    public class MoveComponent : Component
    {
        public float velx, vely;

        public MoveComponent(float minSpeed, float maxSpeed) : base(ComponentType.kCompMove)
        {
            // random angle
            float angle = Utility.RandomFloat01() * 3.1415926f * 2;
            // random movement speed between given min & max
            float speed = Utility.RandomFloat(minSpeed, maxSpeed);
            // velocity x & y components
            velx = (float)Math.Cos(angle) * speed;
            vely = (float)Math.Sin(angle) * speed;
        }

        public override void Start()
        {
            s_MoveSystem.AddObjectToSystem(this);
        }
    }

    public class MoveSystem
    {
        private WorldBoundsComponent bounds;
        private List<PositionComponent> positionList = new List<PositionComponent>();
        private List<MoveComponent> moveList = new List<MoveComponent>();

        public void AddObjectToSystem(MoveComponent o)
        {
            positionList.Add(o.GetGameObject().GetComponent<PositionComponent>(ComponentType.kCompPosition));
            moveList.Add(o);
        }

        public void UpdateSystem(double time, float deltaTime)
        {
            for(int i = 0, n = positionList.Count; i < n; i++)
            {
                PositionComponent pos = positionList[i];
                MoveComponent move = moveList[i];

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

    // When present, tells things that have Avoid component to avoid this object
    public class AvoidThisComponent : Component
    {
        public AvoidThisComponent() : base(ComponentType.kCompAvoidThis) { }
        public float distance;
    }

    // Objects with this component "avoid" objects with AvoidThis component:
    // - when they get closer to them than Avoid::distance, they bounce back,
    // - also they take sprite color from the object they just bumped into
    public class AvoidComponent : Component
    {
        public AvoidComponent() : base(ComponentType.kCompAvoid) { }
        public override void Start()
        {
            s_AvoidanceSystem.AddObjectToSystem(this);
        }
    }

    // "Avoidance system" works out interactions between objects that have AvoidThis and Avoid
    // components. Objects with Avoid component:
    // - when they get closer to AvoidThis than AvoidThis::distance, they bounce back,
    public class AvoidanceSystem
    {
        private List<float> avoidDistanceList;
        private List<PositionComponent> avoidPositionList;
        // objects that avoid: their position components
        private List<PositionComponent> objectList = new List<PositionComponent>();

        void Initialize()
        {
            // find all things to be avoided, and fill our arrays that hold
            var avList = FindAllComponentsOfType<AvoidThisComponent>();
            int size = avList.Count;
            avoidDistanceList = new List<float>(size);
            avoidPositionList = new List<PositionComponent>(size);

            for (int i = 0; i < size; i++)
            {
                AvoidThisComponent av = (AvoidThisComponent)avList[i];
                avoidDistanceList[i] = av.distance;
                avoidPositionList[i] = av.GetGameObject().GetComponent<PositionComponent>(ComponentType.kCompPosition);
            }
        }

        public void AddObjectToSystem(AvoidComponent av)
        {
            objectList.Add(av.GetGameObject().GetComponent<PositionComponent>(ComponentType.kCompPosition));
        }

        public static float DistanceSq(PositionComponent a, PositionComponent b)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            return dx * dx + dy * dy;
        }

        void ResolveCollision(PositionComponent pos, float deltaTime)
        {
            MoveComponent move = pos.GetGameObject().GetComponent<MoveComponent>(ComponentType.kCompMove);
            // flip velocity
            move.velx = -move.velx;
            move.vely = -move.vely;

            // move us out of collision, by moving just a tiny bit more than we'd normally move during a frame
            pos.x += move.velx * deltaTime * 1.1f;
            pos.y += move.vely * deltaTime * 1.1f;
        }

        public void UpdateSystem(double time, float deltaTime)
        {
            int avoidCount = avoidPositionList.Count;
            // go through all objects
            for (int i = 0, size = objectList.Count; i < size; i++)
            {
                PositionComponent myPosition = avoidPositionList[i];

                // Check each thing to avoid
                for (int ia = 0; ia < avoidCount; ia++)
                {
                    float avDistance = avoidDistanceList[ia];
                    PositionComponent avPosition = avoidPositionList[ia];

                    if (DistanceSq(myPosition, avPosition) < avDistance * avDistance)
                    {
                        ResolveCollision(myPosition, deltaTime);

                        SpriteComponent avoidSprite = avPosition.GetGameObject().GetComponent<SpriteComponent>(ComponentType.kCompSprite);
                        SpriteComponent mySprite = myPosition.GetGameObject().GetComponent<SpriteComponent>(ComponentType.kCompSprite);
                        mySprite.colorR = avoidSprite.colorR;
                        mySprite.colorG = avoidSprite.colorG;
                        mySprite.colorB = avoidSprite.colorB;
                    }
                }
            }
        }
    }//AvoidanceSystem
    static AvoidanceSystem s_AvoidanceSystem = new AvoidanceSystem();

}