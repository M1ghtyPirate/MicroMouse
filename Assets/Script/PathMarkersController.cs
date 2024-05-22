using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;

public class PathMarkersController : MonoBehaviour
{

    private GameObject PathMarkersObject;
    private GameObject[,] PathMarkers;

    private bool _ShowPathMarkers;
    [SerializeField]
    public bool ShowPathMarkers { get => _ShowPathMarkers; set { foreach (var marker in PathMarkers ?? new GameObject[0,0]) { marker.SetActive(value); } _ShowPathMarkers = value; } }
    [SerializeField]
    public MouseController MouseController;

    private void OnEnable() {
        InitializePathMarkers();
        MouseController.OnPathRecalculated += (m, p) => DrawPath(p, m.MazePaths);
        DrawPath(MouseController.CurrentCell, MouseController.MazePaths);
        MouseController.OnWallsUpdated += (m, p) => UpdateCellWallMarkers(p, m.MazeWalls[p.X, p.Y]);
        UpdateCellWallMarkers(MouseController.MazeWalls);
    }

    private void InitializePathMarkers() {
        PathMarkersObject = gameObject;
        //Debug.Log($"PathMarkers object found: {PathMarkersObject != null}");
        if (PathMarkersObject == null) {
            Debug.LogError("Unable to find PathMarkers object.");
            return;
        }

        var pathMarkersTransform = PathMarkersObject.GetComponent<Transform>();
        var markerColumns = PathMarkersObject
            .GetComponentsInChildren<Transform>()
            .Where(t => t.parent == pathMarkersTransform)
            .ToList();
        //Debug.Log($"MarkerColumns count: {markerColumns.Count}");
        var MazeColumns = markerColumns.Count;
        var FirstColumn = markerColumns.FirstOrDefault();
        var MazeRows = FirstColumn.GetComponentsInChildren<Transform>().Where(c => c.parent == FirstColumn).Count();
        PathMarkers = new GameObject[MazeColumns, MazeRows];
        for (var j = 0; j < MazeRows; j++) {
            for (var i = 0; i < MazeColumns; i++) {
                PathMarkers[i, j] = markerColumns[i]
                    .GetComponentsInChildren<Transform>()
                    .FirstOrDefault(o => o.name == $"PathMarker{j}")
                    .gameObject;
                DisablePathMarker(PathMarkers[i, j]);
                PathMarkers[i, j].SetActive(ShowPathMarkers);
                //Debug.Log($"PathMarker[{i}, {j}] found: {PathMarkers[i, j] != null}");
                //Debug.Log($"ColumnName: {markerColumns[i].name}");
                //Debug.Log($"MarkerName: {PathMarkers[i, j].name}");
            }
        }
    }

    private void DisablePathMarker(GameObject pathMarker) {
        pathMarker.GetComponent<MeshRenderer>().enabled = false;
        foreach (var wall in pathMarker.GetComponentsInChildren<MeshRenderer>().ToList()) {
            wall.enabled = false;
        }
    }

    private void DrawPath(Point cell, Enums.Direction[,] mazePaths) {
        if (PathMarkers == null) {
            Debug.LogError("PathMarkers object not found.");
            return;
        }

        foreach (var marker in PathMarkers) {
            if (marker == null) {
                Debug.LogError("Matker object not found");
                return;
            }
            marker.GetComponent<MeshRenderer>().enabled = false;
        }

        var currentMarker = new Point(cell.X, cell.Y);
        PathMarkers[currentMarker.X, currentMarker.Y].GetComponent<MeshRenderer>().enabled = true;
        while (mazePaths[currentMarker.X, currentMarker.Y] != Enums.Direction.None) {
            switch (mazePaths[currentMarker.X, currentMarker.Y]) {
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
            PathMarkers[currentMarker.X, currentMarker.Y].GetComponent<MeshRenderer>().enabled = true;
        }
    }

    private void UpdateCellWallMarkers(Point cell, int mazeWallsFlag) {
        if (PathMarkers == null) {
            Debug.LogError("PathMarkers object not found.");
            return;
        }
        var markers = PathMarkers[cell.X, cell.Y].GetComponentsInChildren<MeshRenderer>(true);
        markers.FirstOrDefault(r => r.name == "WallMarkerLeft").enabled = (mazeWallsFlag & (int)Enums.Direction.Left) > 0;
        markers.FirstOrDefault(r => r.name == "WallMarkerRight").enabled = (mazeWallsFlag & (int)Enums.Direction.Right) > 0;
        markers.FirstOrDefault(r => r.name == "WallMarkerBackward").enabled = (mazeWallsFlag & (int)Enums.Direction.Backward) > 0;
        markers.FirstOrDefault(r => r.name == "WallMarkerForward").enabled = (mazeWallsFlag & (int)Enums.Direction.Forward) > 0;
    }

    private void UpdateCellWallMarkers(int[,] mazeWalls) {
        for(var i = 0; i < mazeWalls.GetLength(0); i++) {
            for (var j = 0; j < mazeWalls.GetLength(1); j++) {
                UpdateCellWallMarkers(new Point(i, j), mazeWalls[i, j]);
            }
        }
    }
}
