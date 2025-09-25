using UnityEngine;

[RequireComponent(typeof(RCC_AICarController))]
public class DirectRCCController : MonoBehaviour
{
    private RCC_AICarController aiController;
    private RCC_CarControllerV4 carController;
    
    [Header("Direct Control Settings")]
    public bool enableDirectControl = true;
    public bool bypassAICompletely = false;
    public float steeringSpeed = 2f;
    
    private float currentSteerInput = 0f;
    
    void Start()
    {
        aiController = GetComponent<RCC_AICarController>();
        carController = GetComponent<RCC_CarControllerV4>();
        
        if (carController != null)
        {
            // Ensure external control is enabled
            carController.externalController = true;
            Debug.Log("Direct RCC Controller initialized");
        }
    }
    
    void Update()
    {
        if (!enableDirectControl || carController == null || aiController == null) return;
        
        if (bypassAICompletely)
        {
            // Completely bypass AI and control directly
            ControlCarDirectly();
        }
        else
        {
            // Override AI inputs after they're calculated
            OverrideAIInputs();
        }
    }
    
    void ControlCarDirectly()
    {
        if (aiController.waypointsContainer == null || aiController.waypointsContainer.waypoints.Count == 0)
            return;
        
        // Get current waypoint
        var waypoint = aiController.waypointsContainer.waypoints[aiController.currentWaypointIndex];
        Vector3 targetPos = waypoint.transform.position;
        float distance = Vector3.Distance(transform.position, targetPos);
        
        // Calculate steering
        Vector3 direction = (targetPos - transform.position).normalized;
        Vector3 localDirection = transform.InverseTransformDirection(direction);
        float targetSteer = Mathf.Clamp(localDirection.x * 2f, -1f, 1f);
        
        // Smooth steering
        currentSteerInput = Mathf.MoveTowards(currentSteerInput, targetSteer, Time.deltaTime * steeringSpeed);
        
        // Apply inputs directly to CarController
        carController.steerInput = currentSteerInput;
        carController.throttleInput = 0.8f; // Constant throttle
        carController.brakeInput = 0f;
        carController.handbrakeInput = 0f;
        
        // Check if we reached the waypoint
        if (distance < waypoint.radius)
        {
            aiController.currentWaypointIndex++;
            if (aiController.currentWaypointIndex >= aiController.waypointsContainer.waypoints.Count)
            {
                aiController.currentWaypointIndex = 0;
            }
            Debug.Log($"Reached waypoint, moving to {aiController.currentWaypointIndex}");
        }
        
        // Debug every 30 frames
        if (Time.frameCount % 30 == 0)
        {
            Debug.Log($"DIRECT CONTROL - Steer: {currentSteerInput:F2}, Target: {targetSteer:F2}, Distance: {distance:F1}");
        }
    }
    
    void OverrideAIInputs()
    {
        // Force the inputs we want onto the CarController
        if (aiController.waypointsContainer == null || aiController.waypointsContainer.waypoints.Count == 0)
            return;
        
        var waypoint = aiController.waypointsContainer.waypoints[aiController.currentWaypointIndex];
        Vector3 targetPos = waypoint.transform.position;
        Vector3 direction = (targetPos - transform.position).normalized;
        Vector3 localDirection = transform.InverseTransformDirection(direction);
        float directSteer = Mathf.Clamp(localDirection.x * 1.5f, -1f, 1f);
        
        // FORCE the inputs (override whatever RCC AI calculated)
        carController.steerInput = directSteer;
        carController.throttleInput = 0.8f;
        carController.brakeInput = 0f;
        carController.handbrakeInput = 0f;
        
        // Debug every 30 frames
        if (Time.frameCount % 30 == 0)
        {
            Debug.Log($"INPUT OVERRIDE - Forced Steer: {directSteer:F2}, AI Steer: {aiController.steerInput:F2}");
        }
    }
    
    void LateUpdate()
    {
        // Final override in LateUpdate to ensure nothing else changes our inputs
        if (enableDirectControl && carController != null)
        {
            // Make sure external controller stays enabled
            if (!carController.externalController)
            {
                carController.externalController = true;
                Debug.Log("Re-enabled external controller");
            }
        }
    }
    
    [ContextMenu("Test Direct Steering")]
    void TestDirectSteering()
    {
        if (carController != null)
        {
            StartCoroutine(TestSteeringCoroutine());
        }
    }
    
    System.Collections.IEnumerator TestSteeringCoroutine()
    {
        Debug.Log("Testing direct steering - LEFT");
        
        // Test left steering
        for (float t = 0; t < 2f; t += Time.deltaTime)
        {
            carController.steerInput = -1f; // Full left
            carController.throttleInput = 0.5f;
            carController.brakeInput = 0f;
            carController.handbrakeInput = 0f;
            yield return null;
        }
        
        Debug.Log("Testing direct steering - RIGHT");
        
        // Test right steering
        for (float t = 0; t < 2f; t += Time.deltaTime)
        {
            carController.steerInput = 1f; // Full right
            carController.throttleInput = 0.5f;
            carController.brakeInput = 0f;
            carController.handbrakeInput = 0f;
            yield return null;
        }
        
        Debug.Log("Testing direct steering - STRAIGHT");
        
        // Return to center
        carController.steerInput = 0f;
        carController.throttleInput = 0f;
        carController.brakeInput = 0f;
        carController.handbrakeInput = 1f;
        
        Debug.Log("Direct steering test complete");
    }
}