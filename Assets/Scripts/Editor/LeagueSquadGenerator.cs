
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class LeagueSquadGenerator
{
    private static readonly string[] firstNames =
    {
        "James", "Daniel", "Lucas", "Oliver",
        "Noah", "Liam", "Ethan", "Jack",
        "Alex", "Benjamin", "Ryan", "Nathan",
        "Samuel", "Adam", "David", "Leo",
        "Gabriel", "Thomas", "Oscar", "Jacob",
        "Mateo", "Hugo", "Julian", "Max",
        "Andre", "Marcus", "Victor", "Felix"
    };

    private static readonly string[] lastNames =
    {
        "Wilson", "Martin", "Anderson", "Smith",
        "Taylor", "Brown", "Clark", "Walker",
        "Scott", "Young", "King", "Wright",
        "Mitchell", "Campbell", "Roberts",
        "Morgan", "Williams", "Johnson",
        "Garcia", "Silva", "Costa", "Reyes",
        "Lee", "Kim", "Park", "Chen",
        "Dubois", "Roy", "Gagnon", "Bouchard"
    };

    [MenuItem("Football Manager/Generate League Squads")]
    public static void GenerateLeagueSquads()
    {
        string[] teamGuids = AssetDatabase.FindAssets(
            "t:TeamData",
            new[] { "Assets/Data/Teams" });

        int generatedTeams = 0;
        int generatedPlayers = 0;

        EnsureFolder("Assets/Data", "Players");
        EnsureFolder("Assets/Data/Players", "League");

        foreach (string guid in teamGuids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            TeamData team =
                AssetDatabase.LoadAssetAtPath<TeamData>(
                    path);

            if (team == null)
                continue;

            // Preserve existing squads.
            if (team.squad != null &&
                team.squad.Count > 0)
            {
                Debug.Log(
                    "Skipping existing squad: " +
                    team.name);

                continue;
            }

            if (string.IsNullOrWhiteSpace(team.teamName))
                team.teamName = team.name;

            ConfigureTeam(team);

            int strength = GetTeamStrength(team.name);

            string folderName =
                team.name.Replace(" ", "_");

            string folder =
                "Assets/Data/Players/League/" +
                folderName;

            EnsureFolder(
                "Assets/Data/Players/League",
                folderName);

            team.squad = new List<PlayerData>();
            team.startingLineup = new List<PlayerData>();

            PlayerPosition[] formation =
            {
                PlayerPosition.Goalkeeper,

                PlayerPosition.Defender,
                PlayerPosition.Defender,
                PlayerPosition.Defender,
                PlayerPosition.Defender,

                PlayerPosition.Midfielder,
                PlayerPosition.Midfielder,
                PlayerPosition.Midfielder,

                PlayerPosition.Forward,
                PlayerPosition.Forward,
                PlayerPosition.Forward
            };

            HashSet<string> usedNames =
                new HashSet<string>();

            for (int i = 0; i < 22; i++)
            {
                bool starter = i < 11;

                PlayerPosition position =
                    formation[i % 11];

                int ability = Mathf.Clamp(
                    strength +
                    Random.Range(-7, 8) -
                    (starter ? 0 : 6),
                    45,
                    90);

                PlayerData player = CreatePlayer(
                    position,
                    ability,
                    i + 1,
                    usedNames);

                string assetPath =
                    AssetDatabase.GenerateUniqueAssetPath(
                        folder + "/" +
                        player.shirtNumber.ToString("00") +
                        "_" +
                        player.playerName.Replace(" ", "_") +
                        ".asset");

                AssetDatabase.CreateAsset(
                    player, assetPath);

                team.squad.Add(player);

                if (starter)
                    team.startingLineup.Add(player);

                generatedPlayers++;
            }

            EditorUtility.SetDirty(team);

            generatedTeams++;

            Debug.Log(
                team.teamName +
                ": Generated 22 players.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "League generation complete! " +
            generatedTeams + " teams, " +
            generatedPlayers + " new players.");
    }

    private static PlayerData CreatePlayer(
        PlayerPosition position,
        int ability,
        int shirtNumber,
        HashSet<string> usedNames)
    {
        PlayerData player =
            ScriptableObject.CreateInstance<PlayerData>();

        string fullName;

        do
        {
            fullName =
                firstNames[
                    Random.Range(0, firstNames.Length)] +
                " " +
                lastNames[
                    Random.Range(0, lastNames.Length)];
        }
        while (!usedNames.Add(fullName));

        player.playerName = fullName;
        player.shirtNumber = shirtNumber;
        player.age = Random.Range(18, 35);
        player.preferredPosition = position;

        player.passing = Attribute(ability);
        player.shooting = Attribute(ability - 15);
        player.defending = Attribute(ability - 15);
        player.dribbling = Attribute(ability);
        player.goalkeeping = Random.Range(5, 20);

        player.speed = Attribute(ability);
        player.stamina = Attribute(ability + 5);
        player.strength = Attribute(ability);
        player.positioning = Attribute(ability);
        player.decisionMaking = Attribute(ability);

        switch (position)
        {
            case PlayerPosition.Goalkeeper:

                player.goalkeeping =
                    Attribute(ability + 5);

                player.shooting =
                    Random.Range(5, 20);

                player.dribbling =
                    Attribute(ability - 25);

                player.speed =
                    Attribute(ability - 15);

                break;

            case PlayerPosition.Defender:

                player.defending =
                    Attribute(ability + 8);

                player.strength =
                    Attribute(ability + 5);

                break;

            case PlayerPosition.Midfielder:

                player.passing =
                    Attribute(ability + 8);

                player.decisionMaking =
                    Attribute(ability + 5);

                break;

            case PlayerPosition.Forward:

                player.shooting =
                    Attribute(ability + 8);

                player.dribbling =
                    Attribute(ability + 5);

                player.speed =
                    Attribute(ability + 5);

                break;
        }

        int potentialBonus =
            player.age <= 23
                ? Random.Range(5, 16)
                : Random.Range(0, 5);

        player.potential = Mathf.Clamp(
            ability + potentialBonus,
            1,
            99);

        float ageMultiplier =
            player.age <= 23 ? 1.3f :
            player.age <= 29 ? 1f : 0.65f;

        player.marketValue = Mathf.RoundToInt(
            Mathf.Pow(ability / 70f, 4f) *
            800000f *
            ageMultiplier / 10000f) * 10000;

        return player;
    }

    private static int Attribute(int value)
    {
        return Mathf.Clamp(
            value + Random.Range(-8, 9),
            1,
            99);
    }

    private static int GetTeamStrength(string name)
    {
        switch (name)
        {
            case "Toronto FC": return 78;
            case "Vancouver FC": return 76;
            case "Montreal FC": return 74;
            case "Edmonton FC": return 72;
            case "Calgary FC": return 72;
            case "Ottawa FC": return 70;
            case "Winnipeg FC": return 68;
            case "Halifax FC": return 66;
            default: return 70;
        }
    }

    private static void ConfigureTeam(TeamData team)
    {
        string color = "#FFFFFF";

        switch (team.name)
        {
            case "Toronto FC":
                team.shortName = "TOR";
                team.transferBudget = 20000000;
                color = "#A52638";
                break;

            case "Vancouver FC":
                team.shortName = "VAN";
                team.transferBudget = 18000000;
                color = "#22A06B";
                break;

            case "Montreal FC":
                team.shortName = "MTL";
                team.transferBudget = 16000000;
                color = "#7949D6";
                break;

            case "Edmonton FC":
                team.shortName = "EDM";
                team.transferBudget = 12000000;
                color = "#3478FF";
                break;

            case "Calgary FC":
                team.shortName = "CGY";
                team.transferBudget = 12000000;
                color = "#EF4444";
                break;

            case "Ottawa FC":
                team.shortName = "OTT";
                team.transferBudget = 10000000;
                color = "#F59E0B";
                break;

            case "Winnipeg FC":
                team.shortName = "WPG";
                team.transferBudget = 8000000;
                color = "#19B8C4";
                break;

            case "Halifax FC":
                team.shortName = "HFX";
                team.transferBudget = 7000000;
                color = "#A3A3A3";
                break;
        }

        if (ColorUtility.TryParseHtmlString(
            color, out Color teamColor))
        {
            team.teamColor = teamColor;
        }
    }

    private static void EnsureFolder(
        string parent,
        string folderName)
    {
        string path = parent + "/" + folderName;

        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(
                parent,
                folderName);
        }
    }
}
