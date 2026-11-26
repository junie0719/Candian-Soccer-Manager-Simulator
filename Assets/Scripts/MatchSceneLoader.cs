using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-200)]
public class MatchSceneLoader : MonoBehaviour
{
    [Header("References")]
    public LeagueManager leagueManager;
    public LeagueSimulator leagueSimulator;
    public LeagueStandings leagueStandings;

    void Awake()
    {
        if (leagueManager == null)
            leagueManager =
                FindFirstObjectByType<LeagueManager>();

        if (leagueSimulator == null)
            leagueSimulator =
                FindFirstObjectByType<LeagueSimulator>();

        if (leagueStandings == null)
            leagueStandings =
                FindFirstObjectByType<LeagueStandings>();

        if (MatchSession.returningFromMatch &&
            MatchSession.fixtures != null &&
            leagueManager != null)
        {
            MatchSession.RestoreLeague(leagueManager);
        }
    }

    void Start()
    {
        if (!MatchSession.returningFromMatch)
            return;

        if (leagueManager == null ||
            leagueStandings == null ||
            leagueSimulator == null)
        {
            Debug.LogError("Missing league references!");
            return;
        }

        LeagueFixture fixture =
            leagueManager.GetManagedTeamFixture();

        if (fixture == null)
        {
            Debug.LogError("Unable to restore match fixture!");
            return;
        }

        if (!fixture.played)
        {
            leagueStandings.RecordResult(
                fixture,
                MatchSession.homeScore,
                MatchSession.awayScore
            );
        }

        leagueSimulator.SimulateOtherMatches();

        MatchSession.returningFromMatch = false;
        MatchSession.currentFixture = null;

        Debug.Log("Returned to management successfully.");
    }

    public void PlayMatch()
    {
        if (leagueManager == null)
        {
            Debug.LogError("League Manager missing!");
            return;
        }

        LeagueFixture fixture =
            leagueManager.GetManagedTeamFixture();

        if (fixture == null || fixture.played)
        {
            Debug.LogWarning("No available fixture.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded("match scene"))
        {
            Debug.LogError("match scene is missing from Build Profiles!");
            return;
        }

        MatchSession.StoreLeague(leagueManager);

        MatchSession.currentFixture = fixture;

        MatchSession.homeScore = 0;
        MatchSession.awayScore = 0;
        MatchSession.returningFromMatch = false;

        Time.timeScale = 1f;

        Debug.Log("Loading match scene...");

        SceneManager.LoadScene("match scene");
    }
}