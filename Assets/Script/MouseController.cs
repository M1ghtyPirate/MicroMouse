using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;

public class MouseController : MonoBehaviour {

    #region Fields

    [SerializeField]
    WheelController WheelRight;
    [SerializeField]
    WheelController WheelLeft;

    [SerializeField]
    SensorController SensorCenter;
    [SerializeField]
    SensorController SensorRight;
    [SerializeField]
    SensorController SensorLeft;
    [SerializeField]
    public bool UseManualControl;
    private bool _ShowPathMarkers;
    [SerializeField]
    public bool ShowPathMarkers { get => _ShowPathMarkers; set { PathMarkersObject?.SetActive(value); _ShowPathMarkers = value; } }
    [SerializeField]
    public bool IsActive;

    private float AccelerationMultiplier = 0f;
    private float TurnMultiplier = 0f;
    private float BreakMultiplier = 0f;

    private GameObject PathMarkersObject;
    private GameObject[,] PathMarkers;

    private bool IsTurning;
    private float TargetRotation = 0f;
    private float RotationDifference { get => TargetRotation - gameObject.transform.rotation.eulerAngles.y; }

    private bool IsTravelling;
    private Vector3 TargetPosition;
    private float PositionDifference {
        get {
            var positionDifference = 0f;
            switch (CurrentDirection) {
                case Enums.Direction.Forward:
                    positionDifference = TargetPosition.z - gameObject.transform.position.z;
                    break;
                case Enums.Direction.Right:
                    positionDifference = TargetPosition.x - gameObject.transform.position.x;
                    break;
                case Enums.Direction.Backward:
                    positionDifference = -(TargetPosition.z - gameObject.transform.position.z);
                    break;
                case Enums.Direction.Left:
                    positionDifference = -(TargetPosition.x - gameObject.transform.position.x);
                    break;
            }
            return positionDifference;
        }
    }

    #region MazeMapping

    private int MazeRows = 16;
    private int MazeColumns = 16;
    private Enums.Direction[,] MazePaths;
    private int[,] MazeWalls;
    private Point CurrentCell;
    private Enums.Direction CurrentDirection = Enums.Direction.Forward;
    private Point StartingCell = new Point(0, 0);
    private Point CenterCell = new Point(7, 7);
    private Point TargetCell;

    #endregion

    #endregion

    #region UpdateMethods

