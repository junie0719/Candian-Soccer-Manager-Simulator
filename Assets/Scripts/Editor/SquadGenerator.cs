
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SquadGenerator
{
    private static readonly PlayerPosition[] formation =
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

    private static readonly int[] shirtNumbers =
    {
        1, 2, 4, 5, 3, 6, 8, 10, 7, 9, 11
    };

    private static readonly string[] homeNames =
    {
        "Daniel Brooks",
        "James Wilson",
        "Oliver Smith",
        "Lucas Brown",
        "Ethan Clark",
        "Noah Walker",
        "Ryan Taylor",
        "Jack Anderson",
        "Liam Scott",
        "Alex Morgan",
        "Adam Wright"
    };

    private static readonly string[] awayNames =
    {
        "Thomas Reed",
        "Benjamin Hall",
        "Samuel Young",
        "Henry King",
        "Leo Martin",
        "Jacob Green",
        "Oscar Baker",
        "Max Turner",
        "Charlie Hill",
        "David Adams",
        "Nathan Evans"
    };

    [MenuItem("Football Manager/Generate Squads")]
    public static void GenerateSquads()
    {
        TeamData home = AssetDatabase.LoadAssetAtPath<TeamData>(
            "Assets/Data/Teams/Edmonton FC.asset");

        TeamData away = AssetDatabase.LoadAssetAtPath<TeamData>(
            "Assets/Data/Teams/Calgary FC.asset");

        PlayerData striker = AssetDatabase.LoadAssetAtPath<PlayerData>(
            "Assets/Data/Players/Home_Striker.asset");

        if (home == null || away == null || striker == null)
        {
            Debug.LogError("Team or striker asset not found. Check asset names.");
            return;
        }

        GenerateTeam(home, "Home", homeNames, striker);
        GenerateTeam(away, "Away", awayNames, null);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Successfully generated both squads!");
    }

    private static void GenerateTeam(
        TeamData team,
        string prefix,
        string[] names,
        PlayerData existingStriker)
    {
        team.squad.Clear();
        team.startingLineup.Clear();

        for (int i = 0; i < 11; i++)
        {
            PlayerData player;

            if (i == 9 && existingStriker != null)
            {
                player = existingStriker;
            }
            else
            {
                string path = "Assets/Data/Players/"
                    + prefix + "_Player_" + (i + 1) + ".asset";

                player = AssetDatabase.LoadAssetAtPath<PlayerData>(path);

                if (player == null)
                {
                    player = ScriptableObject.CreateInstance<PlayerData>();

                    player.playerName = names[i];
                    player.age = Random.Range(18, 35);
                    player.shirtNumber = shirtNumbers[i];
                    player.preferredPosition = formation[i];

                    player.speed = Random.Range(55, 91);
                    player.stamina = Random.Range(65, 96);
                    player.strength = Random.Range(50, 91);
                    player.passing = Random.Range(55, 86);
                    player.shooting = Random.Range(35, 66);
                    player.defending = Random.Range(35, 66);
                    player.dribbling = Random.Range(50, 86);
                    player.goalkeeping = Random.Range(5, 21);
                    player.positioning = Random.Range(55, 86);
                    player.decisionMaking = Random.Range(55, 86);

                    switch (formation[i])
                    {
                        case PlayerPosition.Goalkeeper:
                            player.goalkeeping = Random.Range(70, 96);
                            player.shooting = Random.Range(5, 26);
                            break;

                        case PlayerPosition.Defender:
                            player.defending = Random.Range(70, 96);
                            player.strength = Random.Range(70, 96);
                            break;

                        case PlayerPosition.Midfielder:
                            player.passing = Random.Range(70, 96);
                            player.decisionMaking = Random.Range(70, 91);
                            break;

                        case PlayerPosition.Forward:
                            player.shooting = Random.Range(70, 96);
                            player.dribbling = Random.Range(70, 96);
                            break;
                    }

                    player.potential = Random.Range(75, 100);
                    player.marketValue = Random.Range(50, 501) * 1000;

                    AssetDatabase.CreateAsset(player, path);
                }
            }

            team.squad.Add(player);
            team.startingLineup.Add(player);
        }

        EditorUtility.SetDirty(team);
    }
}
