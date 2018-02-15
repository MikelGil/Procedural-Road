using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NodoAuxiliar : MonoBehaviour {

    private int posicion;
    private ProceduralRoadV4 proceduralRoadV4;

    public void Init(int posicion, ProceduralRoadV4 proceduralRoadV4) {
        this.posicion = posicion;
        this.proceduralRoadV4 = proceduralRoadV4;
    }
    void OnMouseUp() {
        proceduralRoadV4.CreatePathNodeAt(posicion, transform.position);
    } 
}