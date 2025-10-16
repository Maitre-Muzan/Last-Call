using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [Header("Camera Follow Settings")]
    public Transform target; // The target the camera will follow

    [Header("Offset Settings")]
    public Vector3 offset = new Vector3(0, 2.0f, -2.5f); // Offset from the target

    [Range(1f, 10f)]
    public float smoothSpeed = 5f; // Speed of the camera smoothing

    [Header("Look at the target")]
    public bool lookAtTarget = true; // Whether the camera should look at the target

    private void LateUpdate()
    {
        if (target == null) return; // If there's no target, do nothing

        //Position targeted
        Vector3 targetedPosition = target.position + offset;

        //Smoothly interpolate to the targeted position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetedPosition, smoothSpeed * Time.deltaTime);

        //Apply the smoothed position
        transform.position = smoothedPosition;

        //Make the camera look at the target if enabled
        if (lookAtTarget)
        {
            transform.LookAt(target.position + Vector3.up * 0.3f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(target.position + offset, 0.1f);
        }
    }

    private void Start()
    {
        //Ajuster automatiquement l'orientation
        if (Screen.height > Screen.width)
        {
            //Mode portrait
            offset = new Vector3(0, 2f, -2.5f);
            Camera.main.fieldOfView = 55f;
        }
        else
        {
            //Mode paysage
            offset = new Vector3(0, 1.3f, -3f);
            Camera.main.fieldOfView = 60f;
        }
    }
}