
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ScoreboardUI : MonoBehaviour
{
    [Header("Match References")]
    public MatchRules matchRules;
    public TeamSpawner teamSpawner;
    public LeagueSimulator leagueSimulator;

    [Header("UI References")]
    public TMP_Text homeTeamText;
    public TMP_Text awayTeamText;
    public TMP_Text scoreText;
    public TMP_Text timerText;

    [Header("Match Buttons")]
    public Button returnToManagementButton;
    public Button simulateRemainingButton;

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

        if (leagueSimulator == null)
        {
            leagueSimulator =
                FindFirstObjectByType<LeagueSimulator>();
        }

        if (returnToManagementButton != null)
        {
            returnToManagementButton.onClick.RemoveListener(
                ReturnToManagement);

            returnToManagementButton.onClick.AddListener(
                ReturnToManagement);

            returnToManagementButton.gameObject.SetActive(false);
        }

        if (simulateRemainingButton != null)
        {
            simulateRemainingButton.onClick.RemoveListener(
                SimulateAndExit);

            simulateRemainingButton.onClick.AddListener(
                SimulateAndExit);

            simulateRemainingButton.gameObject.SetActive(true);
            simulateRemainingButton.interactable = true;
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
            90f);

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

    public void SimulateAndExit()
    {
        if (matchFinished || returning)
            return;

        if (matchRules == null ||
            teamSpawner == null ||
            leagueSimulator == null)
        {
            Debug.LogError(
                "Simulation references are missing!");
            return;
        }

        if (teamSpawner.homeTeam == null ||
            teamSpawner.awayTeam == null)
        {
            Debug.LogError("Match teams are missing!");
            return;
        }

        if (simulateRemainingButton != null)
            simulateRemainingButton.interactable = false;

        Debug.Log("Skipping match at minute " +
            matchMinute.ToString("F1"));

        leagueSimulator.SimulateRemainingMatch(
            teamSpawner.homeTeam,
            teamSpawner.awayTeam,
            matchMinute,
            ref matchRules.homeScore,
            ref matchRules.awayScore);

        FinishMatch();

        ReturnToManagement();
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

        isHalfTime = false;
        halfTimeReached = true;

        matchRules.EndMatch();

        UpdateScore();

        if (timerText != null)
            timerText.text = "FULL TIME";

        if (simulateRemainingButton != null)
        {
            simulateRemainingButton.interactable = false;

            simulateRemainingButton.gameObject.SetActive(false);
        }

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
        }

        Time.timeScale = 0f;

        Debug.Log("MATCH FINISHED: " +
            matchRules.homeScore + " - " +
            matchRules.awayScore);
    }

    public void ReturnToManagement()
    {
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

        if (simulateRemainingButton != null)
        {
            simulateRemainingButton.onClick.RemoveListener(
                SimulateAndExit);
        }

        Time.timeScale = 1f;
    }
}
