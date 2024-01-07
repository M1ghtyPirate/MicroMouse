using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SensorController : MonoBehaviour
{

    #region Fields

    private BoxCollider SensorTrigger;

    public bool ObstacleDetected { get => CurrentCollisions.Count > 0; }

    private List<Collider> CurrentCollisions = new List<Collider>();

    #endregion

    #region UpdateMethods

    private void OnEnable() {
        SensorTrigger = GetComponent<BoxCollider>();
    }

    private void OnTriggerEnter(Collider other) {
        if (!CurrentCollisions.Contains(other)) {
            CurrentCollisions.Add(other);
            //Debug.Log($"{other.gameObject.name} entered {this.gameObject.name} sensor");
        }
    }

    private void OnTriggerExit(Collider other) {
        if (CurrentCollisions.Contains(other)) {
            CurrentCollisions.Remove(other);
            //Debug.Log($"{other.gameObject.name} exited {this.gameObject.name} sensor");
        }
    }

    #endregion
}
