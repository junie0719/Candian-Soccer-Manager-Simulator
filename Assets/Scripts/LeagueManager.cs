
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LeagueFixture
{
    public int matchday;

    public TeamData homeTeam;
    public TeamData awayTeam;

    public int homeScore;
    public int awayScore;

    public bool played;
}

public class LeagueManager : MonoBehaviour
{
    [Header("League Settings")]
    public string leagueName = "Canadian Premier League";
    public int seasonYear = 2026;

    public List<TeamData> teams =
        new List<TeamData>();

    [Header("Managed Club")]
    public TeamData managedTeam;

    [Header("Season Progress")]
    public int currentMatchday = 1;
    public int totalMatchdays;

    [Header("League Fixtures")]
    public List<LeagueFixture> fixtures =
        new List<LeagueFixture>();

    void Start()
    {
        if (fixtures.Count == 0)
        {
            GenerateFixtures();
        }
    }

    [ContextMenu("Generate League Fixtures")]
    public void GenerateFixtures()
    {
        if (teams.Count < 2 ||
            teams.Count % 2 != 0)
        {
            Debug.LogError(
                "League requires an even number of teams.");

            return;
        }

        HashSet<TeamData> uniqueTeams =
            new HashSet<TeamData>(teams);

        if (teams.Contains(null) ||
            uniqueTeams.Count != teams.Count)
        {
            Debug.LogError(
                "League contains missing or duplicate teams.");

            return;
        }

        fixtures.Clear();

        currentMatchday = 1;

        List<TeamData> rotation =
            new List<TeamData>(teams);

        int teamCount = rotation.Count;
        int rounds = teamCount - 1;

        totalMatchdays = rounds * 2;

        // Generate the first half of the season.
        for (int round = 0; round < rounds; round++)
        {
            for (int i = 0; i < teamCount / 2; i++)
            {
                TeamData home = rotation[i];

                TeamData away =
                    rotation[teamCount - 1 - i];

                if (round % 2 == 1)
                {
                    TeamData temp = home;
                    home = away;
                    away = temp;
                }

                LeagueFixture fixture =
                    new LeagueFixture();

                fixture.matchday = round + 1;
                fixture.homeTeam = home;
                fixture.awayTeam = away;

                fixture.homeScore = 0;
                fixture.awayScore = 0;
                fixture.played = false;

                fixtures.Add(fixture);
            }

            // Rotate teams while keeping
            // the first team fixed.
            TeamData lastTeam =
                rotation[rotation.Count - 1];

            rotation.RemoveAt(rotation.Count - 1);
            rotation.Insert(1, lastTeam);
        }

        // Generate reverse fixtures.
        int firstHalfCount = fixtures.Count;

        for (int i = 0; i < firstHalfCount; i++)
        {
            LeagueFixture original = fixtures[i];

            LeagueFixture reverse =
                new LeagueFixture();

            reverse.matchday =
                original.matchday + rounds;

            reverse.homeTeam = original.awayTeam;
            reverse.awayTeam = original.homeTeam;

            reverse.homeScore = 0;
            reverse.awayScore = 0;
            reverse.played = false;

            fixtures.Add(reverse);
        }

        Debug.Log(
            "League generated: " +
            teams.Count + " teams, " +
            totalMatchdays + " matchdays, " +
            fixtures.Count + " matches.");

        PrintFixtures();
    }

    public List<LeagueFixture> GetMatchdayFixtures(
        int matchday)
    {
        List<LeagueFixture> results =
            new List<LeagueFixture>();

        foreach (LeagueFixture fixture in fixtures)
        {
            if (fixture.matchday == matchday)
            {
                results.Add(fixture);
            }
        }

        return results;
    }

    public LeagueFixture GetManagedTeamFixture()
    {
        foreach (LeagueFixture fixture in fixtures)
        {
            if (fixture.matchday != currentMatchday)
                continue;

            if (fixture.homeTeam == managedTeam ||
                fixture.awayTeam == managedTeam)
            {
                return fixture;
            }
        }

        return null;
    }

    [ContextMenu("Print League Fixtures")]
    public void PrintFixtures()
    {
        for (int day = 1; day <= totalMatchdays; day++)
        {
            string message = "MATCHDAY " + day;

            foreach (LeagueFixture fixture in fixtures)
            {
                if (fixture.matchday != day)
                    continue;

                message += "\n" +
                    fixture.homeTeam.teamName +
                    " vs " +
                    fixture.awayTeam.teamName;
            }

            Debug.Log(message);
        }
    }
}
