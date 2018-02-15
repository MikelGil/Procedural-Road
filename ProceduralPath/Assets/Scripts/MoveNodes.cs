using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MoveNodes : MonoBehaviour {

    Camera raycastCamera;
    float heightOffset;
    ProceduralRoadV4 procRoad;

    public void Init(Camera raycastCamera, float heightOffset, ProceduralRoadV4 procRoad) {
        this.raycastCamera = raycastCamera;
        this.heightOffset = heightOffset;
        this.procRoad = procRoad;
    }

    void OnMouseDrag() {
        Ray myRay = raycastCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        LayerMask layerMask = 1 << LayerMask.NameToLayer("Terrain");
        if (Physics.Raycast(myRay, out hitInfo, 10000, layerMask)) {
            Vector3 hitPosition = hitInfo.point + Vector3.up * heightOffset;
            transform.position = hitPosition;
            procRoad.RegenerateAuxNodes();
        }
    }

    public void OnMouseOver() {
        if (Input.GetKeyDown(KeyCode.Delete)) {
            Ray myRay = raycastCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;
            if (Physics.Raycast(myRay, out hitInfo, 10000)) {
                Destroy(transform.gameObject);
                procRoad.pathNodes.Remove(transform);
                procRoad.RegenerateAuxNodes();
            }
        }
    }
}
