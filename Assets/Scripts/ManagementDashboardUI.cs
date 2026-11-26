using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ManagementDashboardUI : MonoBehaviour
{
    [Header("League Reference")]
    public LeagueManager leagueManager;
    public LeagueSimulator leagueSimulator;
    public MatchSceneLoader matchSceneLoader;

    [Header("Header UI")]
    public TMP_Text clubNameText;
    public TMP_Text seasonText;
    public TMP_Text budgetText;

    [Header("Upcoming Match UI")]
    public TMP_Text matchdayText;
    public TMP_Text homeTeamText;
    public TMP_Text awayTeamText;

    [Header("Match Buttons")]
    public Button playMatchButton;
    public Button simulateMatchButton;

    void Start()
    {
        FindReferences();

        if (playMatchButton != null)
        {
            playMatchButton.onClick.RemoveAllListeners();
            playMatchButton.onClick.AddListener(PlayMatch);
        }

        if (simulateMatchButton != null)
        {
            simulateMatchButton.onClick.RemoveAllListeners();
            simulateMatchButton.onClick.AddListener(SimulateMatch);
        }

        RefreshDashboard();
    }

    void FindReferences()
    {
        if (leagueManager == null)
            leagueManager =
                FindFirstObjectByType<LeagueManager>();

        if (leagueManager != null)
        {
            if (leagueSimulator == null)
                leagueSimulator =
                    leagueManager.GetComponent<LeagueSimulator>();

            if (matchSceneLoader == null)
                matchSceneLoader =
                    leagueManager.GetComponent<MatchSceneLoader>();
        }
    }

    void Update()
    {
        RefreshDashboard();
    }

    public void PlayMatch()
    {
        Debug.Log("Play Match clicked!");

        FindReferences();

        if (matchSceneLoader == null)
        {
            Debug.LogError("MatchSceneLoader missing!");
            return;
        }

        if (!ValidateFixture())
            return;

        matchSceneLoader.PlayMatch();
    }

    public void SimulateMatch()
    {
        Debug.Log("Simulate Match clicked!");

        FindReferences();

        if (leagueSimulator == null)
        {
            Debug.LogError("LeagueSimulator missing!");
            return;
        }

        if (!ValidateFixture())
            return;

        leagueSimulator.SimulateCurrentMatchday();

        RefreshDashboard();
    }

    bool ValidateFixture()
    {
        if (leagueManager == null)
        {
            Debug.LogError("LeagueManager missing!");
            return false;
        }

        LeagueFixture fixture =
            leagueManager.GetManagedTeamFixture();

        if (fixture == null)
        {
            Debug.LogError("No upcoming fixture found!");
            return false;
        }

        if (fixture.played)
        {
            Debug.LogError("This fixture is already played!");
            return false;
        }

        return true;
    }

    public void RefreshDashboard()
    {
        if (leagueManager == null)
            return;

        if (leagueManager.managedTeam != null)
        {
            if (clubNameText != null)
                clubNameText.text =
                    leagueManager.managedTeam.teamName;

            if (budgetText != null)
                budgetText.text =
                    "TRANSFER BUDGET\n$" +
                    leagueManager.managedTeam.transferBudget
                    .ToString("N0");
        }

        if (seasonText != null)
            seasonText.text =
                "Manager Career | Season " +
                leagueManager.seasonYear;

        bool seasonComplete =
            leagueManager.totalMatchdays > 0 &&
            leagueManager.currentMatchday >
            leagueManager.totalMatchdays;

        if (playMatchButton != null)
            playMatchButton.interactable = !seasonComplete;

        if (simulateMatchButton != null)
            simulateMatchButton.interactable = !seasonComplete;

        if (seasonComplete)
        {
            if (matchdayText != null)
                matchdayText.text = "SEASON COMPLETE";

            if (homeTeamText != null)
                homeTeamText.text = "-";

            if (awayTeamText != null)
                awayTeamText.text = "-";

            return;
        }

        if (matchdayText != null)
            matchdayText.text =
                leagueManager.leagueName +
                " | Matchday " +
                leagueManager.currentMatchday;

        LeagueFixture fixture =
            leagueManager.GetManagedTeamFixture();

        if (fixture != null &&
            fixture.homeTeam != null &&
            fixture.awayTeam != null)
        {
            if (homeTeamText != null)
                homeTeamText.text =
                    fixture.homeTeam.teamName;

            if (awayTeamText != null)
                awayTeamText.text =
                    fixture.awayTeam.teamName;
        }
        else
        {
            if (homeTeamText != null)
                homeTeamText.text = "-";

            if (awayTeamText != null)
                awayTeamText.text = "-";
        }
    }
}