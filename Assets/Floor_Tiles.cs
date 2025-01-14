using UnityEngine;

public class Floor_Tiles : MonoBehaviour
{
    public enum TileType
    {
        Normal,
        Color,
        Moving,
        SavePoint,
        Finish
    }

    [Header("References")]
    [SerializeField] private TileType tileType;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GameManager gameManager;

    [Header("Color Tile Settings")]
    [SerializeField] private bool isRedTile;
    [SerializeField] private Color redColor = Color.red;
    [SerializeField] private Color blueColor = Color.blue;

    [Header("Moving Tile Settings")]
    [SerializeField] private Vector3 moveDirection = Vector3.forward; // Direction to move
    [SerializeField] private float moveSpeed = 2f;                    // Speed of movement
    [SerializeField] private float moveDuration = 2f;                // How long to move in one direction

    [Header("SavePoint Settings")]
    [SerializeField] private Color savePointActiveColor = Color.green;
    [SerializeField] private Color savePointInactiveColor = Color.grey;
    private bool isSavePointActive = false;

    private MeshRenderer tileRenderer;
    private Transform parentTransform;  // Reference to the parent prefab's transform
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float moveTimer;
    private bool movingToTarget;

    void Start()
    {
        if (playerController == null)
        {
            playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        }

        if (gameManager == null)
        {
            // Using Find by name - make sure your GameManager object is named exactly "GameManager"
            GameObject gameManagerObject = GameObject.Find("GameManager");
            if (gameManagerObject != null)
            {
                gameManager = gameManagerObject.GetComponent<GameManager>();
            }
            else
            {
                Debug.LogWarning("Could not find GameManager object in scene!");
            }
        }

        tileRenderer = GetComponent<MeshRenderer>();
        parentTransform = transform.parent;
        startPosition = parentTransform.position;

        InitializeTile();
    }

    void InitializeTile()
    {
        switch (tileType)
        {
            case TileType.Color:
                SetTileColor();
                break;
            case TileType.Moving:
                InitializeMovingTile();
                break;
            case TileType.SavePoint:
                InitializeSavePoint();
                break;
        }
    }

    private void InitializeMovingTile()
    {
        moveTimer = 0f;
        movingToTarget = true;
        // Calculate target position based on direction and speed
        targetPosition = startPosition + (moveDirection.normalized * moveSpeed * moveDuration);
    }

    void Update()
    {
        if (tileType == TileType.Moving)
        {
            HandleMovingTile();
        }
    }

    private void HandleMovingTile()
    {
        moveTimer += Time.deltaTime;

        if (moveTimer >= moveDuration)
        {
            // Reset timer and switch direction
            moveTimer = 0f;
            movingToTarget = !movingToTarget;
        }

        // Calculate current position
        float normalizedTime = moveTimer / moveDuration;
        Vector3 currentTarget = movingToTarget ? targetPosition : startPosition;
        Vector3 startPos = movingToTarget ? startPosition : targetPosition;

        // Lerp between positions - moving the parent transform
        parentTransform.position = Vector3.Lerp(startPos, currentTarget, normalizedTime);
    }

    private void SetTileColor()
    {
        if (tileRenderer != null)
        {
            tileRenderer.material.color = isRedTile ? redColor : blueColor;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (tileType == TileType.Moving)
            {
                collision.transform.SetParent(parentTransform);
            }
            else if (tileType == TileType.Color)
            {
                CheckColorMatch();
            }
            else if (tileType == TileType.SavePoint)
            {
                HandleSavePointCollision();
            }
            else if (tileType == TileType.Finish) // Add this condition
            {
                if (gameManager != null)
                {
                    gameManager.Finish(); // Call the Finish method
                    Debug.Log("Player reached finish line!");
                }
                else
                {
                    Debug.LogWarning("GameManager reference is missing!");
                }
            }
        }
    }

    private void HandleSavePointCollision()
    {
        ActivateSavePoint();
        // Assuming PlayerController has a method to set spawn point
        playerController.SetSpawnPoint(transform.position);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (tileType == TileType.Moving)
            {
                // Remove the parent-child relationship
                collision.transform.SetParent(null);
            }
        }
    }

    private void CheckColorMatch()
    {
        bool isPlayerRed = playerController.isPlayer1Turn;

        if (isPlayerRed != isRedTile)
        {
            playerController.OnPlayerDeath();
            Debug.Log($"Player died! Player color: {(isPlayerRed ? "Red" : "Blue")}, Tile color: {(isRedTile ? "Red" : "Blue")}");
        }
        else
        {
            Debug.Log($"Safe! Colors match. Player color: {(isPlayerRed ? "Red" : "Blue")}, Tile color: {(isRedTile ? "Red" : "Blue")}");
        }
    }

    private void InitializeSavePoint()
    {
        if (tileRenderer != null)
        {
            tileRenderer.material.color = savePointInactiveColor;
        }
    }

    public void ActivateSavePoint()
    {
        if (tileType == TileType.SavePoint)
        {
            // Deactivate all other save points
            Floor_Tiles[] allSavePoints = FindObjectsOfType<Floor_Tiles>();
            foreach (Floor_Tiles tile in allSavePoints)
            {
                if (tile.tileType == TileType.SavePoint && tile != this)
                {
                    tile.DeactivateSavePoint();
                }
            }

            // Activate this save point
            isSavePointActive = true;
            if (tileRenderer != null)
            {
                tileRenderer.material.color = savePointActiveColor;
            }
        }
    }

    public void DeactivateSavePoint()
    {
        if (tileType == TileType.SavePoint)
        {
            isSavePointActive = false;
            if (tileRenderer != null)
            {
                tileRenderer.material.color = savePointInactiveColor;
            }
        }
    }
}
