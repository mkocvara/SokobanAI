using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;

public class GameRule : MonoBehaviour
{
    public enum ActionType
    {
        [Description("Move onto empty ground")]
        MoveToEmptyGround,
        [Description("Move into a wall")]
        MoveIntoWall,
        [Description("Push the box")]
        PushBox,
        [Description("Push the box onto the mark")]
        BoxOnMark,
        [Description("Push the box into a wall")]
        PushBoxIntoWall
    }

    public GameObject RewardInputObject, ActionNameTextObject;

    public ActionType Action { get { return action; } }
    public int Reward { get { return reward; } }

    private TMP_InputField rewardInput;
    private TextMeshProUGUI actionNameText;
    private ActionType action;
    private int reward;

    private bool initialised = false;

    private void Start()
    {
        // Hide until initialised
        if (!initialised) gameObject.SetActive(false);
    }

    public void Init(ActionType action, int reward = 0)
    {
        rewardInput = RewardInputObject.GetComponent<TMP_InputField>();
        actionNameText = ActionNameTextObject.GetComponent<TextMeshProUGUI>();

        this.action = action;
        this.reward = reward;

        actionNameText.text = action.ToDescription();
        rewardInput.text = reward.ToString();

        initialised = true;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
    }

    public void OnEditReward()
    {
        try
        {
            reward = int.Parse(rewardInput.text);
        }
        catch
        {
            reward = 0;
        }

        // Debug.Log("GameRule.OnEditReward(): Reward for " + action.ToDescription() + " set to " + reward);
    }
}