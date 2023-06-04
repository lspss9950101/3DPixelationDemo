using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayLight : MonoBehaviour {
    void Start() {
        transform.rotation = Quaternion.identity;
    }

    void Update() {
        transform.RotateAround(Vector3.zero, new Vector3(2, 1, -1), 5 * Time.deltaTime);
    }
}
