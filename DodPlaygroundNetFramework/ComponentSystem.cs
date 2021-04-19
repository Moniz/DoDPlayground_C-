

using System;
using System.Diagnostics;
using ComponentVector = System.Collections.Generic.List<Component>;
using GameObjectVector = System.Collections.Generic.List<GameObject>;

public class Utility
{
    public static Random rnd = new Random();
    public static float RandomFloat01() { return (float)rnd.NextDouble(); }
    public static float RandomFloat(float from, float to) { return RandomFloat01() * (to - from) + from; }
}

public class Component
{
    private GameObject m_GameObject;

    public Component()
    {
        m_GameObject = null;
    }

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
            T c = m_Components[i] as T;
            if (c != null)
                return c;
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

    public class PositionComponent : Component
    {
        public float x, y;
    }

    public class SpriteComponent : Component
    {
        public float colorR, colorG, colorB;
        public int spriteIndex;
        public float scale;
    }

    public class WorldBoundsComponent : Component
    {
        public float xMin, xMax, yMin, yMax;
    }

    public class MoveComponent : Component
    {
        public float velx, vely;
        public WorldBoundsComponent bounds;

        private PositionComponent pos;

        public MoveComponent(float minSpeed, float maxSpeed)
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
            bounds = FindOfType<WorldBoundsComponent>();

            pos = GetGameObject().GetComponent<PositionComponent>();
        }

        public override void Update(double time, float deltaTime)
        {
            // update position based on movement velocity & delta time
            pos.x += velx * deltaTime;
            pos.y += vely * deltaTime;

            // check against world bounds; put back onto bounds and mirror the velocity component to "bounce" back
            if (pos.x < bounds.xMin)
            {
                velx = -velx;
                pos.x = bounds.xMin;
            }
            if (pos.x > bounds.xMax)
            {
                velx = -velx;
                pos.x = bounds.xMax;
            }
            if (pos.y < bounds.yMin)
            {
                vely = -vely;
                pos.y = bounds.yMin;
            }
            if (pos.y > bounds.yMax)
            {
                vely = -vely;
                pos.y = bounds.yMax;
            }
        }
    }

    // When present, tells things that have Avoid component to avoid this object
    public class AvoidThisComponent : Component
    {
        public float distance;
    }

    // Objects with this component "avoid" objects with AvoidThis component:
    // - when they get closer to them than Avoid::distance, they bounce back,
    // - also they take sprite color from the object they just bumped into
    public class AvoidComponent : Component
    {
        private static ComponentVector avoidList = new ComponentVector();
        private static ComponentVector avoidPositionList;
        private PositionComponent myposition;

        public override void Start()
        {
            myposition = GetGameObject().GetComponent<PositionComponent>();
            // fetch list of objects we'll be avoiding, if we haven't done that yet
            if (avoidList.Count > 0)
            {
                avoidList = FindAllComponentsOfType<AvoidThisComponent>();
                avoidPositionList = new ComponentVector(avoidList.Count);

                for (int i = 0, size = avoidList.Count; i < size; i++)
                {
                    avoidPositionList.Add(avoidList[i].GetGameObject().GetComponent<PositionComponent>());
                }
            }
        }

        public static float DistanceSq(PositionComponent a, PositionComponent b)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            return dx * dx + dy * dy;
        }

        void ResolveCollision(float deltaTime)
        {
            MoveComponent move = GetGameObject().GetComponent<MoveComponent>();
            // flip velocity
            move.velx = -move.velx;
            move.vely = -move.vely;

            // move us out of collision, by moving just a tiny bit more than we'd normally move during a frame
            PositionComponent pos = GetGameObject().GetComponent<PositionComponent>();
            pos.x += move.velx * deltaTime * 1.1f;
            pos.y += move.vely * deltaTime * 1.1f;
        }

        public override void Update(double time, float deltaTime)
        {
            // check each thing in avoid list
            for (int i = 0, size = avoidList.Count; i < size; i++)
            {
                AvoidThisComponent av = (AvoidThisComponent)avoidList[i];

                PositionComponent avoidposition = (PositionComponent)avoidPositionList[i];
                // is our position closer to "thing to avoid" position than the avoid distance?
                if (DistanceSq(myposition, avoidposition) < av.distance * av.distance)
                {
                    ResolveCollision(deltaTime);

                    // also make our sprite take the color of the thing we just bumped into
                    SpriteComponent avoidSprite = av.GetGameObject().GetComponent<SpriteComponent>();
                    SpriteComponent mySprite = GetGameObject().GetComponent<SpriteComponent>();
                    mySprite.colorR = avoidSprite.colorR;
                    mySprite.colorG = avoidSprite.colorG;
                    mySprite.colorB = avoidSprite.colorB;
                }
            }
        }
    }//AvoidComponent


}