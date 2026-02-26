using UnityEngine;

public class Hazard : MonoBehaviour
{
    public bool isInstakill = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var player = EnhancedMeshGenerator.Instance;
            if (player != null)
            {
                if (isInstakill)
                {
                    player.Instakill();
                }
                else
                {
                    player.TakeDamage();
                }
            }
        }
    }
}
