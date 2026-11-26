
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class LeagueStanding
{
    public TeamData team;

    public int played;
    public int wins;
    public int draws;
    public int losses;

    public int goalsFor;
    public int goalsAgainst;
    public int goalDifference;

    public int points;
    public int position;
}

public class LeagueStandings : MonoBehaviour
{
    [Header("References")]
    public LeagueManager leagueManager;

    [Header("League Table")]
    public List<LeagueStanding> standings =
        new List<LeagueStanding>();

    void Start()
    {
        RecalculateStandings();
    }

    [ContextMenu("Recalculate Standings")]
    public void RecalculateStandings()
    {
        if (leagueManager == null)
        {
            Debug.LogError(
                "LeagueManager is not assigned.");

            return;
        }

        standings.Clear();

        // Create an empty record for each club.
        foreach (TeamData team in leagueManager.teams)
        {
            if (team == null)
                continue;

            LeagueStanding standing =
                new LeagueStanding();

            standing.team = team;

            standings.Add(standing);
        }

        // Process all completed matches.
        foreach (LeagueFixture fixture
                 in leagueManager.fixtures)
        {
            if (!fixture.played)
                continue;

            LeagueStanding home =
                GetStanding(fixture.homeTeam);

            LeagueStanding away =
                GetStanding(fixture.awayTeam);

            if (home == null || away == null)
                continue;

            home.played++;
            away.played++;

            home.goalsFor += fixture.homeScore;
            home.goalsAgainst += fixture.awayScore;

            away.goalsFor += fixture.awayScore;
            away.goalsAgainst += fixture.homeScore;

            if (fixture.homeScore > fixture.awayScore)
            {
                home.wins++;
                away.losses++;
            }
            else if (fixture.awayScore >
                     fixture.homeScore)
            {
                away.wins++;
                home.losses++;
            }
            else
            {
                home.draws++;
                away.draws++;
            }
        }

        // Calculate points and goal difference.
        foreach (LeagueStanding standing in standings)
        {
            standing.points =
                standing.wins * 3 +
                standing.draws;

            standing.goalDifference =
                standing.goalsFor -
                standing.goalsAgainst;
        }

        SortStandings();
    }

    void SortStandings()
    {
        standings = standings
            .OrderByDescending(s => s.points)
            .ThenByDescending(s => s.goalDifference)
            .ThenByDescending(s => s.goalsFor)
            .ThenBy(s => s.team.teamName)
            .ToList();

        for (int i = 0; i < standings.Count; i++)
        {
            standings[i].position = i + 1;
        }
    }

    public LeagueStanding GetStanding(TeamData team)
    {
        foreach (LeagueStanding standing in standings)
        {
            if (standing.team == team)
                return standing;
        }

        return null;
    }

    public bool RecordResult(
        LeagueFixture fixture,
        int homeScore,
        int awayScore)
    {
        if (leagueManager == null ||
            fixture == null ||
            fixture.played ||
            !leagueManager.fixtures.Contains(fixture) ||
            homeScore < 0 ||
            awayScore < 0)
        {
            return false;
        }

        fixture.homeScore = homeScore;
        fixture.awayScore = awayScore;
        fixture.played = true;

        RecalculateStandings();

        Debug.Log(
            fixture.homeTeam.teamName +
            " " + homeScore +
            " - " + awayScore +
            " " + fixture.awayTeam.teamName);

        return true;
    }

    [ContextMenu("Print League Table")]
    public void PrintLeagueTable()
    {
        RecalculateStandings();

        string message = "LEAGUE STANDINGS\n\n";

        foreach (LeagueStanding standing in standings)
        {
            message +=
                standing.position + ". " +
                standing.team.teamName +
                " | P: " + standing.played +
                " | W: " + standing.wins +
                " | D: " + standing.draws +
                " | L: " + standing.losses +
                " | GF: " + standing.goalsFor +
                " | GA: " + standing.goalsAgainst +
                " | GD: " + standing.goalDifference +
                " | PTS: " + standing.points +
                "\n";
        }

        Debug.Log(message);
    }
}
