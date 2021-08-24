using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Draw traffic way point gizmos
/// Credit: https://www.youtube.com/watch?v=MXCZ-n5VyJc
/// </summary>
[InitializeOnLoad()]
public class TrafficWayPointEditor
{
    [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable)]
    public static void OnDrawSceneGizmo(TrafficWayPoint wayPoint, GizmoType gizmoType)
    {
        float gizmoAlpha;

        if((gizmoType & GizmoType.Selected) != 0) //Gizmo is selected
        {
            gizmoAlpha = 1f; 
        } else
        {
            gizmoAlpha = 0.5f;
        }

        if((wayPoint.nextWayPoint == null) && 
            (wayPoint.lastWayPoint == null)){
            Gizmos.color = Color.yellow * gizmoAlpha;
        } else if (wayPoint.isTeleport)
        {
            Gizmos.color = Color.cyan * gizmoAlpha;
        } else
        {
            Gizmos.color = Color.blue * gizmoAlpha;
        }

        Gizmos.DrawSphere(wayPoint.transform.position, 0.1f);

        if (!wayPoint.isTeleport)
        {
            Gizmos.color = Color.red * 0.5f;

            if (wayPoint.nextWayPoint != null)
                Gizmos.DrawLine(wayPoint.transform.position, wayPoint.nextWayPoint.transform.position);
        }
    }
}
