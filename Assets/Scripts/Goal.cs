using UnityEngine;

public class Goal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var player = EnhancedMeshGenerator.Instance;
            if (player != null)
            {
                player.Win();
            }
        }
    }
}
