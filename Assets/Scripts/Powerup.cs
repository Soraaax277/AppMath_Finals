using System.Collections.Generic;
using UnityEngine;

public class Powerup : MonoBehaviour
{
    public enum PowerupType { Fireball = 0, ExtraLife = 1, Invincibility = 2, PW1 = 11, PW2 = 12, PW3 = 13, Collectible = 20 }
    public PowerupType type;
    public float duration = 5f;

    private SimpleCollisionEntity _collisionEntity;

    void Start()
    {
        _collisionEntity = GetComponent<SimpleCollisionEntity>();
    }

    void Update()
    {
        if (_collisionEntity == null) return;

        List<int> collidingIds;
        if (CollisionManager.Instance.CheckCollision(_collisionEntity.GetColliderID(), transform.position, out collidingIds))
        {
            foreach (int id in collidingIds)
            {
                GameObject go = CollisionManager.Instance.GetGameObject(id);
                if (go != null && go.CompareTag("Player"))
                {
                    ApplyPowerup();
                    Destroy(gameObject);
                    break;
                }
            }
        }
    }

    private void ApplyPowerup()
    {
        var player = EnhancedMeshGenerator.Instance;
        if (player == null) return;

        switch (type)
        {
            case PowerupType.Fireball:
                if (player.GetComponent<FireballAbility>() == null)
                    player.gameObject.AddComponent<FireballAbility>();
                break;
            case PowerupType.ExtraLife:
                player.AddLife();
                break;
            case PowerupType.Invincibility:
                player.SetInvincibility(duration);
                break;
            case PowerupType.PW1:
                player.hasDoubleJump = true;
                player.NotifyPowerup("YOU GOT DOUBLE JUMP");
                break;
            case PowerupType.PW2:
                player.hasWallClimb = true;
                player.NotifyPowerup("YOU GOT WALL JUMP");
                break;
            case PowerupType.PW3:
                player.hasDash = true;
                player.NotifyPowerup("YOU GOT DASH");
                break;
            case PowerupType.Collectible:
                player.points += 100;
                break;
        }
    }
}

public class FireballAbility : MonoBehaviour
{
    private float cooldown = 0.5f;
    private float timer = 0f;

    void Update()
    {
        timer -= Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.F) && timer <= 0)
        {
            Shoot();
            timer = cooldown;
        }
    }

    void Shoot()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Fireball";
        go.transform.position = transform.position + Vector3.right; 
        go.AddComponent<Projectile>();
        
        var col = go.AddComponent<SimpleCollisionEntity>();
        col.size = new Vector3(0.5f, 0.5f, 0.5f);
        
        var physicCol = go.GetComponent<Collider>();
        if (physicCol != null) Destroy(physicCol);
    }
}
