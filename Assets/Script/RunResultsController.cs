using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class RunResultsController : MonoBehaviour
{

    private ScrollRect RunResultsScrollRect;
    private RectTransform Content;
    private Text CurrentRunText;
    private Text PreviousRunsText;
    private Vector2 InitialPreviousRunsTextPosition;
    private Vector2 InitialPreviousRunsTextSize;
    private float TimerTime;
    private bool IsTimerRunning;
    private int CurrentRun;
    public MouseController MouseController;

    #region UpdateMethods

    private void OnEnable() {
        RunResultsScrollRect = gameObject.GetComponentsInChildren<ScrollRect>().FirstOrDefault(b => b.name == "RunResults");
        Content = RunResultsScrollRect.GetComponentsInChildren<RectTransform>().FirstOrDefault(b => b.name == "Content");
        CurrentRunText = Content.GetComponentsInChildren<Text>().FirstOrDefault(b => b.name == "CurrentRun");
        PreviousRunsText = Content.GetComponentsInChildren<Text>().FirstOrDefault(b => b.name == "PreviousRuns");
        InitialPreviousRunsTextPosition = PreviousRunsText.rectTransform.anchoredPosition;
        InitialPreviousRunsTextSize = PreviousRunsText.rectTransform.sizeDelta;
        ClearContent();
    }

    private void FixedUpdate() {
        if (!IsTimerRunning) {
            return;
        }

        TimerTime += Time.deltaTime;
        var text = $"{TimerTime:0.00}";
        if (MouseController.CurrentControlMode != Enums.ControlMode.NeuralTraining) {
            var startingCell = MouseController.TargetCell.X == MouseController.CenterCell.X && MouseController.TargetCell.Y == MouseController.CenterCell.Y ?
            MouseController.StartingCell :
            MouseController.CenterCell;
            text = $"{CurrentRun} [{startingCell.X},{startingCell.Y}]-[{MouseController.TargetCell.X},{MouseController.TargetCell.Y}] {text} {MouseController.TargetsReached}";
        }
        CurrentRunText.text = text;
    }

    #endregion

    public void StartTimer() {
        TimerTime = 0f;
        IsTimerRunning = true;
    }

    public void StopTimer() {
        TimerTime = 0f;
        IsTimerRunning = false;
    }

    private void UpdateContentSize() {
        Content.sizeDelta = new Vector2(Content.sizeDelta.x, Mathf.Max(((RectTransform)Content.parent).sizeDelta.y, 
            CurrentRunText.rectTransform.sizeDelta.y + PreviousRunsText.rectTransform.sizeDelta.y));
    }

    public void ClearContent() {
        PreviousRunsText.rectTransform.anchoredPosition = InitialPreviousRunsTextPosition;
        PreviousRunsText.rectTransform.sizeDelta = InitialPreviousRunsTextSize;
        CurrentRunText.text = "";
        PreviousRunsText.text = "";
        UpdateContentSize();
        CurrentRun = 0;
    }

    public void AddPreviousRun(string str) {
        if(!string.IsNullOrWhiteSpace(PreviousRunsText.text)) {
            str = $"{str}\n{PreviousRunsText.text}";
            PreviousRunsText.rectTransform.sizeDelta = new Vector2(PreviousRunsText.rectTransform.sizeDelta.x,
                PreviousRunsText.rectTransform.sizeDelta.y + InitialPreviousRunsTextSize.y);
            PreviousRunsText.rectTransform.position = new Vector2(PreviousRunsText.rectTransform.position.x,
                PreviousRunsText.rectTransform.position.y - InitialPreviousRunsTextSize.y / 2f);
        }
        PreviousRunsText.text = str;
        UpdateContentSize();
    }

    public void StartNewRun() {
        if(CurrentRun != 0) {
            AddPreviousRun(CurrentRunText.text);
        }
        CurrentRun++;
        StartTimer();
    }

    #region MouseEventHadlers

    public void MouseActivationChangedEventHandler(MouseController mouse, bool isActive) {
        //Debug.Log($"Event Handler: {isActive + ""}");
        if (isActive) {
            ClearContent();
            StartNewRun();
        } else {
            StopTimer();
        }
    }

    public void MouseFinalTargetReachedEventhandler(MouseController mouse, Point cell) {
        if (MouseController.CurrentControlMode == Enums.ControlMode.NeuralTraining) {
            return;
        }
        StartNewRun();
    }

    #endregion

}
