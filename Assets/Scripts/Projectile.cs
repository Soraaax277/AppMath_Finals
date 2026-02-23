using UnityEngine;
using System.Collections.Generic;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float lifetime = 3f;
    private Vector3 direction = Vector3.right;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;

        var col = GetComponent<SimpleCollisionEntity>();
        if (col != null)
        {
            List<int> collidingIds;
            if (CollisionManager.Instance.CheckCollision(col.GetColliderID(), transform.position, out collidingIds))
            {
                foreach (int id in collidingIds)
                {
                    GameObject go = CollisionManager.Instance.GetGameObject(id);
                    if (go != null && go.CompareTag("Enemy"))
                    {
                        Destroy(go);
                        Destroy(gameObject);
                        break;
                    }
                }
            }
        }
    }

    public void SetDirection(Vector3 newDir)
    {
        direction = newDir.normalized;
    }
}
