using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ScoreboardUI : MonoBehaviour
{
    [Header("Match References")]
    public MatchRules matchRules;
    public TeamSpawner teamSpawner;

    [Header("UI References")]
    public TMP_Text homeTeamText;
    public TMP_Text awayTeamText;
    public TMP_Text scoreText;
    public TMP_Text timerText;

    [Header("Return Button")]
    public Button returnToManagementButton;
    public string managementSceneName = "managescene";

    [Header("Match Time")]
    public float realMatchDuration = 300f;
    public float halfTimeDuration = 5f;

    public float matchMinute;
    public bool matchFinished;
    public bool halfTimeReached;
    public bool isHalfTime;

    private float elapsedTime;
    private float halfTimeTimer;
    private bool returning;

    void Start()
    {
        Time.timeScale = 1f;

        elapsedTime = 0f;
        matchMinute = 0f;

        matchFinished = false;
        halfTimeReached = false;
        isHalfTime = false;
        returning = false;

        if (returnToManagementButton != null)
        {
            returnToManagementButton.onClick.RemoveListener(
                ReturnToManagement);

            returnToManagementButton.onClick.AddListener(
                ReturnToManagement);

            returnToManagementButton.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("Return button is not assigned!");
        }

        InitializeTeamNames();
        UpdateScore();
        UpdateClock();
    }

    void Update()
    {
        if (matchRules == null)
            return;

        UpdateScore();

        if (matchFinished)
            return;

        if (isHalfTime)
        {
            halfTimeTimer -= Time.deltaTime;

            if (timerText != null)
                timerText.text = "HALF TIME";

            if (halfTimeTimer <= 0f)
            {
                isHalfTime = false;
                matchRules.StartSecondHalf();
            }

            return;
        }

        if (matchRules.isRestarting)
        {
            if (timerText != null)
                timerText.text = matchRules.restartLabel;

            return;
        }

        elapsedTime += Time.deltaTime;

        matchMinute = Mathf.Clamp(
            elapsedTime /
            Mathf.Max(realMatchDuration, 1f) * 90f,
            0f,
            90f
        );

        if (!halfTimeReached && matchMinute >= 45f)
        {
            BeginHalfTime();
            return;
        }

        if (matchMinute >= 90f)
        {
            FinishMatch();
            return;
        }

        UpdateClock();
    }

    void BeginHalfTime()
    {
        halfTimeReached = true;
        isHalfTime = true;

        matchMinute = 45f;

        elapsedTime =
            Mathf.Max(realMatchDuration, 1f) / 2f;

        halfTimeTimer = halfTimeDuration;

        matchRules.StartHalfTime();

        if (timerText != null)
            timerText.text = "HALF TIME";
    }

    void FinishMatch()
    {
        if (matchFinished)
            return;

        matchFinished = true;
        matchMinute = 90f;

        matchRules.EndMatch();

        UpdateScore();

        if (timerText != null)
            timerText.text = "FULL TIME";

        if (MatchSession.currentFixture != null)
        {
            MatchSession.homeScore = matchRules.homeScore;
            MatchSession.awayScore = matchRules.awayScore;
        }

        if (returnToManagementButton != null)
        {
            returnToManagementButton.gameObject.SetActive(true);

            returnToManagementButton.interactable = true;

            returnToManagementButton.transform.SetAsLastSibling();

            Debug.Log("Return button activated.");
        }
        else
        {
            Debug.LogError("Return button reference missing!");
        }

        Time.timeScale = 0f;

        Debug.Log("MATCH FINISHED: " +
            matchRules.homeScore + " - " +
            matchRules.awayScore);
    }

    public void ReturnToManagement()
    {
        Debug.Log("Return button clicked!");

        if (!matchFinished || returning)
            return;

        string targetScene = managementSceneName;

        if (!Application.CanStreamedLevelBeLoaded(targetScene))
        {
            if (Application.CanStreamedLevelBeLoaded("Management"))
            {
                targetScene = "Management";
            }
            else if (Application.CanStreamedLevelBeLoaded("managescene"))
            {
                targetScene = "managescene";
            }
            else
            {
                Debug.LogError(
                    "Management scene not found in Build Profiles!");

                return;
            }
        }

        returning = true;

        if (MatchSession.currentFixture != null)
        {
            MatchSession.homeScore = matchRules.homeScore;
            MatchSession.awayScore = matchRules.awayScore;

            MatchSession.returningFromMatch = true;
        }

        Time.timeScale = 1f;

        Debug.Log("Loading management scene: " + targetScene);

        SceneManager.LoadScene(targetScene);
    }

    void InitializeTeamNames()
    {
        if (teamSpawner == null)
            return;

        if (teamSpawner.homeTeam != null &&
            homeTeamText != null)
        {
            homeTeamText.text =
                string.IsNullOrWhiteSpace(
                    teamSpawner.homeTeam.teamName)
                ? teamSpawner.homeTeam.name
                : teamSpawner.homeTeam.teamName;

            homeTeamText.color =
                teamSpawner.homeTeam.teamColor;
        }

        if (teamSpawner.awayTeam != null &&
            awayTeamText != null)
        {
            awayTeamText.text =
                string.IsNullOrWhiteSpace(
                    teamSpawner.awayTeam.teamName)
                ? teamSpawner.awayTeam.name
                : teamSpawner.awayTeam.teamName;

            awayTeamText.color =
                teamSpawner.awayTeam.teamColor;
        }
    }

    void UpdateScore()
    {
        if (matchRules != null && scoreText != null)
        {
            scoreText.text =
                matchRules.homeScore +
                " - " +
                matchRules.awayScore;
        }
    }

    void UpdateClock()
    {
        if (timerText == null)
            return;

        int totalSeconds =
            Mathf.FloorToInt(matchMinute * 60f);

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        timerText.text =
            minutes.ToString("00") +
            ":" +
            seconds.ToString("00");
    }

    void OnDestroy()
    {
        if (returnToManagementButton != null)
        {
            returnToManagementButton.onClick.RemoveListener(
                ReturnToManagement);
        }

        Time.timeScale = 1f;
    }
}