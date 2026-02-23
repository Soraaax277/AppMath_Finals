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

        // 1. HUD Area
        GUIStyle hudStyle = new GUIStyle(GUI.skin.box);
        hudStyle.padding = new RectOffset(15, 15, 15, 15);
        hudStyle.fontSize = 14;
        hudStyle.normal.textColor = Color.white;
        hudStyle.alignment = TextAnchor.UpperLeft;

        GUILayout.BeginArea(new Rect(20, 20, 220, 220), hudStyle);
        {
            GUILayout.Label("Φ ENERGY STATE");
            GUILayout.Space(5);
            
            // Dash Indicator with cooldown feedback
            if (player.hasDash)
            {
                float cooldown = player.GetDashCooldownRatio();
                GUI.color = cooldown > 0 ? new Color(1, 1, 1, 0.4f) : Color.white;
                GUILayout.Label(cooldown > 0 ? ">> DASH (RECHARGING)" : ">> DASH READY");
                GUI.color = Color.white;
            }
            
            if (player.hasDoubleJump)
            {
                float cooldown = player.GetJumpCooldownRatio();
                GUI.color = cooldown > 0 ? new Color(1, 1, 1, 0.4f) : Color.white;
                GUILayout.Label(cooldown > 0 ? ">> JUMP (RECHARGING)" : ">> JUMP READY");
                GUI.color = Color.white;
            }

            if (player.hasWallClimb)
            {
                float cooldown = player.GetWallJumpCooldownRatio();
                GUI.color = cooldown > 0 ? new Color(1, 1, 1, 0.4f) : Color.white;
                GUILayout.Label(cooldown > 0 ? ">> WALL JUMP (RECHARGING)" : ">> WALL JUMP READY");
                GUI.color = Color.white;
            }
            
            GUILayout.Space(10);
            float elapsedTime = Time.time - startTime;
            GUILayout.Label($"Δ TIME: {elapsedTime:F2}s");
            GUILayout.Label($"V LIFE: {player.lives}");
            GUILayout.Label($"Σ POINTS: {player.points}");

            if (player.IsInvincible())
            {
                GUI.color = Color.yellow;
                GUILayout.Label("STATUS: Ω-STATE");
                GUI.color = Color.white;
            }
        }
        GUILayout.EndArea();

        // 2. Power-up Announcement (Center-Top)
        string announcement = player.GetAnnouncement();
        if (!string.IsNullOrEmpty(announcement))
        {
            GUIStyle announceStyle = new GUIStyle(GUI.skin.label);
            announceStyle.fontSize = 24;
            announceStyle.fontStyle = FontStyle.Bold;
            announceStyle.alignment = TextAnchor.MiddleCenter;
            announceStyle.normal.textColor = Color.cyan;
            
            GUI.Box(new Rect(Screen.width/2 - 200, 50, 400, 40), "");
            GUI.Label(new Rect(Screen.width/2 - 200, 50, 400, 40), announcement, announceStyle);
        }

        // 3. Game Over Overlay
        if (player.lives <= 0)
        {
            GUIStyle gameOverStyle = new GUIStyle(GUI.skin.label);
            gameOverStyle.fontSize = 40;
            gameOverStyle.fontStyle = FontStyle.Bold;
            gameOverStyle.alignment = TextAnchor.MiddleCenter;
            gameOverStyle.normal.textColor = Color.red;

            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "SYSTEM TERMINATED", gameOverStyle);
        }
    }
}
