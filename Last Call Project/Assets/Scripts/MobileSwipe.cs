using UnityEngine;

public class MobileSwipe : MonoBehaviour
{
    [Header("Swipe Detection Settings")]
    [Range(0.02f, 0.25f)] public float minSwipePercent = 0.08f; // 8% of screen height
    public float maxSwipeTime = 0.6f; // Maximum time (in seconds) for a swipe to be registered
    [Header("Dominance d'axe (1 = égal, 1.3 = 30% de plus)")]
    public float axisDominance = 1.2f; // Ratio to determine axis dominance

    private Vector2 startPos;
    private float startTime;
    private bool isSwiping;

    // Events for swipe directions
    public bool SwipeLeft { get; private set; }
    public bool SwipeRight { get; private set; }
    public bool SwipeUp { get; private set; }
    public bool SwipeDown { get; private set; }

    void Update()
    {
        //Reset events chaque frame
        SwipeLeft = SwipeRight = SwipeUp = SwipeDown = false;

#if UNITY_EDITOR || UNITY_STANDALONE
        // For testing in the editor or standalone builds using mouse input
        if (Input.GetMouseButtonDown(0)) { isSwiping = true; startPos = Input.mousePosition; startTime = Time.time; }
        else if (Input.GetMouseButtonUp(0) && isSwiping) { DetectSwipe((Vector2)Input.mousePosition); isSwiping = false; }

        // Tactile input for mobile devices
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began) { isSwiping = true; startPos = t.position; startTime = Time.time; }
            else if ((t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) && isSwiping)
            { DetectSwipe(t.position); isSwiping = false; }
        }
#endif

        // Fallback clavier
        if (Input.GetKeyDown(KeyCode.LeftArrow)) SwipeLeft = true;
        if (Input.GetKeyDown(KeyCode.RightArrow)) SwipeRight = true;
        if (Input.GetKeyDown(KeyCode.UpArrow)) SwipeUp = true;
        if (Input.GetKeyDown(KeyCode.DownArrow)) SwipeDown = true;

    }

    void DetectSwipe(Vector2 endPos)
    {
        float dt = Time.time - startTime;
        if (dt > maxSwipeTime) return;

        Vector2 delta = endPos - startPos;

        // Seuil en pixels basé sur la hauteur d'écran
        float minSwipePx = Screen.height * minSwipePercent;
        if (delta.magnitude < minSwipePx) return;

        float ax = Mathf.Abs(delta.x);
        float ay = Mathf.Abs(delta.y);

        if (ay > ax * axisDominance)
        {
            if (delta.y > 0) SwipeUp = true;
            else SwipeDown = true;
        }
        else if (ax > ay * axisDominance)
        {
            if (delta.x > 0) SwipeRight = true;
            else SwipeLeft = true;
        }
    }
}
