using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelController : MonoBehaviour
{

    #region Fields

    [SerializeField]
    private float BaseAcceleration = 25f;
    [SerializeField]
    private float BaseBreakForce = 15f;
    [SerializeField]
    private Transform WheelMesh;
    
    public float AccelerationMultiplier = 0f;
    public float BrakeMultiplier = 0f;
    public bool IsTravellingDistance { get => DistanceToTravel > 1e-4; }

    private WheelCollider WheelColl;
    private float TravelledDistance = 0f;
    private float DistanceToTravel = 0f;

    private Vector3 StartingPosition;

    #endregion

    #region UpdateMethods

    private void OnEnable() {
        WheelColl = GetComponent<WheelCollider>();
    }

    private void FixedUpdate() {
        if (IsTravellingDistance) {
            RecalculateDistanceToTravel();
            if (!IsTravellingDistance) {
                AccelerationMultiplier = 0f;
                BrakeMultiplier = 1f;
                DistanceToTravel = 0f;
            }
        }

        WheelColl.brakeTorque = BaseBreakForce * BrakeMultiplier;
        WheelColl.motorTorque = BaseAcceleration * AccelerationMultiplier;

        PositionMeshes();

        CalculateTravelledDistance();
        if (WheelColl.rpm == 0 && TravelledDistance != 0) {
            //Debug.Log($"Distance travelled: {TravelledDistance}");
            //TravelledDistance = 0;
        }
    }

    #endregion

    #region PublicMethods

    public void TravelDistance(float distance) {
        var distanceDir = distance.CompareTo(0);
        DistanceToTravel = Mathf.Abs(distance);
        AccelerationMultiplier = distanceDir * 0.1f;
        BrakeMultiplier = 0f;

        //Simplification
        WheelColl.GetWorldPose(out var position, out var rotation);
        StartingPosition = position;
    }

    #endregion

    #region PrivateMethods

    private void CalculateTravelledDistance() {
        float angularVelocity = Mathf.Abs(WheelColl.rpm) * (2.0f * Mathf.PI) / 60.0f;
        TravelledDistance += angularVelocity * WheelColl.radius * Time.deltaTime;
    }

    private void RecalculateDistanceToTravel() {
        if(DistanceToTravel == 0f) {
            return;
        }

        //float angularVelocity = Mathf.Abs(WheelColl.rpm) * (2.0f * Mathf.PI) / 60.0f;
        //DistanceToTravel -= angularVelocity * WheelColl.radius * Time.deltaTime;

        //Simplification
        WheelColl.GetWorldPose(out var position, out var rotation);
        if (DistanceToTravel - (StartingPosition - position).magnitude < 1e-4) {
            Debug.Log($"Travelled distance: {(StartingPosition - position).magnitude}");
            DistanceToTravel = 0f;
        }
    }

    private void PositionMeshes() {
        WheelColl.GetWorldPose(out var wheelPosition, out var wheelRotation);
        WheelMesh.rotation = wheelRotation;
        WheelMesh.position = wheelPosition;
    }

    #endregion

}
