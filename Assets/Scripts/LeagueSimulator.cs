
using UnityEngine;
using System.Collections.Generic;

public class LeagueSimulator : MonoBehaviour
{
    [Header("References")]
    public LeagueManager leagueManager;
    public LeagueStandings leagueStandings;

    [Header("Simulation Settings")]
    public float averageGoals = 1.35f;
    public float homeAdvantage = 0.25f;
    public float strengthInfluence = 0.035f;
    public int maximumGoals = 8;

    [Header("Season Status")]
    public bool seasonFinished;

    [ContextMenu("Simulate Current Matchday")]
    public void SimulateCurrentMatchday()
    {
        if (!ValidateReferences())
            return;

        if (leagueManager.currentMatchday >
            leagueManager.totalMatchdays)
        {
            seasonFinished = true;

            Debug.Log("Season already completed.");
            return;
        }

        int day = leagueManager.currentMatchday;

        List<LeagueFixture> fixtures =
            leagueManager.GetMatchdayFixtures(day);

        if (fixtures.Count == 0)
            return;

        Debug.Log("SIMULATING MATCHDAY " + day);

        foreach (LeagueFixture fixture in fixtures)
        {
            if (!fixture.played)
            {
                SimulateFixture(fixture);
            }
        }

        AdvanceMatchday();
    }

    public void SimulateOtherMatches()
    {
        if (!ValidateReferences())
            return;

        List<LeagueFixture> fixtures =
            leagueManager.GetMatchdayFixtures(
                leagueManager.currentMatchday);

        foreach (LeagueFixture fixture in fixtures)
        {
            if (fixture.played)
                continue;

            // Skip the user's club.
            if (fixture.homeTeam ==
                leagueManager.managedTeam ||
                fixture.awayTeam ==
                leagueManager.managedTeam)
                continue;

            SimulateFixture(fixture);
        }

        AdvanceMatchday();
    }

    public void SimulateFixture(LeagueFixture fixture)
    {
        if (!ValidateReferences() ||
            fixture == null ||
            fixture.played ||
            fixture.homeTeam == null ||
            fixture.awayTeam == null)
            return;

        float homeAttack =
            GetAttackStrength(fixture.homeTeam);

        float awayAttack =
            GetAttackStrength(fixture.awayTeam);

        float homeDefense =
            GetDefenseStrength(fixture.homeTeam);

        float awayDefense =
            GetDefenseStrength(fixture.awayTeam);

        float homeExpectedGoals = Mathf.Clamp(
            averageGoals +
            homeAdvantage +
            (homeAttack - awayDefense) *
            strengthInfluence,
            0.15f,
            4.5f);

        float awayExpectedGoals = Mathf.Clamp(
            averageGoals +
            (awayAttack - homeDefense) *
            strengthInfluence,
            0.15f,
            4.5f);

        int homeGoals =
            GenerateGoals(homeExpectedGoals);

        int awayGoals =
            GenerateGoals(awayExpectedGoals);

        leagueStandings.RecordResult(
            fixture,
            homeGoals,
            awayGoals);
    }

    public float GetAttackStrength(TeamData team)
    {
        if (team == null ||
            team.startingLineup == null)
            return 60f;

        float total = 0f;
        float totalWeight = 0f;

        foreach (PlayerData player
                 in team.startingLineup)
        {
            if (player == null)
                continue;

            if (player.preferredPosition ==
                PlayerPosition.Goalkeeper)
                continue;

            float rating =
                player.shooting * 0.35f +
                player.passing * 0.25f +
                player.dribbling * 0.20f +
                player.positioning * 0.10f +
                player.decisionMaking * 0.10f;

            float weight = 1f;

            if (player.preferredPosition ==
                PlayerPosition.Forward)
                weight = 2f;

            else if (player.preferredPosition ==
                     PlayerPosition.Midfielder)
                weight = 1.5f;

            total += rating * weight;
            totalWeight += weight;
        }

        return totalWeight > 0f
            ? total / totalWeight
            : 60f;
    }

    public float GetDefenseStrength(TeamData team)
    {
        if (team == null ||
            team.startingLineup == null)
            return 60f;

        float total = 0f;
        float totalWeight = 0f;

        foreach (PlayerData player
                 in team.startingLineup)
        {
            if (player == null)
                continue;

            float rating;
            float weight;

            if (player.preferredPosition ==
                PlayerPosition.Goalkeeper)
            {
                rating =
                    player.goalkeeping * 0.75f +
                    player.positioning * 0.25f;

                weight = 3f;
            }
            else
            {
                rating =
                    player.defending * 0.45f +
                    player.positioning * 0.25f +
                    player.strength * 0.15f +
                    player.decisionMaking * 0.15f;

                weight =
                    player.preferredPosition ==
                    PlayerPosition.Defender
                    ? 2f : 0.75f;
            }

            total += rating * weight;
            totalWeight += weight;
        }

        return totalWeight > 0f
            ? total / totalWeight
            : 60f;
    }

    int GenerateGoals(float expectedGoals)
    {
        // Poisson distribution.
        float limit = Mathf.Exp(-expectedGoals);
        float probability = 1f;

        int goals = 0;

        do
        {
            goals++;

            probability *= Mathf.Max(
                Random.value,
                0.000001f);
        }
        while (probability > limit);

        return Mathf.Clamp(
            goals - 1,
            0,
            maximumGoals);
    }

    public void AdvanceMatchday()
    {
        if (!ValidateReferences())
            return;

        List<LeagueFixture> fixtures =
            leagueManager.GetMatchdayFixtures(
                leagueManager.currentMatchday);

        if (fixtures.Count == 0)
            return;

        foreach (LeagueFixture fixture in fixtures)
        {
            if (!fixture.played)
                return;
        }

        leagueManager.currentMatchday++;

        leagueStandings.RecalculateStandings();

        if (leagueManager.currentMatchday >
            leagueManager.totalMatchdays)
        {
            seasonFinished = true;

            Debug.Log("SEASON COMPLETE!");

            if (leagueStandings.standings.Count > 0)
            {
                Debug.Log(
                    "LEAGUE CHAMPION: " +
                    leagueStandings.standings[0]
                        .team.teamName);
            }
        }
        else
        {
            Debug.Log(
                "Next matchday: " +
                leagueManager.currentMatchday);
        }

        leagueStandings.PrintLeagueTable();
    }

    [ContextMenu("Simulate Entire Season")]
    public void SimulateEntireSeason()
    {
        if (!ValidateReferences())
            return;

        while (leagueManager.currentMatchday <=
               leagueManager.totalMatchdays)
        {
            int previousDay =
                leagueManager.currentMatchday;

            SimulateCurrentMatchday();

            if (leagueManager.currentMatchday ==
                previousDay)
                break;
        }
    }

    [ContextMenu("Reset Season Results")]
    public void ResetSeasonResults()
    {
        if (!ValidateReferences())
            return;

        foreach (LeagueFixture fixture
                 in leagueManager.fixtures)
        {
            fixture.homeScore = 0;
            fixture.awayScore = 0;
            fixture.played = false;
        }

        leagueManager.currentMatchday = 1;
        seasonFinished = false;

        leagueStandings.RecalculateStandings();

        Debug.Log("Season results reset.");
    }

    bool ValidateReferences()
    {
        if (leagueManager == null ||
            leagueStandings == null)
        {
            Debug.LogError(
                "League Simulator references missing.");

            return false;
        }

        return true;
    }
}
