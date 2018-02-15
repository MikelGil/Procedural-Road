using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ProceduralRoadV4 : MonoBehaviour {
    public GameObject nodePrefab;
    public GameObject auxNodePrefab;

    public List<Transform> pathNodes;
    public List<Transform> auxNodes;

    public Camera raycastCamera;
    public float width = 5;
    public float heightOffset = 0.5f;
    public float repeatFactor = 1;
    public Material material;
    public float maxSegmentLength = 5;

    //Bezier
    public float interpMinSQRDistance = 10;
    public float interpMaxSQRDistance = 1000;
    public float interpScale = 0.33f;

    //Generar terreno fuera carretera
    public float exceededWidth = 2;

    private Mesh mesh;
    private MeshFilter mf;
    private MeshRenderer mr;

    // Use this for initialization
    void Start() {
        pathNodes = new List<Transform>();
        auxNodes = new List<Transform>();
        mf = gameObject.AddComponent<MeshFilter>();
        mr = gameObject.AddComponent<MeshRenderer>();
        mr.material = material;
        mesh = mf.mesh;
    }

    // Update is called once per frame
    void Update() {
        //Solo cuando se ponga una textura de agua
        if (Input.GetKeyUp(KeyCode.LeftControl)) {
            //Poner Path
            Ray myRay = raycastCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;

            LayerMask layerMask = 1 << LayerMask.NameToLayer("Terrain");
            if (Physics.Raycast(myRay, out hitInfo, 10000, layerMask)) {
                Vector3 hitPosition = hitInfo.point + Vector3.up * heightOffset;

                GameObject go = Instantiate(nodePrefab, hitPosition, Quaternion.identity) as GameObject;
                MoveNodes moveNode = go.AddComponent<MoveNodes>();
                moveNode.Init(raycastCamera, heightOffset, this);
                pathNodes.Add(go.transform);

                Debug.DrawLine(myRay.origin, hitPosition);

                if (pathNodes.Count > 1) {
                    RegenerateAuxNodes();
                }
            }
        }
        if (Input.GetKey(KeyCode.R)) {
            RegenerateAuxNodes();
        }
    }

    public void RegenerateMesh() {
        List<Vector3> sourcePoints = new List<Vector3>();
        List<Vector3> drawingPoints = new List<Vector3>();
        foreach (Transform t in pathNodes) {
            sourcePoints.Add(t.position);
        }
        //Si hay mas de 2 puntos calcula las curvas
        if (pathNodes.Count > 2) {
            BezierPath bezierPath = new BezierPath();
            bezierPath.Interpolate(sourcePoints, interpScale);
            //bezierPath.SamplePoints(sourcePoints, interpMinSQRDistance, interpMaxSQRDistance, interpScale);
            drawingPoints = bezierPath.GetDrawingPoints2();
        }
        //Si son los 2 primeros puntos crea una linea recta
        else {
            drawingPoints = sourcePoints;
        }

        //Clear the mesh before creating a new one
        mesh.Clear();

        //Segment Count
        int segmentCount = drawingPoints.Count - 1;

        // Generate Vertices array
        // Four vertices for each segment + 2 start vertices
        int verticesPerSegment = 4;
        int vertexCount = (segmentCount + 1) * verticesPerSegment;
        Vector3[] vertices = new Vector3[vertexCount];

        // Generate Uvs array
        Vector2[] uvs = new Vector2[vertexCount];

        // Generate Triangle indices
        int[] triangleIndices = new int[6 * (verticesPerSegment - 1) * segmentCount];

        float vDistance = 0;

        //Position Vertices
        for (int i = 0; i < drawingPoints.Count; i++) {
            //Para adaptar el nodo actual a la altura del terreno.
            RaycastHit hitInfo;
            LayerMask layerMask = 1 << LayerMask.NameToLayer("Terrain");
            if (Physics.Raycast(drawingPoints[i] + Vector3.up * 10000, -Vector3.up, out hitInfo, 20000, layerMask)) {
                Vector3 hitPosition = hitInfo.point + Vector3.up * heightOffset;
                drawingPoints[i] = hitPosition;
            }
            Vector3 side = Vector3.zero;
            GetSideVectorAndVDistanceForSegment(i, drawingPoints, ref vDistance, ref side);
            FillSegmentVerticesAndUvs(ref vertices, ref uvs, verticesPerSegment, drawingPoints[i], i, vDistance, side);

            /*
            if (i == 0) {
                // First Segment use current and next to find direction
                Vector3 p0 = drawingPoints[i];
                Vector3 p1 = drawingPoints[i + 1];
                Vector3 dir = p1 - p0;
                Vector3 side = Vector3.Cross(dir, Vector3.up).normalized;

                //FillSegmentVerticesAndUvs(ref vertices, ref uvs, verticesPerSegment, drawingPoints[i], vDistance, side);

                vertices[i * verticesPerSegment + 0] = p0 + side * width * 0.5f;
                vertices[i * verticesPerSegment + 1] = p0 - side * width * 0.5f;
                vertices[i * verticesPerSegment + 2] = p0 + side * (width * 0.5f + exceededWidth);
                vertices[i * verticesPerSegment + 3] = p0 - side * (width * 0.5f + exceededWidth);

                //Altura carretera
                float maxRoadHeight = Mathf.Max(getRoadHeight(vertices[i * verticesPerSegment + 0]), getRoadHeight(vertices[i * verticesPerSegment + 1]));

                vertices[i * verticesPerSegment + 0] = new Vector3(vertices[i * verticesPerSegment + 0].x, maxRoadHeight, vertices[i * verticesPerSegment + 0].z);
                vertices[i * verticesPerSegment + 1] = new Vector3(vertices[i * verticesPerSegment + 1].x, maxRoadHeight, vertices[i * verticesPerSegment + 1].z);
                vertices[i * verticesPerSegment + 2] = new Vector3(vertices[i * verticesPerSegment + 2].x, getRoadHeight(vertices[i * verticesPerSegment + 2]), vertices[i * verticesPerSegment + 2].z);
                vertices[i * verticesPerSegment + 3] = new Vector3(vertices[i * verticesPerSegment + 3].x, getRoadHeight(vertices[i * verticesPerSegment + 3]), vertices[i * verticesPerSegment + 3].z);

                uvs[i * verticesPerSegment + 0] = new Vector2(0.1f, vDistance);
                uvs[i * verticesPerSegment + 1] = new Vector2(0.9f, vDistance);
                uvs[i * verticesPerSegment + 2] = new Vector2(0, vDistance);
                uvs[i * verticesPerSegment + 3] = new Vector2(1, vDistance);

                //MoveNodes pathNodes[segmentCount].Init(raycastCamera, maxRoadHeight, this);

            } else if (i == (drawingPoints.Count - 1)) {
                // Last Segment use previous and current to find direction
                Vector3 p0 = drawingPoints[i - 1];
                Vector3 p1 = drawingPoints[i];
                Vector3 dir = p1 - p0;
                Vector3 side = Vector3.Cross(dir, Vector3.up).normalized;

                vertices[i * verticesPerSegment + 0] = p1 + side * width * 0.5f;
                vertices[i * verticesPerSegment + 1] = p1 - side * width * 0.5f;
                vertices[i * verticesPerSegment + 2] = p1 + side * (width * 0.5f + exceededWidth);
                vertices[i * verticesPerSegment + 3] = p1 - side * (width * 0.5f + exceededWidth);

                vertices[i * verticesPerSegment + 0] = new Vector3(vertices[i * verticesPerSegment + 0].x, Mathf.Max(getRoadHeight(vertices[i * verticesPerSegment + 0]), getRoadHeight(vertices[i * verticesPerSegment + 1])), vertices[i * verticesPerSegment + 0].z);
                vertices[i * verticesPerSegment + 1] = new Vector3(vertices[i * verticesPerSegment + 1].x, Mathf.Max(getRoadHeight(vertices[i * verticesPerSegment + 0]), getRoadHeight(vertices[i * verticesPerSegment + 1])), vertices[i * verticesPerSegment + 1].z);
                vertices[i * verticesPerSegment + 2] = new Vector3(vertices[i * verticesPerSegment + 2].x, getRoadHeight(vertices[i * verticesPerSegment + 2]), vertices[i * verticesPerSegment + 2].z);
                vertices[i * verticesPerSegment + 3] = new Vector3(vertices[i * verticesPerSegment + 3].x, getRoadHeight(vertices[i * verticesPerSegment + 3]), vertices[i * verticesPerSegment + 3].z);

                vDistance += Vector3.Distance(p1, p0);
                uvs[i * verticesPerSegment + 0] = new Vector2(0.1f, vDistance);
                uvs[i * verticesPerSegment + 1] = new Vector2(0.9f, vDistance);
                uvs[i * verticesPerSegment + 2] = new Vector2(0, vDistance);
                uvs[i * verticesPerSegment + 3] = new Vector2(1, vDistance);
            } else {
                // Intermediate Segment use average vector 
                // Last Segment use previous and current to find direction
                Vector3 p0 = drawingPoints[i - 1];
                Vector3 p1 = drawingPoints[i];
                Vector3 p2 = drawingPoints[i + 1];
                Vector3 dir1 = p1 - p0;
                Vector3 dir2 = p2 - p1;
                Vector3 dir = (dir1 + dir2) * 0.5f;
                Vector3 side = Vector3.Cross(dir, Vector3.up).normalized;

                vertices[i * verticesPerSegment + 0] = p1 + side * width * 0.5f;
                vertices[i * verticesPerSegment + 1] = p1 - side * width * 0.5f;
                vertices[i * verticesPerSegment + 2] = p1 + side * (width * 0.5f + exceededWidth);
                vertices[i * verticesPerSegment + 3] = p1 - side * (width * 0.5f + exceededWidth);

                vertices[i * verticesPerSegment + 0] = new Vector3(vertices[i * verticesPerSegment + 0].x, Mathf.Max(getRoadHeight(vertices[i * verticesPerSegment + 0]), getRoadHeight(vertices[i * verticesPerSegment + 1])), vertices[i * verticesPerSegment + 0].z);
                vertices[i * verticesPerSegment + 1] = new Vector3(vertices[i * verticesPerSegment + 1].x, Mathf.Max(getRoadHeight(vertices[i * verticesPerSegment + 0]), getRoadHeight(vertices[i * verticesPerSegment + 1])), vertices[i * verticesPerSegment + 1].z);
                vertices[i * verticesPerSegment + 2] = new Vector3(vertices[i * verticesPerSegment + 2].x, getRoadHeight(vertices[i * verticesPerSegment + 2]), vertices[i * verticesPerSegment + 2].z);
                vertices[i * verticesPerSegment + 3] = new Vector3(vertices[i * verticesPerSegment + 3].x, getRoadHeight(vertices[i * verticesPerSegment + 3]), vertices[i * verticesPerSegment + 3].z);

                vDistance += Vector3.Distance(p1, p0);
                uvs[i * verticesPerSegment + 0] = new Vector2(0.1f, vDistance);
                uvs[i * verticesPerSegment + 1] = new Vector2(0.9f, vDistance);
                uvs[i * verticesPerSegment + 2] = new Vector2(0, vDistance);
                uvs[i * verticesPerSegment + 3] = new Vector2(1, vDistance);
            }*/
        }
        for (int i = 0; i < segmentCount; i++) {
            /*for (int j = 0; j < verticesPerSegment - 1; j++) {
                //Setting triangles
                triangleIndices[i * 6 * (verticesPerSegment - 1) + (j * 6 + 0)] = i * verticesPerSegment + 0 + j;
                triangleIndices[i * 6 * (verticesPerSegment - 1) + (j * 6 + 1)] = i * verticesPerSegment + 4 + j;
                triangleIndices[i * 6 * (verticesPerSegment - 1) + (j * 6 + 2)] = i * verticesPerSegment + 1 + j;

                triangleIndices[i * 6 * (verticesPerSegment - 1) + (j * 6 + 3)] = i * verticesPerSegment + 1 + j;
                triangleIndices[i * 6 * (verticesPerSegment - 1) + (j * 6 + 4)] = i * verticesPerSegment + 4 + j;
                triangleIndices[i * 6 * (verticesPerSegment - 1) + (j * 6 + 5)] = i * verticesPerSegment + 5 + j;
            }*/

            //Setting triangles
            triangleIndices[i * 6 * (verticesPerSegment - 1) + 00] = i * verticesPerSegment + 0;
            triangleIndices[i * 6 * (verticesPerSegment - 1) + 01] = i * verticesPerSegment + 4;
            triangleIndices[i * 6 * (verticesPerSegment - 1) + 02] = i * verticesPerSegment + 1;

            triangleIndices[i * 6 * (verticesPerSegment - 1) + 03] = i * verticesPerSegment + 1;
            triangleIndices[i * 6 * (verticesPerSegment - 1) + 04] = i * verticesPerSegment + 4;
            triangleIndices[i * 6 * (verticesPerSegment - 1) + 05] = i * verticesPerSegment + 5;

            triangleIndices[i * 6 * (verticesPerSegment - 1) + 06] = i * verticesPerSegment + 2;
            triangleIndices[i * 6 * (verticesPerSegment - 1) + 07] = i * verticesPerSegment + 6;
            triangleIndices[i * 6 * (verticesPerSegment - 1) + 08] = i * verticesPerSegment + 0;

            triangleIndices[i * 6 * (verticesPerSegment - 1) + 09] = i * verticesPerSegment + 0;
            triangleIndices[i * 6 * (verticesPerSegment - 1) + 10] = i * verticesPerSegment + 6;
            triangleIndices[i * 6 * (verticesPerSegment - 1) + 11] = i * verticesPerSegment + 4;

            triangleIndices[i * 6 * (verticesPerSegment - 1) + 12] = i * verticesPerSegment + 1;
            triangleIndices[i * 6 * (verticesPerSegment - 1) + 13] = i * verticesPerSegment + 5;
            triangleIndices[i * 6 * (verticesPerSegment - 1) + 14] = i * verticesPerSegment + 3;

            triangleIndices[i * 6 * (verticesPerSegment - 1) + 15] = i * verticesPerSegment + 3;
            triangleIndices[i * 6 * (verticesPerSegment - 1) + 16] = i * verticesPerSegment + 5;
            triangleIndices[i * 6 * (verticesPerSegment - 1) + 17] = i * verticesPerSegment + 7;
        }

        // Set vertices on mesh
        mesh.vertices = vertices;

        // Set uvs on mesh
        mesh.uv = uvs;

        // Set triangle indices on mesh
        mesh.triangles = triangleIndices;

        mesh.RecalculateNormals();

        // Set mesh on MeshFilter Component
        mf.mesh = mesh;
    }

    public float getRoadHeight(Vector3 vertice) {
        Vector3 hitPosition = new Vector3();
        RaycastHit hitInfo;
        LayerMask layerMask = 1 << LayerMask.NameToLayer("Terrain");
        if (Physics.Raycast(vertice + Vector3.up * 10000, -Vector3.up, out hitInfo, 20000, layerMask)) {
            hitPosition = hitInfo.point + Vector3.up * heightOffset;
        }

        return hitPosition.y;
    }

    public void CreatePathNodeAt(int index, Vector3 position) {
        GameObject go = Instantiate(nodePrefab, position, Quaternion.identity) as GameObject;
        MoveNodes pathNode = go.AddComponent<MoveNodes>();
        pathNode.Init(raycastCamera, heightOffset, this);
        pathNodes.Insert(index, go.transform);
        RegenerateAuxNodes();
    }

    public void RegenerateAuxNodes() {
        int segmentCount = pathNodes.Count - 1;
        if (auxNodes.Count > 0) {
            foreach (Transform t in auxNodes) {
                GameObject.DestroyImmediate(t.gameObject);
            }
        }
        //Borrar lista nodos auxiliares
        auxNodes.Clear();
        // At least one segment is needed to create auxiliar nodes
        if (segmentCount >= 1) {
            for (int i = 0; i < segmentCount; i++) {
                Vector3 p0 = pathNodes[i].position;
                Vector3 p1 = pathNodes[i + 1].position;
                Vector3 halfPosition = (p0 + p1) * 0.5f;

                RaycastHit hitInfo;
                LayerMask layerMask = 1 << LayerMask.NameToLayer("Terrain");
                if (Physics.Raycast(halfPosition + Vector3.up * 10000, -Vector3.up, out hitInfo, 20000, layerMask)) {
                    Vector3 hitPosition = hitInfo.point + Vector3.up * heightOffset;
                    halfPosition = hitPosition;
                }

                GameObject goAux = GameObject.Instantiate(auxNodePrefab, halfPosition, Quaternion.identity) as GameObject;
                auxNodes.Add(goAux.transform);
                NodoAuxiliar nodoAuxiliar = goAux.AddComponent<NodoAuxiliar>();
                nodoAuxiliar.Init(i + 1, this);
            }
        }
        RegenerateMesh();
    }

    private void GetSideVectorAndVDistanceForSegment(int index, List<Vector3> drawingPoints, ref float vDistance, ref Vector3 side) {
        if (index == 0) {
            // First Segment use current and next to find direction
            Vector3 p0 = drawingPoints[index];
            Vector3 p1 = drawingPoints[index + 1];
            Vector3 dir = p1 - p0;
            side = Vector3.Cross(dir, Vector3.up).normalized;
            vDistance = 0;
        } else if (index == (drawingPoints.Count - 1)) {
            // Last Segment use previous and current to find direction
            Vector3 p0 = drawingPoints[index - 1];
            Vector3 p1 = drawingPoints[index];
            Vector3 dir = p1 - p0;
            side = Vector3.Cross(dir, Vector3.up).normalized;
            vDistance += Vector3.Distance(p1, p0);
        } else {
            // Intermediate Segment use average vector 
            // Last Segment use previous and current to find direction
            Vector3 p0 = drawingPoints[index - 1];
            Vector3 p1 = drawingPoints[index];
            Vector3 p2 = drawingPoints[index + 1];

            Vector3 dir1 = p1 - p0;
            Vector3 dir2 = p2 - p1;
            Vector3 dir = (dir1 + dir2) * 0.5f;
            side = Vector3.Cross(dir, Vector3.up).normalized;
            vDistance += Vector3.Distance(p1, p0);
        }
    }

    private void FillSegmentVerticesAndUvs(ref Vector3[] vertices, ref Vector2[] uvs, int verticesPerSegment, Vector3 drawingPoint, int index, float vDistance, Vector3 side) {

        Vector3 v0Tentative = drawingPoint + side * width * 0.5f;
        Vector3 v1Tentative = drawingPoint - side * width * 0.5f;
        Vector3 v2Tentative = drawingPoint + side * (width * 0.5f + exceededWidth);
        Vector3 v3Tentative = drawingPoint - side * (width * 0.5f + exceededWidth);

        RaycastHit hitInfo;
        LayerMask layerMask = 1 << LayerMask.NameToLayer("Terrain");
        if (Physics.Raycast(v0Tentative + Vector3.up * 10000, -Vector3.up, out hitInfo, 20000, layerMask)) {
            v0Tentative = hitInfo.point + Vector3.up * heightOffset;
        }

        if (Physics.Raycast(v1Tentative + Vector3.up * 10000, -Vector3.up, out hitInfo, 20000, layerMask)) {
            v1Tentative = hitInfo.point + Vector3.up * heightOffset;
        }

        if (Physics.Raycast(v2Tentative + Vector3.up * 10000, -Vector3.up, out hitInfo, 20000, layerMask)) {
            v2Tentative = hitInfo.point;
        }

        if (Physics.Raycast(v3Tentative + Vector3.up * 10000, -Vector3.up, out hitInfo, 20000, layerMask)) {
            v3Tentative = hitInfo.point;
        }

        float maxY = Mathf.Max(v0Tentative.y, v1Tentative.y);

        v0Tentative = new Vector3(v0Tentative.x, maxY, v0Tentative.z);
        v1Tentative = new Vector3(v1Tentative.x, maxY, v1Tentative.z);

        vertices[index * verticesPerSegment + 0] = v0Tentative;
        vertices[index * verticesPerSegment + 1] = v1Tentative;
        vertices[index * verticesPerSegment + 2] = v2Tentative;
        vertices[index * verticesPerSegment + 3] = v3Tentative;

        uvs[index * verticesPerSegment + 0] = new Vector2(0.1f, vDistance);
        uvs[index * verticesPerSegment + 1] = new Vector2(0.9f, vDistance);
        uvs[index * verticesPerSegment + 2] = new Vector2(0, vDistance);
        uvs[index * verticesPerSegment + 3] = new Vector2(1, vDistance);
    }
}
