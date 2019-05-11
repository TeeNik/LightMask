using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{

    public float ViewRadius;
    [Range(0, 360)]
    public float ViewAngle;

    public LayerMask targetMask;
    public LayerMask obstacleMask;
    [HideInInspector]
    public List<Transform> VisibleTargets = new List<Transform>();

    public float meshResolution;
    public float EdgeResolveIterations;
    public float EdgeDistThreshold;
    public float maskCutoff = .75f;

    public MeshFilter viewMeshFilter;

    private Mesh _viewMesh;

    private void Start()
    {
        _viewMesh = new Mesh();
        _viewMesh.name = "ViewMesh";
        viewMeshFilter.mesh = _viewMesh;

        //StartCoroutine(FindTargetsWithDelay(.2f));
    }

    IEnumerator FindTargetsWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            FindVisibleTargets();
        }
    }

    private void LateUpdate()
    {
        DrawFOV();
    }

    void FindVisibleTargets()
    {
        VisibleTargets.Clear();
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, ViewRadius, targetMask);
        for(int i = 0; i < targetsInViewRadius.Length; ++i)
        {
            Transform target = targetsInViewRadius[i].transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            if(Vector3.Angle(transform.forward, dirToTarget) < ViewAngle / 2)
            {
                float dstToTarget = Vector3.Distance(transform.position, target.position);
                if(!Physics.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask))
                {
                    VisibleTargets.Add(target);
                }
            }
        }
    }

    void DrawFOV()
    {
        int stepCount = Mathf.RoundToInt(ViewAngle * meshResolution);
        float stepAngleSize = ViewAngle / stepCount;
        List<Vector3> viewPoints = new List<Vector3>();
        ViewCastInfo oldVC = new ViewCastInfo();
        for(int i = 0; i <= stepCount; ++i)
        {
            float angle = transform.eulerAngles.y - ViewAngle / 2 + stepAngleSize * i;
            ViewCastInfo newVC = ViewCast(angle);

            if(i > 0)
            {
                bool distThresholdExceeded = Mathf.Abs(oldVC.Dist - newVC.Dist) > EdgeDistThreshold;
                if(oldVC.Hit != newVC.Hit || (oldVC.Hit == true && newVC.Hit && distThresholdExceeded))
                {
                    EdgeInfo edge = FindEdge(oldVC, newVC);
                    if(edge.PointA != Vector3.zero)
                    {
                        viewPoints.Add(edge.PointA);
                    }
                    if (edge.PointB != Vector3.zero)
                    {
                        viewPoints.Add(edge.PointB);
                    }
                }
            }

            viewPoints.Add(newVC.Point);
            oldVC = newVC;
        }

        int vertexCount = viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];

        vertices[0] = Vector3.zero;
        for(int i = 0; i < vertices.Length - 1; ++i)
        {
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]) + Vector3.forward * maskCutoff;

            if(i < vertexCount - 2)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        _viewMesh.Clear();
        _viewMesh.vertices = vertices;
        _viewMesh.triangles = triangles;
        _viewMesh.RecalculateNormals();
    }

    EdgeInfo FindEdge(ViewCastInfo minVC, ViewCastInfo maxVC)
    {
        float minAngle = minVC.Angle;
        float maxAngle = maxVC.Angle;
        Vector3 minPoint = minVC.Point;
        Vector3 maxPoint = maxVC.Point;

        for(int i = 0; i < EdgeResolveIterations; ++i)
        {
            float angle = (minAngle + maxAngle) / 2;
            ViewCastInfo newVC = ViewCast(angle);

            bool distThresholdExceeded = Mathf.Abs(minVC.Dist - newVC.Dist) > EdgeDistThreshold;
            if (newVC.Hit == minVC.Hit && !distThresholdExceeded)
            {
                minAngle = angle;
                minPoint = newVC.Point;
            }
            else
            {
                maxAngle = angle;
                maxPoint = newVC.Point;
            }
        }
        return new EdgeInfo(minPoint, maxPoint);
    }

    ViewCastInfo ViewCast(float globalAngle)
    {
        Vector3 dir = DirFromAngle(globalAngle, true);
        RaycastHit hit;
        if(Physics.Raycast(transform.position, dir, out hit, ViewRadius, obstacleMask))
        {
            return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
        }
        return new ViewCastInfo(false, transform.position + dir * ViewRadius, ViewRadius, globalAngle);
    }

    public Vector3 DirFromAngle(float angle, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angle += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0, Mathf.Cos(angle * Mathf.Deg2Rad));
    }

    private struct ViewCastInfo
    {
        public bool Hit;
        public Vector3 Point;
        public float Dist;
        public float Angle;

        public ViewCastInfo(bool hit, Vector3 point, float dist, float angle)
        {
            Hit = hit;
            Point = point;
            this.Dist = dist;
            this.Angle = angle;
        }
    }

    private struct EdgeInfo
    {
        public Vector3 PointA;
        public Vector3 PointB;

        public EdgeInfo(Vector3 pointA, Vector3 pointB)
        {
            PointA = pointA;
            PointB = pointB;
        }
    }

}
