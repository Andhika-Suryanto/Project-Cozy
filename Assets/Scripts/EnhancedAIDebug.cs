using UnityEngine;
using UnityEngine.AI;

public class EnhancedAIDebug : MonoBehaviour
{
    private RCC_AICarController aiController;
    private NavMeshAgent navigator;
    
    void Start()
    {
        aiController = GetComponent<RCC_AICarController>();
        navigator = GetComponentInChildren<NavMeshAgent>();
    }
    
    void Update()
    {
        if (aiController == null || navigator == null) 
        {
            Debug.LogError("Missing components!");
            return;
        }
        
        // Critical NavMesh checks
        Debug.Log($"=== NAVMESH STATUS ===");
        Debug.Log($"Navigator on NavMesh: {navigator.isOnNavMesh}");
        Debug.Log($"Navigator enabled: {navigator.enabled}");
        Debug.Log($"Has path: {navigator.hasPath}");
        Debug.Log($"Path status: {navigator.pathStatus}");
        Debug.Log($"Navigator position: {navigator.transform.position}");
        
        if (navigator.isOnNavMesh)
        {
            Debug.Log($"=== NAVIGATION DATA ===");
            Debug.Log($"Desired Velocity: {navigator.desiredVelocity}");
            Debug.Log($"Desired Velocity Magnitude: {navigator.desiredVelocity.magnitude}");
            
            // Calculate what the steering should be
            Vector3 localDesiredVel = transform.InverseTransformDirection(navigator.desiredVelocity);
            Debug.Log($"Local Desired Velocity: {localDesiredVel}");
            Debug.Log($"Raw Navigator Input (X): {localDesiredVel.x}");
            
            float navigatorInput = Mathf.Clamp(localDesiredVel.x * 1f, -1f, 1f);
            if (navigatorInput > .4f) navigatorInput = 1f;
            if (navigatorInput < -.4f) navigatorInput = -1f;
            Debug.Log($"Final Navigator Input: {navigatorInput}");
        }
        
        // AI Controller status
        Debug.Log($"=== AI CONTROLLER STATUS ===");
        Debug.Log($"Current Waypoint Index: {aiController.currentWaypointIndex}");
        Debug.Log($"Navigation Mode: {aiController.navigationMode}");
        Debug.Log($"Steer Input: {aiController.steerInput}");
        Debug.Log($"Throttle Input: {aiController.throttleInput}");
        Debug.Log($"Use Raycasts: {aiController.useRaycasts}");
        
        // Waypoint container check
        if (aiController.waypointsContainer != null)
        {
            Debug.Log($"=== WAYPOINT DATA ===");
            Debug.Log($"Waypoints count: {aiController.waypointsContainer.waypoints.Count}");
            
            if (aiController.waypointsContainer.waypoints.Count > 0 && 
                aiController.currentWaypointIndex < aiController.waypointsContainer.waypoints.Count)
            {
                var currentWaypoint = aiController.waypointsContainer.waypoints[aiController.currentWaypointIndex];
                Vector3 targetPos = currentWaypoint.transform.position;
                float distance = Vector3.Distance(transform.position, targetPos);
                Debug.Log($"Target waypoint position: {targetPos}");
                Debug.Log($"Distance to waypoint: {distance}");
                Debug.Log($"Waypoint radius: {currentWaypoint.radius}");
                
                // Check if waypoint is on NavMesh
                NavMeshHit hit;
                if (NavMesh.SamplePosition(targetPos, out hit, 5f, NavMesh.AllAreas))
                {
                    Debug.Log($"Waypoint IS on NavMesh");
                    Debug.Log($"NavMesh hit point: {hit.position}");
                }
                else
                {
                    Debug.LogError($"Waypoint is NOT on NavMesh!");
                }
            }
        }
        else
        {
            Debug.LogError("No waypoint container found!");
        }
        
        // Car controller status
        var carController = aiController.CarController;
        if (carController != null)
        {
            Debug.Log($"=== CAR CONTROLLER STATUS ===");
            Debug.Log($"Can Control: {carController.canControl}");
            Debug.Log($"External Controller: {carController.externalController}");
            Debug.Log($"Actual Steer Input: {carController.steerInput}");
            Debug.Log($"Car Speed: {carController.speed}");
        }
    }
    
    void OnDrawGizmos()
    {
        if (navigator != null && navigator.isOnNavMesh)
        {
            // Draw the path
            if (navigator.hasPath)
            {
                Gizmos.color = Color.yellow;
                Vector3[] corners = navigator.path.corners;
                for (int i = 0; i < corners.Length - 1; i++)
                {
                    Gizmos.DrawLine(corners[i], corners[i + 1]);
                }
            }
            
            // Draw desired velocity
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, navigator.desiredVelocity);
            
            // Draw current destination
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(navigator.destination, 2f);
        }
    }
}