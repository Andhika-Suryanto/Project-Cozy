using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(RCC_AICarController))]
public class RCCSteeringOverride : MonoBehaviour
{
    private RCC_AICarController aiController;
    private NavMeshAgent navigator;
    
    [Header("Steering Override Settings")]
    public bool enableSteeringOverride = true;
    public bool enableDebugLogging = true;
    public float steeringMultiplier = 1.5f;
    
    void Start()
    {
        aiController = GetComponent<RCC_AICarController>();
        navigator = GetComponentInChildren<NavMeshAgent>();
    }
    
    void LateUpdate()
    {
        if (!enableSteeringOverride || navigator == null || aiController == null) return;
        
        // Override steering calculation in LateUpdate (after RCC calculates)
        OverrideSteering();
    }
    
    void OverrideSteering()
    {
        if (!navigator.isOnNavMesh || aiController.waypointsContainer == null) return;
        
        // Get current waypoint
        if (aiController.currentWaypointIndex >= aiController.waypointsContainer.waypoints.Count) return;
        
        var currentWaypoint = aiController.waypointsContainer.waypoints[aiController.currentWaypointIndex];
        Vector3 targetPos = currentWaypoint.transform.position;
        
        // Calculate direct steering to waypoint
        Vector3 directionToWaypoint = (targetPos - transform.position).normalized;
        Vector3 localDirection = transform.InverseTransformDirection(directionToWaypoint);
        
        float directSteerInput = Mathf.Clamp(localDirection.x * steeringMultiplier, -1f, 1f);
        
        // Apply steering based on distance and angle
        float distanceToWaypoint = Vector3.Distance(transform.position, targetPos);
        
        if (distanceToWaypoint > 2f) // Only steer if we're not at the waypoint
        {
            // Override RCC's steering if it's not working
            if (Mathf.Abs(aiController.steerInput) < 0.1f && Mathf.Abs(directSteerInput) > 0.1f)
            {
                aiController.steerInput = directSteerInput;
                
                if (enableDebugLogging)
                {
                    Debug.Log($"STEERING OVERRIDE: {directSteerInput:F2} (Distance: {distanceToWaypoint:F1})");
                }
            }
            
            // Also ensure throttle is applied
            if (aiController.throttleInput < 0.1f)
            {
                aiController.throttleInput = 0.8f;
                
                if (enableDebugLogging)
                {
                    Debug.Log("THROTTLE OVERRIDE: Applied 0.8");
                }
            }
        }
        
        // Debug current state
        if (enableDebugLogging && Time.frameCount % 60 == 0) // Log every 60 frames
        {
            Debug.Log($"=== STEERING DEBUG ===");
            Debug.Log($"RCC Steer Input: {aiController.steerInput:F2}");
            Debug.Log($"Direct Steer Calc: {directSteerInput:F2}");
            Debug.Log($"Navigator Desired Velocity: {navigator.desiredVelocity}");
            Debug.Log($"Distance to waypoint: {distanceToWaypoint:F1}");
            Debug.Log($"Car speed: {aiController.CarController.speed:F1}");
        }
    }
    
    // Manual steering test
    [ContextMenu("Test Manual Steering")]
    void TestManualSteering()
    {
        if (aiController.waypointsContainer == null || aiController.waypointsContainer.waypoints.Count == 0)
        {
            Debug.LogError("No waypoints available for testing!");
            return;
        }
        
        var waypoint = aiController.waypointsContainer.waypoints[aiController.currentWaypointIndex];
        Vector3 direction = (waypoint.transform.position - transform.position).normalized;
        Vector3 localDir = transform.InverseTransformDirection(direction);
        
        float testSteer = Mathf.Clamp(localDir.x, -1f, 1f);
        
        Debug.Log($"Manual test steering: {testSteer:F2}");
        Debug.Log($"Waypoint position: {waypoint.transform.position}");
        Debug.Log($"Car position: {transform.position}");
        Debug.Log($"Direction: {direction}");
        Debug.Log($"Local direction: {localDir}");
        
        // Apply test steering for 2 seconds
        StartCoroutine(ApplyTestSteering(testSteer));
    }
    
    System.Collections.IEnumerator ApplyTestSteering(float steerValue)
    {
        float startTime = Time.time;
        while (Time.time - startTime < 2f)
        {
            aiController.steerInput = steerValue;
            aiController.throttleInput = 0.8f;
            yield return null;
        }
        
        Debug.Log("Test steering complete");
    }
}