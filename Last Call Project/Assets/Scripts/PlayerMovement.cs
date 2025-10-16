using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public MobileSwipe input;
    public CharacterController controller; // Reference to the CharacterController component

    [Header("Lane Settings")]
    public int laneCount = 3; // Number of lanes: Toujours impair pour avoir une voie centrale
    public float laneWidth = 1.0f; // Distance entre les voies
    public float laneChangeSpeed = 12f; //Vitesse latérale (Lerp)
    public int currentLaneIndex = 0; // Index de la voie actuelle (0 = centre, -1 = gauche, 1 = droite)
    private float targetXPosition; // Position X cible pour le changement de voie

    [Header("Forward Motion")]
    public float startSpeed = 5f; // Vitesse de départ
    public float maxSpeed = 14f; // Vitesse maximale
    public float speedIncreaseRate = 0.15f; // Taux d'augmentation de la vitesse
    public float forwardSpeed; // Vitesse de déplacement vers l'avant

    [Header("Jump Settings")]
    public float jumpHeight = 1.0f; // Hauteur du saut
    public float gravity = -30f; // Gravité appliquée au personnage
    private float verticalVelocity; // Vitesse verticale actuelle
    public float coyoteTime = 0.08f; // marge juste après avoir quitté le sol
    private float lastGroundedTime; // Dernier moment où le personnage était au sol

    [Header("Slide Settings")]
    public float slideDuration = 0.75f; // Durée du slide
    public float slideHeight = 0.3f; // Hauteur du personnage pendant le slide
    private bool isSliding = false; // Indique si le personnage est en train de glisser
    private float defaultHeight; // Hauteur par défaut du personnage
    private Vector3 defaultCenter; // Centre par défaut du CharacterController

    [Header("Ground Check")]
    public float groundCheckOffset = 0.05f; // Offset pour la vérification du sol

    [Header("Visual Tilt")]
    public Transform visual; // Référence au transform visuel du personnage
    public float tiltAngle = 8f; // Angle d'inclinaison lors des changements de voie
    public float tiltSpeed = 12f; // Vitesse d'inclinaison


    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (input == null) input = FindObjectOfType<MobileSwipe>();

        //Init lanes (central lane)
        currentLaneIndex = 0;
        targetXPosition = 0f;

        //Vitesse avant
        forwardSpeed = startSpeed;

        //Default values for slide
        defaultHeight = controller.height;
        defaultCenter = controller.center;
    }

    // Update is called once per frame
    void Update()
    {
        // 1) Read input (mobile swipe)
        ReadInputs();

        // 2) Accelerate forward speed
        forwardSpeed = Mathf.Min(forwardSpeed + speedIncreaseRate * Time.deltaTime, maxSpeed);

        // 3) Calcul movement
        Vector3 moveVector = Vector3.zero;

        // 4) Lane change (X axis)
        moveVector.z = forwardSpeed;

        // Calculate target X position based on current lane
        float desiredXPosition = targetXPosition;
        float newXPosition = Mathf.Lerp(transform.position.x, desiredXPosition, laneChangeSpeed * Time.deltaTime);
        moveVector.x = (newXPosition - transform.position.x) / Time.deltaTime;

        //Vertical movement (Y axis)
        bool isGrounded = controller.isGrounded || IsAlmostGrounded();

        if (isGrounded)
        {
            lastGroundedTime = Time.time;

            if (verticalVelocity < 0f)
                verticalVelocity = -2f; // Small downward force to keep grounded

            // Jump
            if (wantJump && !isSliding)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                wantJump = false;
            }
        }
        else
        {
            // Coyote time allows jumping shortly after leaving the ground
            if (wantJump && (Time.time - lastGroundedTime) <= coyoteTime && !isSliding)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                wantJump = false;
            }
        }

        // Apply gravity
        verticalVelocity += gravity * Time.deltaTime;
        moveVector.y = verticalVelocity;

        // 4) Apply Movement
        controller.Move(moveVector * Time.deltaTime);

        // 5) Visual tilt for lane changes
        UpdateTilt(newXPosition);

        // 6) Reset input flags
        wantJump = false;
        wantSlide = false;
    }

        bool IsAlmostGrounded()
        {
            // Raycast just below the character to check for ground
            Ray ray = new Ray(transform.position + Vector3.up * 0.1f, Vector3.down);
            return Physics.SphereCast(ray, controller.radius * 0.95f, out _, 0.12f + groundCheckOffset);
        }

        // ---- INPUT -> lOGIQUE ----
        private bool wantJump, wantSlide;

        void ReadInputs()
        {
            if (input == null) return;

            if (input.SwipeUp) wantJump = true;
            if (input.SwipeDown) TrySlide();
            if (input.SwipeLeft) ChangeLane(-1);
            if (input.SwipeRight) ChangeLane(1);
        }

    void ChangeLane(int dir)
    {
        int half = (laneCount - 1) / 2; // Half the number of lanes on one side
        int newIndex = Mathf.Clamp(currentLaneIndex + dir, -half, half); // Calculate new lane index and clamp it within valid range
        if (newIndex == currentLaneIndex) return; // No change

        currentLaneIndex = newIndex;
        targetXPosition = currentLaneIndex * laneWidth; // Update target X position based on lane index
    }

    void TrySlide()
    {
        if (isSliding) return;

        bool groundedish = controller.isGrounded || IsAlmostGrounded() || (Time.time - lastGroundedTime) <= 0.12f;
        if (!groundedish) return;

        StartCoroutine(CoSlide());
    }

    System.Collections.IEnumerator CoSlide()
    {
        verticalVelocity = Mathf.Min(verticalVelocity, -6f); // force une accroche au sol

        isSliding = true;

        // Reduction du hitbox
        controller.height = slideHeight;
        controller.center = new Vector3(defaultCenter.x, slideHeight * 0.5f, defaultCenter.z);

        // Annule un saut en cours
        if (verticalVelocity > 0f) verticalVelocity = 0f;

        //Incliner le visuel pendant le slide
        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.deltaTime;
            yield return null;

        }

        //Revenir aux valeurs par défaut
        controller.height = defaultHeight;
        controller.center = defaultCenter;

        isSliding = false;
    }

    void UpdateTilt(float newXPosition)
    {
        if (visual == null) return;

        // Calculate target tilt based on lane change direction
        float dx = (newXPosition - transform.position.x); // Normalized change in position
        float targetTilt = Mathf.Clamp(-dx * tiltAngle * 2f, -tiltAngle, tiltAngle);

        Quaternion targetRotation = Quaternion.Euler(0f, 0f, -targetTilt);
        // Smoothly interpolate to the target rotation
        visual.localRotation = Quaternion.Slerp(visual.localRotation, targetRotation, tiltSpeed * Time.deltaTime);
    }

    // Gizmos for lane positions
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        int half = (laneCount - 1) / 2;
        for (int i = -half; i <= half; i++)
        {
            Vector3 pos = transform.position;
            float xPos = i * laneWidth;
            float yPos = transform.position.y + 0.01f;
            Gizmos.DrawWireCube(pos + Vector3.forward * -1f, pos + Vector3.forward * 1f);
        }


    }

// Start is called once before the first execution of Update after the MonoBehaviour is created
void Start()
    {
        
    }


}
