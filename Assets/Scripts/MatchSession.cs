using UnityEngine;
using System.Collections.Generic;

public static class MatchSession
{
    public static LeagueFixture currentFixture;

    public static List<LeagueFixture> fixtures;
    public static List<TeamData> teams;

    public static TeamData managedTeam;

    public static int currentMatchday;
    public static int totalMatchdays;
    public static int seasonYear;
    public static string leagueName;

    public static int homeScore;
    public static int awayScore;

    public static bool returningFromMatch;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession()
    {
        currentFixture = null;
        fixtures = null;
        teams = null;
        managedTeam = null;

        currentMatchday = 1;
        totalMatchdays = 0;
        seasonYear = 0;
        leagueName = null;

        homeScore = 0;
        awayScore = 0;
        returningFromMatch = false;
    }

    public static void StoreLeague(LeagueManager league)
    {
        fixtures = new List<LeagueFixture>(league.fixtures);
        teams = new List<TeamData>(league.teams);

        managedTeam = league.managedTeam;
        currentMatchday = league.currentMatchday;
        totalMatchdays = league.totalMatchdays;
        seasonYear = league.seasonYear;
        leagueName = league.leagueName;
    }

    public static void RestoreLeague(LeagueManager league)
    {
        league.fixtures = new List<LeagueFixture>(fixtures);
        league.teams = new List<TeamData>(teams);

        league.managedTeam = managedTeam;
        league.currentMatchday = currentMatchday;
        league.totalMatchdays = totalMatchdays;
        league.seasonYear = seasonYear;
        league.leagueName = leagueName;
    }
}