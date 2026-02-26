using UnityEngine;

public class EnemyRoamer : MonoBehaviour
{
    public float speed = 2f;
    public float range = 5f;
    public Vector3 startPos;
    private int direction = 1;

    void Update()
    {
        transform.position += Vector3.right * direction * speed * Time.deltaTime;

        float dist = transform.position.x - startPos.x;
        if (Mathf.Abs(dist) > range)
        {
            direction *= -1;
            transform.position = new Vector3(startPos.x + Mathf.Sign(dist) * range, transform.position.y, transform.position.z);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var player = EnhancedMeshGenerator.Instance;
            if (player != null)
            {
                player.TakeDamage();
            }
        }
    }
}
