using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window for easily editing traffic system, such as WayPoints.
/// Credit: https://www.youtube.com/watch?v=MXCZ-n5VyJc
/// </summary>
public class TrafficManagerWindow : EditorWindow
{
    [MenuItem("Tools/TrafficManager")]
    public static void Open()
    {
        GetWindow<TrafficManagerWindow>();
    }

    public Transform wayPointsContainer;
    public Transform agentsContainer;
    public GameObject[] agentPrefabs;
    public float agentMinSpacing;
    public int numberOfAgents;

    private void OnGUI()
    {
        EditorGUILayout.LabelField("1. Create Way Points");
        SerializedObject window = new SerializedObject(this);
        DrawAddWayPointsLayout(window);

        EditorGUILayout.Space(20f);
        EditorGUILayout.LabelField("Way Points Adjustment Tools");
        DrawAdjustWayPointsLayout(window);

        EditorGUILayout.Space(20f);
        EditorGUILayout.LabelField("2. Spawn Agents");
        DrawSpawnAgentsLayout(window);


        window.ApplyModifiedProperties();
    }

    #region Create way points

    void DrawAddWayPointsLayout(SerializedObject window)
    {
        EditorGUILayout.PropertyField(window.FindProperty("wayPointsContainer"));

        if (wayPointsContainer == null)
        {
            EditorGUILayout.HelpBox("Assign a waypoints container to add waypoints to.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.BeginVertical("box");
            DrawWayPointButtons();
            EditorGUILayout.EndVertical();
        }
    }

    void DrawWayPointButtons()
    {


        if (Selection.activeGameObject != null
               && Selection.activeGameObject.GetComponent<TrafficWayPoint>())
        {
            if (GUILayout.Button("Insert Way Point Before"))
            {
                InsertWayPointBefore();
            }
            if (GUILayout.Button("Insert Way Point After"))
            {
                InsertWayPointAfter();
            }
            if (GUILayout.Button("Remove Way Point"))
            {
                RemoveWayPoint();
            }
        } else {
            if (GUILayout.Button("Add Way Point"))
            {
                AddWayPointAtEnd();
            }
        }
    }

    void InsertWayPointBefore()
    {
        TrafficWayPoint currentWayPoint = CreateWayPoint();

        //Get selected way point
        TrafficWayPoint selectedWayPoint = Selection.activeGameObject
            .GetComponent<TrafficWayPoint>();

        SetWayPointTransform(currentWayPoint, selectedWayPoint);

        //Insert current way point before
        TrafficWayPoint temp = selectedWayPoint.lastWayPoint;
        selectedWayPoint.lastWayPoint = currentWayPoint;
        currentWayPoint.lastWayPoint = temp;
        currentWayPoint.lastWayPoint.nextWayPoint = currentWayPoint;
        currentWayPoint.nextWayPoint = selectedWayPoint;

        //Select current way point
        Selection.activeGameObject = currentWayPoint.gameObject;
    }

    void InsertWayPointAfter()
    {
        TrafficWayPoint currentWayPoint = CreateWayPoint();

        //Get selected way point
        TrafficWayPoint selectedWayPoint = Selection.activeGameObject
            .GetComponent<TrafficWayPoint>();

        SetWayPointTransform(currentWayPoint, selectedWayPoint);

        //Insert current way point after
        TrafficWayPoint temp = selectedWayPoint.nextWayPoint;
        selectedWayPoint.nextWayPoint = currentWayPoint;
        currentWayPoint.nextWayPoint = temp;
        if(currentWayPoint.nextWayPoint != null)
            currentWayPoint.nextWayPoint.lastWayPoint = currentWayPoint;
        currentWayPoint.lastWayPoint = selectedWayPoint;

        //Select current way point
        Selection.activeGameObject = currentWayPoint.gameObject;
    }

    void RemoveWayPoint(){
        //Get selected way point
        TrafficWayPoint selectedWayPoint = Selection.activeGameObject
            .GetComponent<TrafficWayPoint>();
        if(selectedWayPoint.lastWayPoint != null)
            selectedWayPoint.lastWayPoint.nextWayPoint = selectedWayPoint.nextWayPoint;
        
        if(selectedWayPoint.nextWayPoint != null)
            selectedWayPoint.nextWayPoint.lastWayPoint = selectedWayPoint.lastWayPoint;

        DestroyImmediate(selectedWayPoint.gameObject);
    }

    void AddWayPointAtEnd()
    {
        TrafficWayPoint currentWayPoint = CreateWayPoint();

        if (wayPointsContainer.childCount > 1)
        {
            TrafficWayPoint lastWayPoint = wayPointsContainer
                                .GetChild(wayPointsContainer.childCount - 2)
                                .GetComponent<TrafficWayPoint>();

            SetWayPointTransform(currentWayPoint, lastWayPoint);
        } else {
            currentWayPoint.transform.position = new Vector3(0, 0, 0);
        }

        Selection.activeGameObject = currentWayPoint.gameObject;
    }

    TrafficWayPoint CreateWayPoint()
    {
        GameObject wayPointObject = new GameObject(GetWayPointName(wayPointsContainer.childCount),
                                             typeof(TrafficWayPoint));
        wayPointObject.transform.SetParent(wayPointsContainer);
        TrafficWayPoint currentWayPoint = wayPointObject.GetComponent<TrafficWayPoint>();

        return currentWayPoint;
    }

    void SetWayPointTransform(TrafficWayPoint currentWayPoint, TrafficWayPoint referenceWayPoint)
    {
        currentWayPoint.transform.position = referenceWayPoint.transform.position;
        currentWayPoint.transform.rotation = referenceWayPoint.transform.rotation;
    }

    string GetWayPointName(int index){
        return "WayPoint " + index;
    }

    #endregion

    #region Adjust way points 

    void DrawAdjustWayPointsLayout(SerializedObject window)
    {
        if (wayPointsContainer == null)
        {
            EditorGUILayout.HelpBox("Assign a way points container with way points in step 1.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.BeginVertical("box");
            if (GUILayout.Button("Re-align all way point rotations"))
            {
                AlignWayPoints();
            }

            if (GUILayout.Button("Rename all way points in order"))
            {
                RenameWayPoints();
            }
            EditorGUILayout.EndVertical();
        }
    }

    void AlignWayPoints()
    {
        TrafficWayPoint startPoint = wayPointsContainer.GetChild(0).gameObject.GetComponent<TrafficWayPoint>();
        TrafficWayPoint currentPoint = startPoint;
        int count = 0;

        do
        {
            if (currentPoint.nextWayPoint == null)
                break;

            currentPoint.transform.LookAt(currentPoint.nextWayPoint.transform);

            currentPoint = currentPoint.nextWayPoint;
            count++;

        } while (currentPoint != startPoint && count < wayPointsContainer.childCount);
    }

    void RenameWayPoints(){
        TrafficWayPoint startPoint = wayPointsContainer.GetChild(0).gameObject.GetComponent<TrafficWayPoint>();
        TrafficWayPoint currentPoint = startPoint;
        int count = 0;

        do
        {
            currentPoint.gameObject.name = GetWayPointName(count);
            currentPoint.transform.SetSiblingIndex(count);

            if (currentPoint.nextWayPoint == null)
                break;

            currentPoint = currentPoint.nextWayPoint;
            count++;

        } while (currentPoint != startPoint && count < wayPointsContainer.childCount);
    }

    #endregion

    #region Spawn agents

    void DrawSpawnAgentsLayout(SerializedObject window)
    {
        EditorGUILayout.PropertyField(window.FindProperty("agentMinSpacing"));
        EditorGUILayout.PropertyField(window.FindProperty("numberOfAgents"));

        EditorGUILayout.PropertyField(window.FindProperty("agentsContainer"));

        EditorGUILayout.PropertyField(window.FindProperty("agentPrefabs"));
        if (agentPrefabs == null || agentPrefabs.Length <= 0)
        {
            EditorGUILayout.HelpBox("Assign agent prefabs to spawn.", MessageType.Warning);

        }
        else if (agentsContainer == null)
        {
            EditorGUILayout.HelpBox("Assign container to spawn agents into.", MessageType.Warning);
        }
        else if (wayPointsContainer == null)
        {
            EditorGUILayout.HelpBox("Assign container containing way points in step 1.", MessageType.Warning);
        }
        else if (wayPointsContainer.childCount <= 1)
        {
            EditorGUILayout.HelpBox("Add at least 2 way points step 1.", MessageType.Warning);
        }
        {
            EditorGUILayout.BeginVertical("box");
            if (GUILayout.Button("Spawn Agents Along Way Points"))
            {
                SpawnAgents();
            }
            EditorGUILayout.EndVertical();
        }
    }

    void SpawnAgents()
    {
        float[] spawnDistances = GetSpawnDistances();

        TrafficWayPoint startPoint = wayPointsContainer.GetChild(0).gameObject.GetComponent<TrafficWayPoint>();
        TrafficWayPoint currentPoint = startPoint;
        int count = 0;
        float totalDistance = 0;
        int numSpawned = 0;

        do
        {
            if (currentPoint.nextWayPoint == null)
                break;

            float previousTotal = totalDistance;
            float currentDistance = Vector3.Distance(
                                        currentPoint.transform.position,
                                        currentPoint.nextWayPoint.transform.position);
            totalDistance += currentDistance;

            if(totalDistance > spawnDistances[numSpawned])
            {
                float spawnPositionParameter = (spawnDistances[numSpawned] - previousTotal) / currentDistance;
                Debug.Log("Spawn param " + spawnPositionParameter
                    + " spawn dist " + spawnDistances[numSpawned]
                    + " prev total dist " + previousTotal
                    + " curr dist " + currentDistance
                    );
                SpawnAgent(spawnPositionParameter, currentPoint, currentPoint.nextWayPoint);
                numSpawned++;
            }

            Debug.Log("current point not start: " + currentPoint != startPoint
                    + " waypoint count " + count
                    + " numSpawned " + numSpawned);

            currentPoint = currentPoint.nextWayPoint;
            count++;

        } while (currentPoint != startPoint 
                && count < wayPointsContainer.childCount 
                && numSpawned < numberOfAgents);

    }

    void SpawnAgent(float positionParameter, TrafficWayPoint lastWayPoint, TrafficWayPoint nextWayPoint)
    {
        Vector3 spawnPosition = Vector3.Lerp(lastWayPoint.transform.position,
                                      nextWayPoint.transform.position,
                                      positionParameter);
        Quaternion spawnRotation = lastWayPoint.transform.rotation;

        GameObject agentObject = (GameObject) PrefabUtility.InstantiatePrefab(GetAgentPrefab());
        agentObject.transform.SetParent(agentsContainer);
        agentObject.transform.position = spawnPosition;
        agentObject.transform.rotation = spawnRotation;

        TrafficAgent agent = agentObject.GetComponent<TrafficAgent>();

        if (agent)
        {
            agent.lastWayPoint = lastWayPoint;
            agent.nextWayPoint = nextWayPoint;
        }
    }

    GameObject GetAgentPrefab()
    {
        return agentPrefabs[Random.Range(0, agentPrefabs.Length)];
    }

    // Spawn at pseudo random distances along path from the first waypoint point
    float[] GetSpawnDistances()
    {
        float totalPathLength = GetTotalPathLength();
        float[] spawnDistances = new float[numberOfAgents];

        float spacePerAgent = totalPathLength / numberOfAgents;
        float maxRangePerAgent = spacePerAgent - agentMinSpacing;

        Debug.Log("Total Length " + totalPathLength + " space Per " + spacePerAgent + " max range " + maxRangePerAgent);

        if(maxRangePerAgent < 0f)
        {
            maxRangePerAgent = 0f;
            Debug.LogError("Not enough space for " 
                + numberOfAgents + " agents while allowing for " 
                + agentMinSpacing + " min spacing.");
        }

        for (int i = 0; i < spawnDistances.Length; i++)
        {
            // Base position spread evenly along path
            spawnDistances[i] = (i + 0.5f) * spacePerAgent;

            // Add noise without violating spacing requirements
            spawnDistances[i] += Random.Range(-0.5f * maxRangePerAgent,
                                               0.5f * maxRangePerAgent);
        }

        return spawnDistances;
    }

    // Traverse way points and get total path length
    float GetTotalPathLength()
    {
        TrafficWayPoint startPoint = wayPointsContainer.GetChild(0).gameObject.GetComponent<TrafficWayPoint>();
        if(startPoint == null)
        {
            EditorGUILayout.HelpBox("Could not get first way point.", MessageType.Error);
            return 0;
        }

        TrafficWayPoint currentPoint = startPoint;
        int count = 0;
        float totalLength = 0;

        do
        {
            if (currentPoint.nextWayPoint == null)
                break;

            totalLength += Vector3.Distance(
                                        currentPoint.transform.position,
                                        currentPoint.nextWayPoint.transform.position);

            currentPoint = currentPoint.nextWayPoint;
            count++;

        } while (currentPoint != startPoint && count < wayPointsContainer.childCount);

        return totalLength;
    }

    #endregion
}
