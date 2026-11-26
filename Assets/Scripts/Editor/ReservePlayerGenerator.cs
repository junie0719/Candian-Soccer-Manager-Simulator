
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class ReservePlayerGenerator
{
    static readonly string[] firstNames =
    {
        "James", "Oliver", "Noah", "Liam",
        "Ethan", "Lucas", "Daniel", "Ryan",
        "Adam", "Jack", "Samuel", "Nathan",
        "Benjamin", "Oscar", "Leo", "Thomas",
        "Jacob", "William", "Mason", "Dylan"
    };

    static readonly string[] lastNames =
    {
        "Wilson", "Brown", "Clark", "Taylor",
        "Martin", "Scott", "Walker", "Smith",
        "Anderson", "Wright", "Brooks", "Hill",
        "Moore", "Lewis", "Young", "King",
        "Baker", "Green", "Hall", "Adams"
    };

    [MenuItem("Football Manager/Generate Reserve Players")]
    public static void GenerateReserves()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play Mode first.");
            return;
        }

        string[] teamGuids = AssetDatabase.FindAssets(
            "t:TeamData",
            new[] { "Assets/Data/Teams" }
        );

        int totalCreated = 0;

        foreach (string guid in teamGuids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            TeamData team =
                AssetDatabase.LoadAssetAtPath<TeamData>(path);

            if (team == null)
                continue;

            Undo.RecordObject(team, "Generate Reserves");

            // Remove duplicate and empty references.
            team.squad = team.squad == null
                ? new List<PlayerData>()
                : team.squad
                    .Where(p => p != null)
                    .Distinct()
                    .ToList();

            // Preserve all existing starters.
            if (team.startingLineup != null)
            {
                foreach (PlayerData player
                         in team.startingLineup)
                {
                    if (player != null &&
                        !team.squad.Contains(player))
                    {
                        team.squad.Add(player);
                    }
                }
            }

            string root = "Assets/Data/Players";

            if (!AssetDatabase.IsValidFolder(
                root + "/Reserves"))
            {
                AssetDatabase.CreateFolder(
                    root, "Reserves");
            }

            string reserveRoot = root + "/Reserves";

            string folderName =
                team.teamName.Replace(" ", "_");

            string teamFolder =
                reserveRoot + "/" + folderName;

            if (!AssetDatabase.IsValidFolder(teamFolder))
            {
                AssetDatabase.CreateFolder(
                    reserveRoot, folderName);
            }

            int created = 0;

            while (team.squad.Count < 22)
            {
                PlayerPosition position =
                    GetNeededPosition(team);

                PlayerData player =
                    ScriptableObject.CreateInstance<PlayerData>();

                player.playerName = GenerateName(team);
                player.age = Random.Range(18, 34);
                player.shirtNumber = GetAvailableNumber(team);
                player.preferredPosition = position;

                player.passing = Random.Range(48, 76);
                player.shooting = Random.Range(40, 72);
                player.defending = Random.Range(40, 72);
                player.dribbling = Random.Range(45, 76);
                player.goalkeeping = Random.Range(10, 30);
                player.speed = Random.Range(50, 85);
                player.stamina = Random.Range(55, 85);
                player.strength = Random.Range(45, 80);
                player.positioning = Random.Range(50, 78);
                player.decisionMaking = Random.Range(45, 76);

                switch (position)
                {
                    case PlayerPosition.Goalkeeper:
                        player.goalkeeping =
                            Random.Range(60, 78);
                        break;

                    case PlayerPosition.Defender:
                        player.defending =
                            Random.Range(60, 78);
                        break;

                    case PlayerPosition.Midfielder:
                        player.passing =
                            Random.Range(60, 78);

                        player.dribbling =
                            Random.Range(58, 78);
                        break;

                    case PlayerPosition.Forward:
                        player.shooting =
                            Random.Range(60, 78);
                        break;
                }

                player.potential =
                    Random.Range(72, 91);

                player.marketValue =
                    Random.Range(80, 501) * 1000;

                string assetPath =
                    teamFolder + "/Reserve_" +
                    player.shirtNumber + ".asset";

                assetPath =
                    AssetDatabase.GenerateUniqueAssetPath(
                        assetPath);

                AssetDatabase.CreateAsset(
                    player, assetPath);

                team.squad.Add(player);

                created++;
                totalCreated++;
            }

            EditorUtility.SetDirty(team);

            Debug.Log(
                team.teamName +
                " | Squad: " + team.squad.Count +
                " | New reserves: " + created);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "RESERVE GENERATION COMPLETE! " +
            totalCreated + " new players created.");
    }

    static PlayerPosition GetNeededPosition(TeamData team)
    {
        if (team.squad.Count(p =>
            p.preferredPosition ==
            PlayerPosition.Goalkeeper) < 2)
            return PlayerPosition.Goalkeeper;

        if (team.squad.Count(p =>
            p.preferredPosition ==
            PlayerPosition.Defender) < 8)
            return PlayerPosition.Defender;

        if (team.squad.Count(p =>
            p.preferredPosition ==
            PlayerPosition.Midfielder) < 6)
            return PlayerPosition.Midfielder;

        return PlayerPosition.Forward;
    }

    static int GetAvailableNumber(TeamData team)
    {
        for (int number = 1; number <= 99; number++)
        {
            if (!team.squad.Any(
                p => p.shirtNumber == number))
            {
                return number;
            }
        }

        return 99;
    }

    static string GenerateName(TeamData team)
    {
        string name;

        do
        {
            name =
                firstNames[Random.Range(
                    0, firstNames.Length)] +
                " " +
                lastNames[Random.Range(
                    0, lastNames.Length)];
        }
        while (team.squad.Any(
            p => p.playerName == name));

        return name;
    }
}
