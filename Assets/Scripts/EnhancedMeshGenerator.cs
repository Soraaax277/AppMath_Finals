using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

public class EnhancedMeshGenerator : MonoBehaviour
{
    private static EnhancedMeshGenerator _instance;
    public static EnhancedMeshGenerator Instance => _instance;

    [Header("Rendering")]
    public Material material;
    public Material playerMaterial;
    public int instanceCount = 100;
    private Mesh cubeMesh;
    private Mesh spikeMesh;
    private List<Matrix4x4> matrices = new List<Matrix4x4>();
    private List<int> colliderIds = new List<int>();
    
    [Header("Mesh Dimensions")]
    public float width = 1f;
    public float height = 1f;
    public float depth = 1f;
    
    [Header("Player Settings")]
    public float movementSpeed = 6f;
    public float gravity = 18f;
    public float jumpForce = 8f;
    public int lives = 3;
    
    [Header("Abilities")]
    public bool hasDoubleJump = false;
    public bool hasDash = false;
    public bool hasWallClimb = false;
    private int currentJumps = 0;
    public float jumpCooldown = 0.7f;
    private float jumpCooldownTimer = 0f;
    
    [Header("Wall Jump")]
    public float wallKickForceX = 8f;
    public float wallKickForceY = 8f;
    public float wallKickDuration = 0.2f;
    public float wallJumpCooldown = 0.2f;
    private float wallKickTimer = 0f;
    private float wallJumpCooldownTimer = 0f;
    private float wallKickDir = 0f;

    [Header("Dash")]
    public float dashForce = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.5f;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private float lastHDir = 1f;

    [Header("UI Announcements")]
    private string lastPowerupName = "";
    private float announcementTimer = 0f;

    private bool isInvincible = false;
    private float invincibilityTimer = 0f;
    private int playerID = -1;
    private Vector3 playerPosition;
    private Vector3 playerVelocity = Vector3.zero;
    private bool isGrounded = false;
    private bool isWalled = false;
    
    [Header("Camera Settings")]
    public Vector3 cameraOffset = new Vector3(0, 0, -15);
    public float cameraSmoothSpeed = 0.1f;
    private Transform mainCameraTransform;
    
    [Header("World Settings")]
    public float constantZPosition = 0f;
    public float voidThreshold = -30f;
    public Vector3 spawnPosition = new Vector3(0, 5, 0);
    private Vector3 lastSafePlatformPosition;
    public int points = 0;
    private bool hasWon = false;
    private bool isGameOver = false;
    public float totalPlayTime = 0f;

    private Dictionary<int, float> disappearingTimers = new Dictionary<int, float>();
    private List<int> hiddenInstances = new List<int>();

    void Awake()
    {
        if (_instance == null) _instance = this;
        gameObject.tag = "Player";
    }

