using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Superclass for all traffic agents
/// </summary>
public class TrafficAgent : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Range(0, 100)]
    public float baseSpeed = 2f; //Units per second
    public float speed = 0f;
    public float acceleration = 0f;
    float waitBuffer = 0;
    public float rotateSpeedMultiplier = 2f;

    [Header("WayPoints")]
    public float wayPointTolerance = 0.7f; //Units
    public TrafficWayPoint lastWayPoint;
    public TrafficWayPoint nextWayPoint;

    [Header("Obstacle Detection")]
    public int rightOfWayPriority = 0;
    public float distanceAtStop = 1f;
    public string[] obstacleTags;
    private List<string> obstacleTagList;
    public bool isDetectingObstacleAhead = false;
    public Transform obstacleAhead;
    public float distanceToObstacleAhead = Mathf.Infinity;
    float distanceAtDetection = Mathf.Infinity;
    bool isStopped = false;
    public bool isStoppedAtObstacle = false;

    private void Awake()
    {
        CheckWayPoints();
        speed = baseSpeed;
    }

    protected virtual void Update()
    {
        if (CheckWayPoints())
        {
            //Update direction
            UpdateDirection();

            //Move
            transform.Translate(transform.forward * GetUpdatedSpeed() * Time.deltaTime, Space.World);
        }

        if(isDetectingObstacleAhead)
        {
            // Track distance to obstacle ahead
            distanceToObstacleAhead = Vector3.Distance(transform.position, obstacleAhead.position);

            // Prevent perpetually locked bug
            if (!ObstacleIsAhead(obstacleAhead.transform))
                UnRegisterObstacleAhead();
        }


        if (!isStopped && CheckStopped())
        {
            isStopped = true;
            OnStopMoving();

            if (!isStoppedAtObstacle && CheckStoppedAtObstacle())
            {
                isStoppedAtObstacle = true;
                OnStopMovingAtObstacle(obstacleAhead);
            }
        }

        if(isStopped && !CheckStopped())
        {
            isStopped = false;
            OnStartMoving();

            if (isStoppedAtObstacle && !CheckStoppedAtObstacle())
            {
                isStoppedAtObstacle = false;
                OnStartMovingAtObstacle(obstacleAhead);
            }
        }
    }

    protected virtual void OnStopMoving()
    {

    }

    protected virtual void OnStopMovingAtObstacle(Transform Obstacle)
    {

    }

    protected virtual void OnStartMoving()
    {

    }

    protected virtual void OnStartMovingAtObstacle(Transform obstacle)
    {

    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!ObstacleIsTracked(other.transform) && IsObstacle(other.gameObject) && ObstacleIsAhead(other.transform))
        {
            if (IsFacingObstacle(other.transform))
            {
                if (!HasRightOfWayOver(other.gameObject)) //this agent doesn't have right of way
                {
                    RegisterObstacleAhead(other.transform);

                } // else continue. The other agent will yield.

            }
            else // is behind obstacle
            {
                RegisterObstacleAhead(other.transform);
            }
        }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (ObstacleIsTracked(other.transform))
        {
            UnRegisterObstacleAhead();
        }
    }

    bool IsObstacle(GameObject obstacle)
    {
        return GetObstacleTagList().Contains(obstacle.tag);
    }

    List<string> GetObstacleTagList()
    {
        if(obstacleTagList == null)
            obstacleTagList = new List<string>(obstacleTags);

        return obstacleTagList;
    }

    /// <summary>
    /// Return true if we have right of way relative to the obstacle
    /// </summary>
    /// <param name="obstacle"></param>
    /// <returns></returns>
    bool HasRightOfWayOver(GameObject obstacle)
    {
        bool verdict = false;

        TrafficAgent otherAgent = obstacle.GetComponent<TrafficAgent>();
        
        if (otherAgent == null) // Always let objects that are not traffic agents go first
        {
            verdict = false;

        } else {

            if (rightOfWayPriority < otherAgent.rightOfWayPriority) {

                verdict = true; // Next higher priority (lower number) goes first


            } else if(rightOfWayPriority > otherAgent.rightOfWayPriority)
            {
                verdict = false;

            } else if (rightOfWayPriority == otherAgent.rightOfWayPriority)
            {
                if ((baseSpeed - otherAgent.baseSpeed) > 0.5)
                {
                    verdict = true; // Let faster agent go first

                }
                else if (Mathf.Abs(baseSpeed - otherAgent.baseSpeed) <= 0.5)
                {
                    verdict = gameObject.GetInstanceID() > obstacle.gameObject.GetInstanceID(); //Coin toss
                }

            }
        }
        /*Debug.Log("Collision 1: " + gameObject.GetInstanceID() +
            " 2: " + obstacle.gameObject.GetInstanceID() +
            " speed 1: " + speed +
            " speed 2: " + obstacle.speed +
            " verdict: " + verdict);*/

        return verdict;
    }

    protected bool ObstacleIsTracked(Transform obstacle)
    {
        return (obstacleAhead != null) && (obstacle.gameObject.GetInstanceID() == obstacleAhead.gameObject.GetInstanceID());
    }

    bool ObstacleIsAhead(Transform obstacle)
    {
        return ObstacleIsAhead(transform, obstacle);
    }

    bool ObstacleIsAhead(Transform agent, Transform other)
    {
        return (Vector3.Dot(agent.forward, other.position - agent.position) > 0);
    }

    bool IsFacingObstacle(Transform obstacle)
    {
        //return (Vector3.Dot(transform.forward, obstacle.forward) <= 0);
        bool otherIsAhead = ObstacleIsAhead(transform, obstacle);
        bool aheadOfOther = ObstacleIsAhead(obstacle, transform);
        return otherIsAhead && aheadOfOther;
    }

    bool CheckStopped()
    {
        return speed <= 0;
    }

    bool CheckStoppedAtObstacle()
    {
        return isDetectingObstacleAhead 
            && distanceToObstacleAhead <= distanceAtStop 
            && CheckStopped();
    }

    protected void RegisterObstacleAhead(Transform obstacle)
    {
        float distance = Vector3.Distance(transform.position, obstacle.position);

        //Register obstacle ahead only if closer
        if (isDetectingObstacleAhead && (distance > distanceToObstacleAhead))
        {
            return;
        } 

        distanceAtDetection = distance;
        obstacleAhead = obstacle;
        distanceToObstacleAhead = distance;
        isDetectingObstacleAhead = true;
    }

    protected void UnRegisterObstacleAhead()
    {
        isDetectingObstacleAhead = false;
        obstacleAhead = null;
        distanceAtDetection = Mathf.Infinity;
        distanceToObstacleAhead = Mathf.Infinity;
    }

    bool CheckWayPoints()
    {
        if(nextWayPoint == null)
            return false; // Eliminates error in first update

        OnCheckWayPoints();

        if (Vector3.Distance(transform.position, nextWayPoint.transform.position) < wayPointTolerance)
        {
            AtWayPoint(nextWayPoint);

            // Update Waypoint or stop
            if(nextWayPoint.nextWayPoint == null)
            {
                return false; //Stop
            } else
            {
                UpdateNextPoint();
            }
        }

        return true; //Keep going
    }

    protected virtual void AtWayPoint(TrafficWayPoint waypoint)
    {

    }

    protected virtual void OnCheckWayPoints()
    {
        //Check for teleport way point
        if (lastWayPoint.isTeleport)
        {
            JumpToNext();
        }
    }

    void JumpToNext()
    {
        transform.position = nextWayPoint.transform.position;
        transform.rotation = nextWayPoint.transform.rotation;

        UpdateNextPoint();
    }

    void UpdateDirection()
    {
        Vector3 direction = nextWayPoint.transform.position - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        //transform.rotation = targetRotation;
        transform.rotation = Quaternion.Lerp(transform.rotation, 
                                             targetRotation, 
                                             Time.deltaTime * GetRotateSpeedFactor());
    }

    void UpdateNextPoint()
    {
        lastWayPoint = nextWayPoint;
        nextWayPoint = nextWayPoint.nextWayPoint;
    }

    float GetUpdatedSpeed()
    {
        speed += GetUpdatedAcceleration();

        NoBackingClamp();

        //Slow down for gradual turns //TODO, convert this to acceleration
        Vector3 direction = nextWayPoint.transform.position - transform.position;
        speed *= Vector3.Dot(direction.normalized, transform.forward);


        return speed;
    }

    void NoBackingClamp()
    {
        if (speed < 0)
        {
            waitBuffer += Time.deltaTime;
            speed = 0;
        }

        if(waitBuffer > 0)
        {
            speed = 0;
        }

        waitBuffer -= Time.deltaTime;
        waitBuffer = Mathf.Clamp(waitBuffer, 0, 5f);
    }

    float GetUpdatedAcceleration()
    {
        acceleration = (baseSpeed - speed) * 0.5f;

        //Slow down for obstacles ahead
        
        if (isDetectingObstacleAhead)
        {
            if (distanceToObstacleAhead <= distanceAtStop)
            {
                acceleration = -Mathf.Infinity;
            }
            else
            {
                acceleration = acceleration = -0.1f / (distanceToObstacleAhead - distanceAtStop)
                              + (0.1f / (distanceAtDetection - distanceAtStop));
                // a = 0 at Detection; a = -inf at Stop
            }

        }

        return acceleration;
    }

    float GetRotateSpeedFactor()
    {
        float factor = rotateSpeedMultiplier * speed;
        factor = Mathf.Max(factor, 2f);
        return factor;
    }

}
