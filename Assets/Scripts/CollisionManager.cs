using System.Collections.Generic;
using UnityEngine;

public class AABBBounds
{
    public Vector3 Center { get; private set; }
    public Vector3 Size { get; private set; }
    public Vector3 Extents { get; private set; }
    public Vector3 Min { get; private set; }
    public Vector3 Max { get; private set; }
    public int ID { get; private set; }
    public bool IsPlayer { get; private set; }
    public bool IsOneWay { get; private set; }
    public bool IsTemporany { get; private set; }
    public bool IsTrigger { get; private set; }
    public Matrix4x4 Matrix { get; set; }

    public AABBBounds(Vector3 center, Vector3 size, int id, bool isPlayer = false, bool isOneWay = false, bool isTemporany = false, bool isTrigger = false)
    {
        ID = id;
        IsPlayer = isPlayer;
        IsOneWay = isOneWay;
        IsTemporany = isTemporany;
        IsTrigger = isTrigger;
        UpdateBounds(center, size);
    }

    public void UpdateBounds(Vector3 center, Vector3 size)
    {
        Center = center;
        Size = size;
        Extents = size * 0.5f;
        Min = center - Extents;
        Max = center + Extents;
    }

    public bool Intersects(AABBBounds other)
    {
        return !(Max.x < other.Min.x || Min.x > other.Max.x ||
                 Max.y < other.Min.y || Min.y > other.Max.y ||
                 Max.z < other.Min.z || Min.z > other.Max.z);
    }
}

public class SimpleCollisionEntity : MonoBehaviour
{
    public Vector3 size = Vector3.one;
    public bool isStatic = false;
    public bool isPlayer = false;
    public bool isOneWay = false;
    public bool isTrigger = false;

    private int _colliderID = -1;

    void Start()
    {
        _colliderID = CollisionManager.Instance.RegisterCollider(transform.position, size, isPlayer, gameObject, isOneWay, false, isTrigger);
    }

    void OnDestroy()
    {
        if (CollisionManager.Instance != null && _colliderID != -1)
        {
            CollisionManager.Instance.RemoveCollider(_colliderID);
        }
    }

    void LateUpdate()
    {
        if (!isStatic && _colliderID != -1)
        {
            CollisionManager.Instance.UpdateCollider(_colliderID, transform.position, size);
        }
    }

    public int GetColliderID() => _colliderID;
}

public class CollisionManager : MonoBehaviour
{
    private static CollisionManager _instance;
    public static CollisionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("CollisionManager");
                _instance = go.AddComponent<CollisionManager>();
            }
            return _instance;
        }
    }

    public void Clear()
    {
        _colliders.Clear();
        _gameObjectMap.Clear();
        nextID = 0;
    }

    private Dictionary<int, AABBBounds> _colliders = new Dictionary<int, AABBBounds>();
    private Dictionary<int, GameObject> _gameObjectMap = new Dictionary<int, GameObject>();
    private int nextID = 0;

    public int RegisterCollider(Vector3 center, Vector3 size, bool isPlayer = false, GameObject owner = null, bool isOneWay = false, bool isTemporany = false, bool isTrigger = false)
    {
        int id = nextID++;
        _colliders[id] = new AABBBounds(center, size, id, isPlayer, isOneWay, isTemporany, isTrigger);
        if (owner != null) _gameObjectMap[id] = owner;
        return id;
    }

    public void UpdateCollider(int id, Vector3 center, Vector3 size)
    {
        if (_colliders.TryGetValue(id, out AABBBounds bounds))
        {
            bounds.UpdateBounds(center, size);
        }
    }

    public void RemoveCollider(int id)
    {
        _colliders.Remove(id);
        _gameObjectMap.Remove(id);
    }

    public void UpdateMatrix(int id, Matrix4x4 matrix)
    {
        if (_colliders.TryGetValue(id, out AABBBounds bounds))
        {
            bounds.Matrix = matrix;
        }
    }

    public bool CheckCollision(int id, Vector3 newCenter, out List<int> collidingIds)
    {
        collidingIds = new List<int>();
        if (!_colliders.TryGetValue(id, out AABBBounds current))
            return false;

        AABBBounds temp = new AABBBounds(newCenter, current.Size, -1);

        bool collided = false;
        foreach (var kvp in _colliders)
        {
            if (kvp.Key == id) continue;

            if (temp.Intersects(kvp.Value))
            {
                collidingIds.Add(kvp.Key);
                collided = true;
            }
        }
        return collided;
    }

    public AABBBounds GetBounds(int id)
    {
        _colliders.TryGetValue(id, out AABBBounds b);
        return b;
    }

    public GameObject GetGameObject(int id)
    {
        _gameObjectMap.TryGetValue(id, out GameObject go);
        return go;
    }

    public Matrix4x4 GetMatrix(int id)
    {
        if (_colliders.TryGetValue(id, out AABBBounds bounds))
        {
            return bounds.Matrix;
        }
        return Matrix4x4.identity;
    }
}