    void Start()
    {
        if (material != null) material.enableInstancing = true;
        if (playerMaterial != null) playerMaterial.enableInstancing = true;

        SetupCamera();
        CreateCubeMesh();
        CreateSpikeMesh();
        CreatePlayer();
        CreateLevel();

        lastSafePlatformPosition = spawnPosition;

        if (FindAnyObjectByType<GameUI>() == null)
        {
            new GameObject("GameUI").AddComponent<GameUI>();
        }
    }
    
    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("MainCamera");
            cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }
        mainCameraTransform = cam.transform;
    }

    void CreateCubeMesh()
    {
        cubeMesh = new Mesh();
        float hw = width * 0.5f;
        float hh = height * 0.5f;
        float hd = depth * 0.5f;

        Vector3[] vertices = new Vector3[8]
        {
            new Vector3(-hw, -hh, -hd), new Vector3( hw, -hh, -hd),
            new Vector3( hw, -hh,  hd), new Vector3(-hw, -hh,  hd),
            new Vector3(-hw,  hh, -hd), new Vector3( hw,  hh, -hd),
            new Vector3( hw,  hh,  hd), new Vector3(-hw,  hh,  hd)
        };
        
        int[] triangles = new int[36]
        {
            0, 4, 1, 1, 4, 5, 2, 6, 3, 3, 6, 7,
            0, 3, 4, 4, 3, 7, 1, 5, 2, 2, 5, 6,
            0, 1, 3, 3, 1, 2, 4, 7, 5, 5, 7, 6 
        };
        
        Vector2[] uvs = new Vector2[8];
        for (int i = 0; i < 8; i++) uvs[i] = new Vector2(vertices[i].x / width + 0.5f, vertices[i].z / depth + 0.5f);

        cubeMesh.vertices = vertices;
        cubeMesh.triangles = triangles;
        cubeMesh.uv = uvs;
        cubeMesh.RecalculateNormals();
        cubeMesh.RecalculateBounds();
    }

    void CreateSpikeMesh()
    {
        spikeMesh = new Mesh();
        Vector3[] vertices = new Vector3[5]
        {
            new Vector3(0, 0.5f, 0),
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f)
        };
        int[] triangles = new int[18]
        {
            0, 2, 1, 0, 3, 2, 0, 4, 3, 0, 1, 4,
            1, 2, 3, 1, 3, 4
        };
        spikeMesh.vertices = vertices;
        spikeMesh.triangles = triangles;
        spikeMesh.RecalculateNormals();
        spikeMesh.RecalculateBounds();
    }
    
    void CreatePlayer()
    {
        playerPosition = spawnPosition;
        if (CollisionManager.Instance != null)
        {
            CollisionManager.Instance.Clear();
        }

        playerID = CollisionManager.Instance.RegisterCollider(playerPosition, new Vector3(width, height, depth), true, gameObject);
        
        var col = GetComponent<BoxCollider>();
        if (col == null) col = gameObject.AddComponent<BoxCollider>();
        col.size = new Vector3(width, height, depth);
        col.isTrigger = true;

        var rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        
        Matrix4x4 playerMatrix = Matrix4x4.TRS(playerPosition, Quaternion.identity, Vector3.one);
        matrices.Add(playerMatrix);
        colliderIds.Add(playerID);
    }
    
    void CreateLevel()
    {
        AddPlatform(new Vector3(0, 0, 0), new Vector3(10, 1, 1));
        AddHP(new Vector3(-2, 2, 0)); 
        AddPlatform(new Vector3(11, 2, 0), new Vector3(4, 1, 1));
        AddC(new Vector3(11, 4, 0)); 
        
        AddPW(new Vector3(5, 5, 0), 1); 
        AddPlatform(new Vector3(7, 4, 0), new Vector3(2, 0.5f, 1), true);
        AddC(new Vector3(3, 7, 0));

        AddPlatform(new Vector3(18, 5, 0), new Vector3(6, 1, 1));
        AddHP(new Vector3(18, 7, 0));
        AddPlatform(new Vector3(26, 8, 0), new Vector3(4, 1, 1));
        AddC(new Vector3(26, 10, 0));
        
        AddPW(new Vector3(35, 10, 0), 2); 
        AddPlatform(new Vector3(35, 9, 0), new Vector3(5, 1, 1));
        AddPlatform(new Vector3(40, 7, 0), new Vector3(3, 1, 1), false, true); 
        
        AddPlatform(new Vector3(45, 12, 0), new Vector3(1, 15, 1)); 
        AddPlatform(new Vector3(40, 15, 0), new Vector3(3, 1, 1), false, true); 
        AddPW(new Vector3(44, 25, 0), 3); 
        AddC(new Vector3(43, 20, 0));
        
        AddPlatform(new Vector3(55, 15, 0), new Vector3(10, 1, 1), true); 
        AddC(new Vector3(60, 17, 0));
        AddC(new Vector3(65, 20, 0));
        AddHP(new Vector3(70, 22, 0));
        AddPlatform(new Vector3(70, 20, 0), new Vector3(10, 1, 1)); 
        
        AddGoal(new Vector3(74, 21.5f, 0));

        AddSpike(new Vector3(12.5f, 2.75f, 0));

        AddEnemy(new Vector3(18, 5.9f, 0), 2f); 
        AddSpike(new Vector3(37, 9.75f, 0)); 

        AddSpike(new Vector3(53, 15.75f, 0));
        AddSpike(new Vector3(57, 15.75f, 0)); 
        
        AddSpike(new Vector3(67, 20.75f, 0));
        AddSpike(new Vector3(71, 20.75f, 0));
    }

    void AddPlatform(Vector3 pos, Vector3 scale, bool isOneWay = false, bool isTemporany = false)
    {
        int id = CollisionManager.Instance.RegisterCollider(pos, Vector3.Scale(new Vector3(width, height, depth), scale), false, null, isOneWay, isTemporany, false);
        matrices.Add(Matrix4x4.TRS(pos, Quaternion.identity, scale));
        colliderIds.Add(id);
        CollisionManager.Instance.UpdateMatrix(id, matrices[matrices.Count-1]);
    }

    void AddC(Vector3 pos) { AddCollectible(pos, Powerup.PowerupType.Collectible); }
    void AddHP(Vector3 pos) { AddCollectible(pos, Powerup.PowerupType.ExtraLife); }

    void AddCollectible(Vector3 pos, Powerup.PowerupType pType)
    {
        GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        p.transform.position = pos;
        p.transform.localScale = Vector3.one * 0.5f;
        var pw = p.AddComponent<Powerup>();
        pw.type = pType;
        var col = p.AddComponent<SimpleCollisionEntity>();
        col.size = Vector3.one * 0.5f;
        col.isTrigger = true;
        
        var unityCol = p.GetComponent<Collider>();
        if (unityCol != null) unityCol.isTrigger = true;
        
        Renderer r = p.GetComponent<Renderer>();
        if (r != null) r.material.color = pType == Powerup.PowerupType.ExtraLife ? Color.red : Color.yellow;
    }

    void AddPW(Vector3 pos, int type)
    {
        GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        p.name = "PW" + type;
        p.transform.position = pos;
        p.transform.localScale = Vector3.one * 0.8f;
        var pw = p.AddComponent<Powerup>();
        pw.type = (Powerup.PowerupType)(type + 10);
        var col = p.AddComponent<SimpleCollisionEntity>();
        col.size = Vector3.one * 0.8f;
        col.isTrigger = true;
        
        var unityCol = p.GetComponent<Collider>();
        if (unityCol != null) unityCol.isTrigger = true;
        
        Renderer r = p.GetComponent<Renderer>();
        if (r != null) r.material.color = type == 1 ? Color.cyan : (type == 2 ? Color.magenta : Color.green);
    }

    void AddEnemy(Vector3 pos, float range)
    {
        GameObject e = GameObject.CreatePrimitive(PrimitiveType.Cube);
        e.name = "Enemy";
        e.transform.position = pos;
        e.transform.localScale = Vector3.one * 0.8f;
        var roamer = e.AddComponent<EnemyRoamer>();
        roamer.range = range;
        roamer.startPos = pos;
        
        var col = e.AddComponent<SimpleCollisionEntity>();
        col.size = Vector3.one * 0.8f;
        col.isTrigger = true;
        
        var unityCol = e.GetComponent<Collider>();
        if (unityCol != null) unityCol.isTrigger = true;
        
        Renderer r = e.GetComponent<Renderer>();
        if (r != null) r.material.color = new Color(0.5f, 0, 0);
    }

    void AddSpike(Vector3 pos)
    {
        GameObject s = new GameObject("Spike");
        s.transform.position = pos;
        s.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        s.AddComponent<MeshFilter>().mesh = spikeMesh;
        var r = s.AddComponent<MeshRenderer>();
        r.material = material;
        r.material.color = Color.black;
        s.AddComponent<Hazard>().isInstakill = true;
        var col = s.AddComponent<SimpleCollisionEntity>();
        col.size = new Vector3(0.8f, 0.5f, 0.8f);
        col.isTrigger = true;
        var unityCol = s.AddComponent<BoxCollider>();
        unityCol.isTrigger = true;
        unityCol.size = new Vector3(0.8f, 0.5f, 0.8f);
    }

    void AddGoal(Vector3 pos)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        g.name = "Goal";
        g.transform.position = pos;
        g.transform.localScale = new Vector3(1, 2, 1);
        g.AddComponent<Goal>();
        
        var col = g.AddComponent<SimpleCollisionEntity>();
        col.size = new Vector3(1, 2, 1);
        col.isTrigger = true;
        
        var unityCol = g.GetComponent<Collider>();
        if (unityCol != null) unityCol.isTrigger = true;
        
        Renderer r = g.GetComponent<Renderer>();
        if (r != null) r.material.color = Color.blue;
    }

    void Update()
    {
        if (!hasWon && !isGameOver)
        {
            totalPlayTime += Time.deltaTime;
        }

        if (invincibilityTimer > 0)
        {
            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0) isInvincible = false;
        }

        UpdatePlayer();
        UpdateCamera();
        UpdateDisappearingPlatforms();
        RenderBoxes();
    }

    void UpdateDisappearingPlatforms()
    {
        List<int> toRemove = new List<int>();
        foreach (var id in disappearingTimers.Keys.ToArray())
        {
            disappearingTimers[id] -= Time.deltaTime;
            if (disappearingTimers[id] <= 0)
            {
                hiddenInstances.Add(id);
                toRemove.Add(id);
            }
        }
        foreach (int id in toRemove) disappearingTimers.Remove(id);
    }

    void UpdatePlayer()
    {
        if (playerID == -1 || hasWon || isGameOver) return;
        int index = colliderIds.IndexOf(playerID);

        if (dashTimer > 0) dashTimer -= Time.deltaTime;
        if (dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;
        if (announcementTimer > 0) announcementTimer -= Time.deltaTime;
        if (wallKickTimer > 0) wallKickTimer -= Time.deltaTime;
        if (jumpCooldownTimer > 0) jumpCooldownTimer -= Time.deltaTime;
        if (wallJumpCooldownTimer > 0) wallJumpCooldownTimer -= Time.deltaTime;

        float horizontal = 0;
        if (Input.GetKey(KeyCode.A)) horizontal -= 1;
        if (Input.GetKey(KeyCode.D)) horizontal += 1;
        if (horizontal != 0) lastHDir = Mathf.Sign(horizontal);

        if (hasDash && Input.GetKeyDown(KeyCode.LeftShift) && dashTimer <= 0 && dashCooldownTimer <= 0)
        {
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown + dashDuration;
            playerVelocity.x = lastHDir * dashForce;
            playerVelocity.y = 0;
            isGrounded = false;
        }

        if (dashTimer > 0)
        {
            float hMove = playerVelocity.x * Time.deltaTime;
            Vector3 hTargetPos = playerPosition + new Vector3(hMove, 0, 0);
            if (!CheckSolidCollision(hTargetPos, out _))
                playerPosition.x = hTargetPos.x;
            
            CheckSolidCollision(playerPosition + new Vector3(Mathf.Sign(playerVelocity.x) * 0.1f, 0, 0), out isWalled);
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                int allowedJumps = hasDoubleJump ? 2 : 1;
                if (isGrounded)
                {
                    playerVelocity.y = jumpForce;
                    isGrounded = false;
                    currentJumps = 1;
                }
                else if (hasWallClimb && isWalled && wallJumpCooldownTimer <= 0)
                {
                    playerVelocity.y = wallKickForceY;
                    wallKickTimer = wallKickDuration;
                    wallJumpCooldownTimer = wallJumpCooldown;
                    wallKickDir = -lastHDir;
                    playerVelocity.x = wallKickDir * wallKickForceX;
                    currentJumps = 1;
                }
                else if (currentJumps < allowedJumps && jumpCooldownTimer <= 0)
                {
                    playerVelocity.y = jumpForce;
                    jumpCooldownTimer = jumpCooldown;
                    currentJumps++;
                }
            }

            if (!isGrounded)
            {
                float fallMult = playerVelocity.y < 0 ? 0.7f : 1.4f; 
                playerVelocity.y -= gravity * fallMult * Time.deltaTime;
            }

            float airControlMult = isGrounded ? 1.0f : 0.7f; 
            float targetHVelocity = horizontal * movementSpeed * airControlMult;
            if (wallKickTimer > 0) targetHVelocity = wallKickDir * wallKickForceX;

            float hMove = targetHVelocity * Time.deltaTime;
            if (hMove != 0)
            {
                Vector3 hTargetPos = playerPosition + new Vector3(hMove, 0, 0);
                if (!CheckSolidCollision(hTargetPos, out _))
                    playerPosition.x = hTargetPos.x;
            }
            CheckSolidCollision(playerPosition + new Vector3(lastHDir * 0.1f, 0, 0), out isWalled);

            float vMove = playerVelocity.y * Time.deltaTime;
            Vector3 vTargetPos = playerPosition + new Vector3(0, vMove, 0);
            bool vCollided = false;
            List<int> hitIds;
            if (CollisionManager.Instance.CheckCollision(playerID, vTargetPos, out hitIds))
            {
                foreach (int id in hitIds)
                {
                    if (hiddenInstances.Contains(id)) continue;
                    var b = CollisionManager.Instance.GetBounds(id);
                    if (b == null || b.IsTrigger) continue;
                    
                    if (b.IsTemporany && playerVelocity.y <= 0 && !disappearingTimers.ContainsKey(id))
                        disappearingTimers[id] = 1.0f;

                    if (b.IsOneWay)
                    {
                        float playerPrevBottom = playerPosition.y - height * 0.5f;
                        if (playerVelocity.y <= 0 && playerPrevBottom >= b.Max.y - 0.1f)
                        {
                            vCollided = true;
                            break;
                        }
                    }
                    else
                    {
                        vCollided = true;
                        break;
                    }
                }
            }

            if (vCollided)
            {
                if (playerVelocity.y < 0) 
                { 
                    isGrounded = true; 
                    currentJumps = 0; 

                    List<int> gHits;
                    if (CollisionManager.Instance.CheckCollision(playerID, playerPosition + Vector3.down * 0.1f, out gHits))
                    {
                        foreach(int id in gHits)
                        {
                            var b = CollisionManager.Instance.GetBounds(id);
                            if (b != null && !b.IsTrigger && !b.IsTemporany)
                            {
                                lastSafePlatformPosition = new Vector3(playerPosition.x, b.Max.y + height * 0.51f, playerPosition.z);
                                break;
                            }
                        }
                    }
                }
                playerVelocity.y = 0;
            }
            else
            {
                playerPosition.y = vTargetPos.y;
                if (playerVelocity.y <= 0)
                {
                    if (!CheckGrounded()) isGrounded = false;
                    else 
                    { 
                        isGrounded = true; 
                        currentJumps = 0; 
                    }
                }
            }
        }

        if (playerPosition.y < voidThreshold)
        {
            TakeDamage();
            playerPosition = lastSafePlatformPosition;
            playerVelocity = Vector3.zero;
        }

        Matrix4x4 newMat = Matrix4x4.TRS(playerPosition, Quaternion.identity, Vector3.one);
        matrices[index] = newMat;
        CollisionManager.Instance.UpdateCollider(playerID, playerPosition, new Vector3(width, height, depth));
        CollisionManager.Instance.UpdateMatrix(playerID, newMat);

        transform.position = playerPosition;
    }

    bool CheckSolidCollision(Vector3 target, out bool hitWall)
    {
        hitWall = false;
        List<int> hits;
        if (CollisionManager.Instance.CheckCollision(playerID, target, out hits))
        {
            foreach (int id in hits)
            {
                if (hiddenInstances.Contains(id)) continue;
                var b = CollisionManager.Instance.GetBounds(id);
                if (b != null && !b.IsOneWay && !b.IsTrigger) { hitWall = true; return true; }
            }
        }
        return false;
    }

    bool CheckGrounded()
    {
        List<int> hits;
        Vector3 checkPos = playerPosition + new Vector3(0, -0.05f, 0);
        if (CollisionManager.Instance.CheckCollision(playerID, checkPos, out hits))
        {
            foreach (int id in hits)
            {
                if (hiddenInstances.Contains(id)) continue;
                var b = CollisionManager.Instance.GetBounds(id);
                if (b == null || b.IsTrigger) continue;
                if (!b.IsOneWay) return true;
                if (playerPosition.y - height * 0.5f >= b.Max.y - 0.1f) return true;
            }
        }
        return false;
    }

    void UpdateCamera()
    {
        if (mainCameraTransform == null) return;
        Vector3 targetPos = playerPosition + cameraOffset;
        mainCameraTransform.position = Vector3.Lerp(mainCameraTransform.position, targetPos, cameraSmoothSpeed);
    }

    void RenderBoxes()
    {
        if (cubeMesh == null || material == null) return;
        int playerIndex = colliderIds.IndexOf(playerID);
        Graphics.DrawMesh(cubeMesh, matrices[playerIndex], playerMaterial != null ? playerMaterial : material, 0);

        List<Matrix4x4> env = new List<Matrix4x4>();
        for (int i = 0; i < matrices.Count; i++) 
        {
            int id = colliderIds[i];
            if (i != playerIndex && !hiddenInstances.Contains(id)) env.Add(matrices[i]);
        }

        Matrix4x4[] arr = env.ToArray();
        for (int i = 0; i < arr.Length; i += 1023) {
            int sz = Mathf.Min(1023, arr.Length - i);
            Matrix4x4[] batch = new Matrix4x4[sz];
            System.Array.Copy(arr, i, batch, 0, sz);
            Graphics.DrawMeshInstanced(cubeMesh, 0, material, batch, sz);
        }
    }

    public void TakeDamage(int amount = 1)
    {
        if (isGameOver) return;

        if (isInvincible && amount <= 1) return;
        lives -= amount;
        
        if (lives <= 0) 
        {
            lives = 0;
            isGameOver = true;
            return;
        }
        
        SetInvincibility(2f);
        playerPosition = lastSafePlatformPosition;
        playerVelocity = Vector3.zero;
        currentJumps = 0;
    }

    public void Instakill()
    {
        TakeDamage(lives);
    }

    public void Win()
    {
        if (hasWon || isGameOver) return;
        hasWon = true;
    }

    public bool HasWon() => hasWon;
    public bool IsGameOver() => isGameOver;

    public void ResetGame()
    {
        hasWon = false;
        isGameOver = false;
        lives = 3;
        points = 0;
        playerPosition = spawnPosition;
        lastSafePlatformPosition = spawnPosition;
        playerVelocity = Vector3.zero;
        currentJumps = 0;
        
        hasDoubleJump = false;
        hasDash = false;
        hasWallClimb = false;
        
        hiddenInstances.Clear();
        disappearingTimers.Clear();
        
        announcementTimer = 0;
        lastPowerupName = "";
        
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    public void AddLife() => lives++;
    public void SetInvincibility(float duration) { isInvincible = true; invincibilityTimer = duration; }
    public bool IsInvincible() => isInvincible;

    public void NotifyPowerup(string name)
    {
        lastPowerupName = name;
        announcementTimer = 3f;
    }

    public string GetAnnouncement() => announcementTimer > 0 ? lastPowerupName : "";
    public float GetDashCooldownRatio() => dashCooldownTimer > 0 ? (dashCooldownTimer / (dashCooldown + dashDuration)) : 0f;
    public float GetJumpCooldownRatio() => jumpCooldownTimer > 0 ? (jumpCooldownTimer / jumpCooldown) : 0f;
    public float GetWallJumpCooldownRatio() => wallJumpCooldownTimer > 0 ? (wallJumpCooldownTimer / wallJumpCooldown) : 0f;
}
