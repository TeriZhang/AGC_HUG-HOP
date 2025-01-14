using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Vector3 currentSpawnPoint;
    private bool isDead = false;
    private float deathTimer = 0f;
    private float autoRespawnTime = 8f;

    #region Serialized Fields
    [Header("Turn Control")]
    [SerializeField] public bool isPlayer1Turn = true;

    //[Header("Movement Settings")]
    //[SerializeField] private float rotationSpeed = 180f;

    [Header("Jump Settings")]
    [SerializeField] private float minJumpForce = 5f;
    [SerializeField] private float maxJumpForce = 15f;
    [SerializeField] private float maxChargeTime = 2f;
    [SerializeField] private float jumpAngle = 45f;

    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform characterModel;

    [SerializeField] private GameObject model_1;    
    [SerializeField] private GameObject model_2;    
    [SerializeField] private GameObject model_r_1;  
    [SerializeField] private GameObject model_r_2;

    [Header("Ground Check Settings")]
    [SerializeField] private Transform p1GroundCheckPoint;
    [SerializeField] private Transform p2GroundCheckPoint;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Spin Settings")]
    [SerializeField] private float baseSpinSpeed = 360f;
    [SerializeField] private int baseSpinLoops = 1;
    [SerializeField] private int maxAdditionalLoops = 3;
    #endregion

    #region Private Variables
    // Spin State
    [SerializeField] private bool isSpinning = false;
    private float spinProgress = 0f;
    private float targetSpinAngle = 0f;
    private float currentSpinSpeed = 0f;
    private Quaternion startRotation;
    private Quaternion endRotation;

    // Movement State
    private bool isCharging = false;
    private float chargeStartTime;
    private float currentChargeTime;
    private int facingDirection = 1;
    private bool isGrounded = false;
    //private bool isRotating = false;
    //private float targetRotationY = 0f;
    #endregion

    #region Unity Lifecycle Methods
    private void Start()
    {
        InitializeComponents();
        SetInitialRotation();
        SetInitialModelStates();
        isGrounded = true;
    }
    private void SetInitialModelStates()
    {
        // Set regular models as active
        model_1.SetActive(true);
        model_2.SetActive(true);

        // Set reversed models as inactive
        model_r_1.SetActive(false);
        model_r_2.SetActive(false);
    }

    public void SetSpawnPoint(Vector3 position)
    {
        currentSpawnPoint = position;
        Debug.Log($"New spawn point set at: {position}");
    }
    private void HandleRespawn()
    {
        // Manual respawn with Space
        if (Input.GetKeyDown(KeyCode.X))
        {
            Respawn();
            return;
        }

        // Auto respawn after 8 seconds
        deathTimer += Time.deltaTime;
        if (deathTimer >= autoRespawnTime)
        {
            Respawn();
        }
    }

    // Add this new method
    private void Respawn()
    {
        isDead = false;
        deathTimer = 0f;

        transform.position = currentSpawnPoint;
        transform.rotation = Quaternion.identity;

        model_1.SetActive(true);
        model_2.SetActive(true);
        model_r_1.SetActive(false);
        model_r_2.SetActive(false);

        if (GetComponent<Rigidbody>() != null)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log("Player respawned!");
    }

    public void OnPlayerDeath()
    {
        if (!isDead)
        {
            isDead = true;
            deathTimer = 0f;

            // Your existing death code
            model_1.SetActive(false);
            model_2.SetActive(false);
            model_r_1.SetActive(true);
            model_r_2.SetActive(true);
            isSpinning = false;
            currentSpinSpeed = 0f;

            if (GetComponent<Rigidbody>() != null)
            {
                GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
            }

            Debug.Log("Player died! Press SPACE to respawn or wait 8 seconds for auto-respawn.");
        }
    }

    private void Update()
    {
        if (!isDead)
        {
            UpdateGroundState();
            HandleJumpInput();
            UpdateSpinning();

#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.X))
            {
                Debug.Log("Debug: Player Death Triggered");  
                OnPlayerDeath();
            }
#endif
        }
        else
        {
            // Handle respawn input and timer when dead
            HandleRespawn();
        }
    }

    #endregion

    #region Initialization
    private void InitializeComponents()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
    }

    private void SetInitialRotation()
    {
        if (!isSpinning)
        {
            // Parent (Player) rotation stays at identity
            transform.rotation = Quaternion.identity;

            // Set different rotations based on whose turn it is
            if (isPlayer1Turn)
            {
                // Player 1's turn - normal upright position
                characterModel.rotation = Quaternion.Euler(0, 0, 0);
            }
            else
            {
                // Player 2's turn - rotated 90 degrees counterclockwise
                characterModel.rotation = Quaternion.Euler(0, 0, -90);
            }

            facingDirection = 1; // Always face right
        }
    }
 

    #endregion

    #region Ground Check
    private void UpdateGroundState()
    {
        bool wasGrounded = isGrounded;
        isGrounded = CheckIfGrounded();

        if (!wasGrounded && isGrounded)  // Landing moment
        {
            isSpinning = false;
            SetInitialRotation();
            SwitchTurns();  // Switch turns when landing instead of when jumping
        }
    }

    private bool CheckIfGrounded()
    {
        Transform currentGroundCheck = isPlayer1Turn ? p1GroundCheckPoint : p2GroundCheckPoint;
        return Physics.CheckSphere(currentGroundCheck.position, groundCheckRadius, groundLayer);
    }
    #endregion

    #region Rotation Handling
    //private void HandleRotationInput()
    //{
    //    if (isCharging || isSpinning) return;

    //    if (isPlayer1Turn)
    //    {
    //        if (Input.GetKeyDown(KeyCode.A)) StartRotation(-1);
    //        if (Input.GetKeyDown(KeyCode.D)) StartRotation(1);
    //    }
    //    else
    //    {
    //        if (Input.GetKeyDown(KeyCode.J)) StartRotation(-1);
    //        if (Input.GetKeyDown(KeyCode.L)) StartRotation(1);
    //    }
    //}

    //private void StartRotation(int direction)
    //{
    //    // If trying to turn in the same direction we're already facing, do nothing
    //    if (direction == facingDirection) return;

    //    // Get current Y rotation
    //    float currentYRotation = characterModel.localRotation.eulerAngles.y;

    //    // Debug log to see what's happening
    //    Debug.Log("Current Y Rotation: " + currentYRotation);

    //    // If facing right (0) and turning left, add 180
    //    // If facing left (180) and turning right, subtract 180
    //    if (Mathf.Approximately(currentYRotation, 0f))
    //    {
    //        targetRotationY = 180f;
    //    }
    //    else if (Mathf.Approximately(currentYRotation, 180f) || Mathf.Approximately(currentYRotation, -180f))
    //    {
    //        targetRotationY = 0f;
    //    }
    //    else
    //    {
    //        // If we're in between rotations, determine which way to go
    //        targetRotationY = (currentYRotation < 90f) ? 180f : 0f;
    //    }

    //    // Update facing direction
    //    facingDirection = direction;
    //    isRotating = true;

    //    // Debug log to verify target
    //    Debug.Log("Target Y Rotation: " + targetRotationY);
    //}

    //private void UpdateCharacterRotation()
    //{
    //    if (characterModel != null && isRotating && !isSpinning)
    //    {
    //        // Use localRotation since we're rotating the child model
    //        Quaternion targetRotation = Quaternion.Euler(0, targetRotationY, 0); // Local rotation
    //        characterModel.localRotation = Quaternion.RotateTowards(
    //            characterModel.localRotation,
    //            targetRotation,
    //            rotationSpeed * Time.deltaTime
    //        );

    //        if (Quaternion.Angle(characterModel.localRotation, targetRotation) < 0.1f)
    //        {
    //            characterModel.localRotation = targetRotation;
    //            isRotating = false;
    //        }
    //    }
    //}
    #endregion

    #region Jump Handling
    private void HandleJumpInput()
    {
        bool jumpKeyPressed = isPlayer1Turn ? 
            Input.GetKey(KeyCode.Space) : 
            Input.GetKey(KeyCode.RightShift);

        bool jumpKeyReleased = isPlayer1Turn ? 
            Input.GetKeyUp(KeyCode.Space) : 
            Input.GetKeyUp(KeyCode.RightShift);

        if (jumpKeyPressed && !isCharging && isGrounded)
        {
            StartCharging();
            GameManager.Instance.StartTimer();
        }
        else if (jumpKeyPressed && isCharging)
        {
            ContinueCharging();
        }
        else if (jumpKeyReleased && isCharging)
        {
            ExecuteJump();
        }
    }

    private void StartCharging()
    {
        isCharging = true;
        chargeStartTime = Time.time;
        currentChargeTime = 0f;
    }

    private void ContinueCharging()
    {
        currentChargeTime = Mathf.Min(Time.time - chargeStartTime, maxChargeTime);
    }

    private void ExecuteJump()
    {
        float chargePercent = currentChargeTime / maxChargeTime;
        float jumpForce = Mathf.Lerp(minJumpForce, maxJumpForce, chargePercent);

        float angleInRadians = jumpAngle * Mathf.Deg2Rad;
        Vector3 jumpDirection = new Vector3(
            facingDirection * Mathf.Cos(angleInRadians),
            Mathf.Sin(angleInRadians),
            0f
        );

        StartSpinning(chargePercent);

        rb.velocity = Vector3.zero;
        rb.AddForce(jumpDirection * jumpForce, ForceMode.Impulse);

        isCharging = false;
        // Removed SwitchTurns() from here
    }
    #endregion

    #region Spin Handling
    private void StartSpinning(float chargePercent)
    {
        isSpinning = true;
        Debug.Log("Started spinning with charge: " + chargePercent);  // Debug log

        spinProgress = 0f;
        startRotation = characterModel.localRotation;

        int additionalLoops = Mathf.RoundToInt(chargePercent * maxAdditionalLoops);
        float totalRotation = 360f * (baseSpinLoops + additionalLoops);

        float estimatedAirTime = 2f;
        currentSpinSpeed = totalRotation / estimatedAirTime;

        targetSpinAngle = totalRotation;
    }


    private void UpdateSpinning()
    {
        if (isSpinning)
        {
            

            characterModel.Rotate(Vector3.forward * currentSpinSpeed * Time.deltaTime, Space.Self);
            // Debug.Log("Spinning at speed: " + currentSpinSpeed);  // Uncomment to debug spinning
        }
    }
    #endregion

    #region Turn Management
    private void SwitchTurns()
    {
        isPlayer1Turn = !isPlayer1Turn;
        SetInitialRotation();
        Debug.Log($"Now it's Player {(isPlayer1Turn ? "1" : "2")}'s turn!");
    }
    #endregion

    #region Debug
    //private void OnDrawGizmos()
    //{
    //    // Draw ground check spheres for both players
    //    if (p1GroundCheckPoint != null)
    //    {
    //        // Player 1 ground check visualization
    //        Gizmos.color = (isPlayer1Turn && isGrounded) ? Color.green : Color.red;
    //        Gizmos.DrawWireSphere(p1GroundCheckPoint.position, groundCheckRadius);
    //    }

    //    if (p2GroundCheckPoint != null)
    //    {
    //        // Player 2 ground check visualization
    //        Gizmos.color = (!isPlayer1Turn && isGrounded) ? Color.green : Color.red;
    //        Gizmos.DrawWireSphere(p2GroundCheckPoint.position, groundCheckRadius);
    //    }

    //    if (isCharging)
    //    {
    //        float chargePercent = currentChargeTime / maxChargeTime;
    //        float jumpForce = Mathf.Lerp(minJumpForce, maxJumpForce, chargePercent);

    //        Vector3 start = transform.position;
    //        Vector3 velocity = new Vector3(
    //            facingDirection * Mathf.Cos(jumpAngle * Mathf.Deg2Rad) * jumpForce,
    //            Mathf.Sin(jumpAngle * Mathf.Deg2Rad) * jumpForce,
    //            0
    //        );

    //        Gizmos.color = Color.yellow;
    //        float timestep = 0.02f; // Smaller timestep for more accurate collision detection
    //        float maxTime = 3f;
    //        bool collisionFound = false;
    //        Vector3 collisionPoint = Vector3.zero;

    //        // Create a small sphere to check for collisions
    //        float collisionRadius = 0.5f; // Adjust based on your character's size

    //        for (float t = 0; t < maxTime && !collisionFound; t += timestep)
    //        {
    //            Vector3 end = start + new Vector3(
    //                velocity.x * t,
    //                velocity.y * t - (Physics.gravity.y * t * t) / 2f,
    //                0
    //            );

    //            // Check for collisions with buildings
    //            Collider[] hitColliders = Physics.OverlapSphere(end, collisionRadius);
    //            foreach (Collider collider in hitColliders)
    //            {
    //                if (collider.CompareTag("Building"))
    //                {
    //                    collisionFound = true;
    //                    collisionPoint = end;
    //                    break;
    //                }
    //            }

    //            Gizmos.DrawLine(start, end);

    //            if (collisionFound)
    //            {
    //                // Draw collision indicator
    //                Gizmos.color = Color.red;
    //                Gizmos.DrawWireSphere(collisionPoint, collisionRadius);

    //                // Draw landing point text
    //                UnityEditor.Handles.Label(collisionPoint + Vector3.up, "Impact Point");
    //            }

    //            start = end;
    //        }

    //        // If no collision found, show the end of trajectory
    //        if (!collisionFound)
    //        {
    //            Gizmos.color = Color.gray;
    //            Gizmos.DrawWireSphere(start, collisionRadius);
    //            UnityEditor.Handles.Label(start + Vector3.up, "Max Range");
    //        }

    //        // Draw charge percentage
    //        Vector3 chargeTextPosition = transform.position + Vector3.up * 2f;
    //        UnityEditor.Handles.Label(chargeTextPosition, $"Charge: {(chargePercent * 100):F0}%");
    //    }
    //}
    #endregion
}