    private void OnEnable() {
        TargetPosition = gameObject.transform.position;
        CurrentCell = new Point(StartingCell.X, StartingCell.Y);
        TargetCell = new Point(CenterCell.X, CenterCell.Y);
        //ShowPathMarkers = true;

        InitializePathMarkers();
        InitializeMazeWalls();
        InitializeMazePaths(TargetCell);
    }

    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }

    private void FixedUpdate() {
        if(!IsActive) {
            return;
        }

        if (IsTurning) {
            Turn();
            return;
        }

        if (IsTravelling) {
            Travel();
            return;
        }

        if(UseManualControl) {
            ManualControl();
        } else {
            AutoControl();
        }
    }

    #endregion

    #region PrivateMethods

    private void AutoControl() {
        UpdateCellWalls();

        if (((int)MazePaths[CurrentCell.X, CurrentCell.Y] & MazeWalls[CurrentCell.X, CurrentCell.Y]) > 0) {
            InitializeMazePaths(TargetCell);
        }

        var nextMove = MazePaths[CurrentCell.X, CurrentCell.Y];

        if (nextMove == 0) {
            if(TargetCell.X != CurrentCell.X && TargetCell.Y == CurrentCell.Y) {
                TurnToRotation(TargetRotation + 90);
                Debug.Log($"No path found.");
                return;
            }
            TargetCell = TargetCell.X == CenterCell.X && TargetCell.Y == CenterCell.Y ?
                new Point(StartingCell.X, StartingCell.Y) :
                new Point(CenterCell.X, CenterCell.Y);
            InitializeMazePaths(TargetCell);
            Debug.Log($"Target reached, heading towards: [{TargetCell.X}, {TargetCell.Y}]");
            return;
        }

        if (nextMove != CurrentDirection) {
            float degrees;
            if ((((int)nextMove >> 2 | (int)nextMove << 2) & (int)CurrentDirection) > 0) {
                degrees = 180;
            } else if ((((int)nextMove >> 1 | (int)nextMove << 3) & (int)CurrentDirection) > 0) {
                degrees = 90;
            } else {
                degrees = -90;
            }
            TurnToRotation(TargetRotation + degrees);
            CurrentDirection = nextMove;
            return;
        }

        TravelDistance(0.18f);
        switch (CurrentDirection) {
            case Enums.Direction.Forward:
                CurrentCell.Y++;
                break;
            case Enums.Direction.Right:
                CurrentCell.X++;
                break;
            case Enums.Direction.Backward:
                CurrentCell.Y--;
                break;
            case Enums.Direction.Left:
                CurrentCell.X--;
                break;
        }

    }

    private void ManualControl() {

        if (Input.GetKey(KeyCode.F)) {
            TargetPosition = gameObject.transform.position;
            RecalculateCurrentDirection();
            Debug.Log($"CurrendDir: {CurrentDirection}");
            TravelDistance(0.18f);
            return;
        }

        if (Input.GetKey(KeyCode.B)) {
            TargetPosition = gameObject.transform.position;
            RecalculateCurrentDirection();
            Debug.Log($"CurrendDir: {CurrentDirection}");
            TravelDistance(-0.18f);
            return;
        }

        if (Input.GetKey(KeyCode.L)) {
            RecalculateCurrentDirection();
            TurnToRotation(TargetRotation - 90f);
            CurrentDirection = (Enums.Direction)(0xF & ((int)CurrentDirection >> 1 | (int)CurrentDirection << 3));
            return;
        }

        if (Input.GetKey(KeyCode.R)) {
            RecalculateCurrentDirection();
            TurnToRotation(TargetRotation + 90f);
            CurrentDirection = (Enums.Direction)(0xF & ((int)CurrentDirection << 1 | (int)CurrentDirection >> 3));
            return;
        }

        BreakMultiplier = Input.GetKey(KeyCode.Space) ? 1 : 0;
        WheelRight.BrakeMultiplier = BreakMultiplier;
        WheelLeft.BrakeMultiplier = BreakMultiplier;

        AccelerationMultiplier = Input.GetAxis("Vertical");
        WheelRight.AccelerationMultiplier = AccelerationMultiplier;
        WheelLeft.AccelerationMultiplier = AccelerationMultiplier;

        TurnMultiplier = Input.GetAxis("Horizontal");
        WheelRight.AccelerationMultiplier -= TurnMultiplier / 2;
        WheelLeft.AccelerationMultiplier += TurnMultiplier / 2;
    }

    private void RecalculateCurrentDirection() {
        Debug.Log($"CurrentRotation:{gameObject.transform.rotation.eulerAngles.y};OldRotation:{TargetRotation};");
        TargetRotation = ((int)gameObject.transform.rotation.eulerAngles.y / 90) * 90 
            + (((int)gameObject.transform.rotation.eulerAngles.y % 90) > 45 ? 90 : 0);
        Debug.Log($"NewRotation:{TargetRotation};Floor:{((int)gameObject.transform.rotation.eulerAngles.y / 90) * 90};Residue:{((int)gameObject.transform.rotation.eulerAngles.y % 90)}");
        switch (TargetRotation) {
            case 0f:
            case 360f:
                CurrentDirection = Enums.Direction.Forward;
                break;
            case 90f:
                CurrentDirection = Enums.Direction.Right;
                break;
            case 180f:
                CurrentDirection = Enums.Direction.Backward;
                break;
            case 270f:
                CurrentDirection = Enums.Direction.Left;
                break;
        }
    }

    private void TurnToRotation(float degrees) {
        TargetRotation = Mathf.Repeat(degrees, 360f);
        IsTurning = true;
    }

    private void Turn() {
        var turnDir = Mathf.Abs(RotationDifference) < 180f ? RotationDifference.CompareTo(0) : -RotationDifference.CompareTo(0);
        if (Mathf.Abs(RotationDifference) < 5e-1 
            || WheelLeft.AccelerationMultiplier != 0f && turnDir != WheelLeft.AccelerationMultiplier.CompareTo(0)) {
            IsTurning = false;
            Stop();
            return;
        }

        //Debug.Log($"Current rotation: {gameObject.transform.rotation.eulerAngles.y}, Target rotation: {TargetRotation}, Rotation difference {RotationDifference}");
        var turnMultiplier = Mathf.Abs(RotationDifference) > 1 ? 0.05f : 0.01f;
        WheelRight.AccelerationMultiplier = -turnDir * turnMultiplier;
        WheelRight.BrakeMultiplier = 0f;
        WheelLeft.AccelerationMultiplier = turnDir * turnMultiplier;
        WheelLeft.BrakeMultiplier = 0f;
    }

    private void TravelDistance(float distance) {
        switch(CurrentDirection) {
            case Enums.Direction.Forward:
                TargetPosition.z += distance;
                break;
            case Enums.Direction.Right:
                TargetPosition.x += distance;
                break;
            case Enums.Direction.Backward:
                TargetPosition.z -= distance;
                break;
            case Enums.Direction.Left:
                TargetPosition.x -= distance;
                break;
        }
        IsTravelling = true;
    }

    private void Travel() {
        var travelDir = PositionDifference.CompareTo(0);
        if (Math.Abs(PositionDifference) < 5e-3 
            || WheelRight.AccelerationMultiplier != 0f && travelDir != WheelRight.AccelerationMultiplier.CompareTo(0)) {
            IsTravelling = false;
            Stop();
            TurnToRotation(TargetRotation);
            return;
        }

        //Debug.Log($"Current position: {gameObject.transform.position}, Target position: {TargetPosition}, Position difference {PositionDifference}");
        var TravelMultiplier = Mathf.Abs(PositionDifference) > 5e-2 ? 0.4f : 0.05f;
        WheelRight.AccelerationMultiplier = travelDir * TravelMultiplier;
        WheelRight.BrakeMultiplier = 0f;
        WheelLeft.AccelerationMultiplier = travelDir * TravelMultiplier;
        WheelLeft.BrakeMultiplier = 0f;
    }

    private void Stop() {
        WheelRight.AccelerationMultiplier = 0f;
        WheelRight.BrakeMultiplier = 1f;
        WheelLeft.AccelerationMultiplier = 0f;
        WheelLeft.BrakeMultiplier = 1f;
    }

    #region MazeMappingMethods

    private void InitializeMazePaths(Point targetCell) {
        MazePaths = new Enums.Direction[MazeColumns, MazeRows];
        MapPathToCell(targetCell);
        //for (var i = 0; i < MazeRows; i++) {
        //    for(var j = 0; j < MazeColumns; j++) {
        //        Debug.Log($"[{i}, {j}]: {MazePaths[i, j] + ""}");
        //    }
        //}
    }

    private void MapPathToCell(Point targetCell) {
        if (!IsInbound(targetCell)) {
            Debug.LogError($"Mapping path to outbound cell: [{targetCell.X}, {targetCell.Y}]");
            return;
        }

        SetTravelDirection(targetCell, Enums.Direction.Forward);

        var cells = new Queue<Point>();
        cells.Enqueue(targetCell);

        while(cells.Count > 0) {
            var cell = cells.Dequeue();

            if (IsReachable(cell, Enums.Direction.Left) && MazePaths[cell.X - 1, cell.Y] == Enums.Direction.None) {
                SetTravelDirection(new Point(cell.X - 1, cell.Y), Enums.Direction.Right);
                cells.Enqueue(new Point(cell.X - 1, cell.Y));
            }
            if (IsReachable(cell, Enums.Direction.Right) && MazePaths[cell.X + 1, cell.Y] == Enums.Direction.None) {
                SetTravelDirection(new Point(cell.X + 1, cell.Y), Enums.Direction.Left);
                cells.Enqueue(new Point(cell.X + 1, cell.Y));
            }
            if (IsReachable(cell, Enums.Direction.Backward) && MazePaths[cell.X, cell.Y - 1] == Enums.Direction.None) {
                SetTravelDirection(new Point(cell.X, cell.Y - 1), Enums.Direction.Forward);
                cells.Enqueue(new Point(cell.X, cell.Y - 1));
            }
            if (IsReachable(cell, Enums.Direction.Forward) && MazePaths[cell.X, cell.Y + 1] == Enums.Direction.None) {
                SetTravelDirection(new Point(cell.X, cell.Y + 1), Enums.Direction.Backward);
                cells.Enqueue(new Point(cell.X, cell.Y + 1));
            }
        }

        SetTravelDirection(targetCell, Enums.Direction.None);
        DrawPath(CurrentCell);
    }

    private void SetTravelDirection(Point cell, Enums.Direction direction) {
        if(!IsInbound(cell)) {
            return;
        }

        MazePaths[cell.X, cell.Y] = direction;
    }

    private bool IsInbound(Point cell) => cell.X >= 0 && cell.X < MazeColumns && cell.Y >= 0 && cell.Y < MazeRows;

    private bool IsReachable(Point cell, Enums.Direction direction) => IsInbound(cell) && (MazeWalls[cell.X, cell.Y] & (int)direction) == 0;

    private void InitializeMazeWalls() {
        MazeWalls = new int[MazeColumns, MazeRows];
        for(var i = 0; i < MazeColumns; i++) {
            MazeWalls[i, 0] |= (int)Enums.Direction.Backward;
            MazeWalls[i, MazeRows - 1] |= (int)Enums.Direction.Forward;
            UpdateWallCellWallMarkers(new Point(i, 0));
            UpdateWallCellWallMarkers(new Point(i, MazeRows - 1));
        }
        for (var i = 0; i < MazeRows; i++) {
            MazeWalls[0, i] |= (int)Enums.Direction.Left;
            MazeWalls[MazeColumns - 1, i] |= (int)Enums.Direction.Right;
            UpdateWallCellWallMarkers(new Point(0, i));
            UpdateWallCellWallMarkers(new Point(MazeColumns - 1, i));
        }
    }

    private void UpdateCellWalls() {
        int cellWalls = (int)Enums.Direction.None;
        cellWalls |= SensorCenter.ObstacleDetected ? (int)Enums.Direction.Forward : (int)Enums.Direction.None;
        cellWalls |= SensorRight.ObstacleDetected ? (int)Enums.Direction.Right : (int)Enums.Direction.None;
        cellWalls |= SensorLeft.ObstacleDetected ? (int)Enums.Direction.Left : (int)Enums.Direction.None;

        if (cellWalls == (int)Enums.Direction.None) {
            return;
        }

        if ((((int)Enums.Direction.Forward << 2) & (int)CurrentDirection) > 0) {
            cellWalls = (cellWalls >> 2) | (cellWalls << 2);
        } else if ((((int)Enums.Direction.Forward << 3) & (int)CurrentDirection) > 0) {
            cellWalls = (cellWalls >> 1) | (cellWalls << 3);
        } else if ((((int)Enums.Direction.Forward << 1) & (int)CurrentDirection) > 0) {
            cellWalls = (cellWalls << 1) | (cellWalls >> 3);
        }

        cellWalls &= 0xF;

        Debug.Log($"cellWalls[{CurrentCell.X}, {CurrentCell.Y}]:{Convert.ToString(cellWalls, 2)}");

        if((MazeWalls[CurrentCell.X, CurrentCell.Y] & cellWalls) == cellWalls) {
            return;
        }

        MazeWalls[CurrentCell.X, CurrentCell.Y] |= cellWalls;
        UpdateWallCellWallMarkers(CurrentCell);

        if ((cellWalls & (int)Enums.Direction.Left) > 0 && IsInbound(new Point(CurrentCell.X - 1, CurrentCell.Y))) {
            MazeWalls[CurrentCell.X - 1, CurrentCell.Y] |= (int)Enums.Direction.Right;
            UpdateWallCellWallMarkers(new Point(CurrentCell.X - 1, CurrentCell.Y));
        }
        if ((cellWalls & (int)Enums.Direction.Right) > 0 && IsInbound(new Point(CurrentCell.X + 1, CurrentCell.Y))) {
            MazeWalls[CurrentCell.X + 1, CurrentCell.Y] |= (int)Enums.Direction.Left;
            UpdateWallCellWallMarkers(new Point(CurrentCell.X + 1, CurrentCell.Y));
        }
        if ((cellWalls & (int)Enums.Direction.Backward) > 0 && IsInbound(new Point(CurrentCell.X, CurrentCell.Y - 1))) {
            MazeWalls[CurrentCell.X, CurrentCell.Y - 1] |= (int)Enums.Direction.Forward;
            UpdateWallCellWallMarkers(new Point(CurrentCell.X, CurrentCell.Y - 1));
        }
        if ((cellWalls & (int)Enums.Direction.Forward) > 0 && IsInbound(new Point(CurrentCell.X, CurrentCell.Y + 1))) {
            MazeWalls[CurrentCell.X, CurrentCell.Y + 1] |= (int)Enums.Direction.Backward;
            UpdateWallCellWallMarkers(new Point(CurrentCell.X, CurrentCell.Y + 1));
        }
    }

    private void InitializePathMarkers() {
        PathMarkersObject = GameObject.Find("PathMarkers");
        Debug.Log($"PathMarkers object found: {PathMarkersObject != null}");
        if(PathMarkersObject == null) {
            return;
        }

        PathMarkersObject.SetActive(ShowPathMarkers);

        var pathMarkersTransform = PathMarkersObject.GetComponent<Transform>();
        var markerColumns = PathMarkersObject
            .GetComponentsInChildren<Transform>()
            .Where(t => t.parent == pathMarkersTransform)
            .ToList();
        Debug.Log($"MarkerColumns count: {markerColumns.Count}");
        PathMarkers = new GameObject[MazeColumns, MazeRows];
        for (var j = 0; j < MazeRows; j++) {
            for (var i = 0; i < MazeColumns; i++) {
                PathMarkers[i, j] = markerColumns[i]
                    .GetComponentsInChildren<Transform>()
                    .FirstOrDefault(o => o.name == $"PathMarker{j}")
                    .gameObject;
                PathMarkers[i, j].GetComponent<MeshRenderer>().enabled = false;
                foreach (var wall in PathMarkers[i, j].GetComponentsInChildren<MeshRenderer>().ToList()) {
                    wall.enabled = false;
                }
                Debug.Log($"PathMarker[{i}, {j}] found: {PathMarkers[i, j] != null}");
                Debug.Log($"ColumnName: {markerColumns[i].name}");
                Debug.Log($"MarkerName: {PathMarkers[i, j].name}");
            }
        }
    }

    private void DrawPath(Point point) {
        foreach(var marker in PathMarkers) {
            //marker.SetActive(false);
            marker.GetComponent<MeshRenderer>().enabled = false;
        }

        var currentMarker = new Point(point.X, point.Y);
        //PathMarkers[currentMarker.X, currentMarker.Y].SetActive(true);
        PathMarkers[currentMarker.X, currentMarker.Y].GetComponent<MeshRenderer>().enabled = true;
        while (MazePaths[currentMarker.X, currentMarker.Y] != Enums.Direction.None) {
            switch(MazePaths[currentMarker.X, currentMarker.Y]) {
                case Enums.Direction.Left:
                    currentMarker.X--;
                    break;
                case Enums.Direction.Right:
                    currentMarker.X++;
                    break;
                case Enums.Direction.Backward:
                    currentMarker.Y--;
                    break;
                case Enums.Direction.Forward:
                    currentMarker.Y++;
                    break;
            }
            //PathMarkers[currentMarker.X, currentMarker.Y].SetActive(true);
            PathMarkers[currentMarker.X, currentMarker.Y].GetComponent<MeshRenderer>().enabled = true;
        }
    }

    private void UpdateWallCellWallMarkers(Point cell) {
        var markers = PathMarkers[cell.X, cell.Y].GetComponentsInChildren<MeshRenderer>(true);
        if((MazeWalls[cell.X, cell.Y] & (int)Enums.Direction.Left) > 0) {
            markers.FirstOrDefault(r => r.name == "WallMarkerLeft").enabled = true;
        }
        if ((MazeWalls[cell.X, cell.Y] & (int)Enums.Direction.Right) > 0) {
            markers.FirstOrDefault(r => r.name == "WallMarkerRight").enabled = true;
        }
        if ((MazeWalls[cell.X, cell.Y] & (int)Enums.Direction.Backward) > 0) {
            markers.FirstOrDefault(r => r.name == "WallMarkerBackward").enabled = true;
        }
        if ((MazeWalls[cell.X, cell.Y] & (int)Enums.Direction.Forward) > 0) {
            markers.FirstOrDefault(r => r.name == "WallMarkerForward").enabled = true;
        }
    }

    #endregion

    #endregion

}
