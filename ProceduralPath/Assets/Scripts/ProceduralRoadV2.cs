using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ProceduralRoadV2 : MonoBehaviour {
    public GameObject nodePrefab;
    public List<Transform> pathNodes;
    public Camera raycastCamera;
    public float width = 5;
    public float heightOffset = 0.5f;
    public float repeatFactor = 1;
    public float nodeMaxDistance = 5f;
    public Material material;

    private Mesh mesh;
    private MeshFilter mf;
    private MeshRenderer mr;

    // Use this for initialization
    void Start () {
        pathNodes = new List<Transform>();
        mf = gameObject.AddComponent<MeshFilter>();
        mr = gameObject.AddComponent<MeshRenderer>();
        // Create the mesh
        mesh = new Mesh();
        // Set mesh on MeshFilter Component
        mf.mesh = mesh;
        //Set material on MeshRenderer
        mr.material = material;
    }
	
	// Update is called once per frame
	void Update () {
        if (Input.GetMouseButtonUp(0)) {
            Ray myRay = raycastCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;
            if (Physics.Raycast(myRay, out hitInfo)) {
                Vector3 hitPosition = hitInfo.point + Vector3.up * heightOffset;
                GameObject go = Instantiate(nodePrefab, hitPosition, Quaternion.identity) as GameObject;
                pathNodes.Add(go.transform);
                Debug.DrawLine(myRay.origin, hitPosition);
                if (pathNodes.Count > 1) {
                    SubdivideNodes();
                    RegenerateMesh();
                }
            }
        }
    }
    
    void SubdivideNodes() {
        bool subdivide = true;
        while (subdivide) {
            subdivide = false;
            for (int i = 0; i < pathNodes.Count - 1; i++) {
                subdivide = false;
                Vector3 p0 = pathNodes[i].position;
                Vector3 p1 = pathNodes[i + 1].position;

                //Poner la altura a 0 en los 2 puntos para calcular la distancia entre nodos
                p0.y = 0;
                p1.y = 0;

                //Si la distancia es mayor que la distancia entre nodos indicada
                if (Vector3.Distance(p0, p1) > nodeMaxDistance) {
                    // insert node at average
                    Vector3 newNodeTentative = (p1 + p0) * 0.5f;
                    RaycastHit hitInfo;

                    //Para adaptar el nodo al terreno...
                    //Se tira un rayo desde mucha altura y a mucha profundidad hasta que choque con el layer "Terrain"
                    if (Physics.Raycast(newNodeTentative + Vector3.up * 10000, -Vector3.up, out hitInfo, 20000)) {
                        Vector3 hitPosition = hitInfo.point + Vector3.up * heightOffset;
                        newNodeTentative = hitPosition;
                    }

                    GameObject go = Instantiate(nodePrefab, newNodeTentative, Quaternion.identity) as GameObject;

                    //Inserta el nodo en la mitad de 2 nodos
                    pathNodes.Insert(i + 1, go.transform);
                    subdivide = true;
                    break;
                }
            }
        }
    }
    void RegenerateMesh() {
        //Segment Count
        int segmentCount = pathNodes.Count - 1;

        // Generate Vertices array
        // Two vertices for each segment + 2 start vertices
        int vertexCount = segmentCount * 2 + 2;
        Vector3[] vertices = new Vector3[vertexCount];

        // Generate Triangle indices
        int[] triangleIndices = new int[6 * segmentCount];

        // Generate Uvs array
        Vector2[] uvs = new Vector2[vertexCount];

        //Position Vertices
        for (int i = 0; i < pathNodes.Count; i++) {
            if (i == 0) {
                // First Segment use current and next to find direction
                Vector3 p0 = pathNodes[i].position;
                Vector3 p1 = pathNodes[i + 1].position;
                Vector3 dir = p1 - p0;
                Vector3 side = Vector3.Cross(dir, Vector3.up).normalized;

                vertices[i * 2 + 0] = p0 + side * width * 0.5f;
                vertices[i * 2 + 1] = p0 - side * width * 0.5f;

                uvs[i] = new Vector2(0, i);
                uvs[i + 1] = new Vector2(1, i);

            } else if (i == (pathNodes.Count - 1)) {
                // Last Segment use previous and current to find direction
                Vector3 p0 = pathNodes[i - 1].position;
                Vector3 p1 = pathNodes[i].position;
                Vector3 dir = p1 - p0;
                Vector3 side = Vector3.Cross(dir, Vector3.up).normalized;

                vertices[i * 2 + 0] = p1 + side * width * 0.5f;
                vertices[i * 2 + 1] = p1 - side * width * 0.5f;

                uvs[i * 2 + 0] = new Vector2(0, i);
                uvs[i * 2 + 1] = new Vector2(1, i);

            } else {
                // Intermediate Segment use average vector 
                // Last Segment use previous and current to find direction
                Vector3 p0 = pathNodes[i - 1].position;
                Vector3 p1 = pathNodes[i].position;
                Vector3 p2 = pathNodes[i + 1].position;
                Vector3 dir1 = p1 - p0;
                Vector3 dir2 = p2 - p1;
                Vector3 dir = (dir1 + dir2) * 0.5f;
                Vector3 side = Vector3.Cross(dir, Vector3.up).normalized;

                vertices[i * 2 + 0] = p1 + side * width * 0.5f;
                vertices[i * 2 + 1] = p1 - side * width * 0.5f;

                uvs[i * 2 + 0] = new Vector2(0, i);
                uvs[i * 2 + 1] = new Vector2(1, i);
            }
        }
        for (int i = 0; i < segmentCount; i++) {
            //Setting triangles
            triangleIndices[i * 6 + 0] = i * 2 + 0;
            triangleIndices[i * 6 + 1] = i * 2 + 2;
            triangleIndices[i * 6 + 2] = i * 2 + 1;

            triangleIndices[i * 6 + 3] = i * 2 + 1;
            triangleIndices[i * 6 + 4] = i * 2 + 2;
            triangleIndices[i * 6 + 5] = i * 2 + 3;
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
}
