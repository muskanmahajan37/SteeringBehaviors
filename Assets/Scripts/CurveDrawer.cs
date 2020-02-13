using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurveDrawer : MonoBehaviour {

    private LineRenderer lr;

    // Start is called before the first frame update
    void Start() {
        this.lr = this.GetComponent<LineRenderer>();

    }


    void Update() {
        //drawCurve(30, 2 * Mathf.PI, 0, 0.5f);
    }

    public void drawCurve(int numEdges, float startAngle, float endAngle, float radius) {
        // 30 edges is good for small full circle curves
        // 50 edges is good for larger full circles

        lr.positionCount = numEdges + 1;
        float angleSlice = (endAngle - startAngle) / numEdges;

        for (int i = 0; i < numEdges + 1; i++) {
            Vector3 pt = this.transform.position;

            float ptAngle = startAngle + (angleSlice * i);
            pt.x += Mathf.Cos(ptAngle) * radius;
            pt.y += Mathf.Sin(ptAngle) * radius;

            lr.SetPosition(i, pt);
        }
    }


}
