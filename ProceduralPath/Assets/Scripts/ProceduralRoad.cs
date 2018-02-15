using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ProceduralRoad : MonoBehaviour {
    public GameObject nodePrefab;
    public List<Transform> pathNodes;
    public List<Transform> segments;
    public Camera raycastCamera;
    public float width = 5;
    public float heightOffset = 0.5f;
    public float repeatFactor = 1;
    public Material material;

    void Start() {
        pathNodes = new List<Transform>();
        segments = new List<Transform>();
    }

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
                    GenerateLastSegment();
                    PostProcess();
                }  
            }
        }
    }

    void GenerateLastSegment() {
        Vector3 p0 = pathNodes[pathNodes.Count - 2].position;
        Vector3 p1 = pathNodes[pathNodes.Count - 1].position;

        //Sacar el vector de direccion entre el punto 1 y 0
        Vector3 dir = p1 - p0;

        //Calculamos el vector lateral
        Vector3 side = Vector3.Cross(dir, Vector3.up).normalized;

        Vector3 v0 = p0 + side * width * 0.5f;
        Vector3 v1 = p0 - side * width * 0.5f;
        Vector3 v2 = p1 + side * width * 0.5f;
        Vector3 v3 = p1 - side * width * 0.5f;

        GameObject segmentGO = new GameObject();
        segments.Add(segmentGO.transform);

        MeshFilter mf = segmentGO.AddComponent<MeshFilter>();
        MeshRenderer mr = segmentGO.AddComponent<MeshRenderer>();
        mr.material = material;

        Mesh mesh = new Mesh();

        // Generate Vertices array
        Vector3[] vertices = new Vector3[4];

        // Set vertices positions
        vertices[0] = v0;
        vertices[1] = v1;
        vertices[2] = v2;
        vertices[3] = v3;

        // Generate Uvs array
        Vector2[] uvs = new Vector2[4];

        float _v2 = Vector3.Distance(v2, v0) * repeatFactor;
        float _v3 = Vector3.Distance(v3, v1) * repeatFactor;

        // Set uvs
        uvs[0] = new Vector2(0, 0);
        uvs[1] = new Vector2(1, 0);
        uvs[2] = new Vector2(0, _v2);
        uvs[3] = new Vector2(1, _v3);

        // Generate Triangle indices
        int[] triangleIndices = new int[6];
        triangleIndices[0] = 0;
        triangleIndices[1] = 2;
        triangleIndices[2] = 1;

        triangleIndices[3] = 1;
        triangleIndices[4] = 2;
        triangleIndices[5] = 3;

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

    void PostProcess() {
        if (segments.Count > 1) {
            for (int i = 0; i < segments.Count - 1; i++) {
                Transform s0 = segments[i];
                Transform s1 = segments[i + 1];

                // Hacer la media de primeros vertices de s1 a los últimos de s0 e igualarlos
                Mesh MeshS0 = s0.GetComponent<MeshFilter>().mesh;
                Mesh MeshS1 = s1.GetComponent<MeshFilter>().mesh;

                Vector3[] s0Vertices = MeshS0.vertices;
                Vector3[] s1Vertices = MeshS1.vertices;

                Vector3 new0 = (s0Vertices[2] + s1Vertices[0]) / 2;
                Vector3 new1 = (s0Vertices[3] + s1Vertices[1]) / 2;

                s0Vertices[2] = new0;
                s1Vertices[0] = new0;

                s0Vertices[3] = new1;
                s1Vertices[1] = new1;

                MeshS0.vertices = s0Vertices;
                MeshS1.vertices = s1Vertices;
            }
        }
    }
}
