using UnityEngine;

public class GameUI : MonoBehaviour
{
    private EnhancedMeshGenerator player;
    private float startTime;

    void Start()
    {
        player = EnhancedMeshGenerator.Instance;
        startTime = Time.time;
    }

    void OnGUI()
    {
        if (player == null)
        {
            player = EnhancedMeshGenerator.Instance;
            if (player == null) return;
        }

        GUI.Box(new Rect(10, 10, 200, 100), "Game Status");

        float elapsedTime = Time.time - startTime;
        GUI.Label(new Rect(20, 35, 180, 20), $"Time: {elapsedTime:F2}s");

        GUI.Label(new Rect(20, 55, 180, 20), $"Lives: {player.lives}");

        GUI.Label(new Rect(20, 75, 180, 20), $"Points: {player.points}");

        if (player.IsInvincible())
        {
            GUI.color = Color.yellow;
            GUI.Label(new Rect(20, 95, 180, 20), "INVINCIBLE!");
            GUI.color = Color.white;
        }

        if (player.lives <= 0)
        {
            GUI.color = Color.red;
            GUI.Label(new Rect(Screen.width / 2 - 50, Screen.height / 2, 100, 20), "GAME OVER");
        }
    }
}
