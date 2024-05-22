using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class BestAgentsController : MonoBehaviour
{
    private ScrollRect BestAgentsScrollRect;
    private RectTransform Content;
    private Text BestAgentsText;
    private Vector2 InitialBestAgentsTextPosition;
    private Vector2 InitialBestAgentsTextSize;

    #region UpdateMethods

    private void OnEnable() {
        BestAgentsScrollRect = gameObject.GetComponentsInChildren<ScrollRect>().FirstOrDefault(b => b.name == "Fitness");
        Content = BestAgentsScrollRect.GetComponentsInChildren<RectTransform>().FirstOrDefault(b => b.name == "Content");
        BestAgentsText = Content.GetComponentsInChildren<Text>().FirstOrDefault(b => b.name == "FitnessList");
        InitialBestAgentsTextPosition = BestAgentsText.rectTransform.anchoredPosition;
        InitialBestAgentsTextSize = BestAgentsText.rectTransform.sizeDelta;
        ClearContent();
    }

    #endregion

    private void UpdateContentSize() {
        Content.sizeDelta = new Vector2(Content.sizeDelta.x, Mathf.Max(((RectTransform)Content.parent).sizeDelta.y,
            BestAgentsText.rectTransform.sizeDelta.y));
    }

    public void ClearContent() {
        UpdateAgentFitness();
    }

    public void UpdateAgentFitness(IEnumerable<float> fitnesses = null) {
        if (BestAgentsText == null) {
            return;
        }
        BestAgentsText.text = string.Join("\n", fitnesses?.Select(f => f.ToString("0")) ?? new[] { "" });
        var fitnessesCount = fitnesses?.Count() ?? 1;
        BestAgentsText.rectTransform.sizeDelta = new Vector2(InitialBestAgentsTextSize.x,
                InitialBestAgentsTextSize.y * fitnessesCount);
        BestAgentsText.rectTransform.anchoredPosition = new Vector2(InitialBestAgentsTextPosition.x,
            InitialBestAgentsTextPosition.y - (fitnessesCount - 1) * InitialBestAgentsTextSize.y / 2f);
        UpdateContentSize();
    }
}
