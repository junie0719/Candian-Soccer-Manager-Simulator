
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PlayerNameFixer
{
    [MenuItem("Football Manager/Fix Player Names")]
    public static void FixPlayerNames()
    {
        string[] guids = AssetDatabase.FindAssets(
            "t:TeamData",
            new[] { "Assets/Data/Teams" }
        );

        HashSet<PlayerData> processed =
            new HashSet<PlayerData>();

        int renamed = 0;

        foreach (string guid in guids)
        {
            TeamData team =
                AssetDatabase.LoadAssetAtPath<TeamData>(
                    AssetDatabase.GUIDToAssetPath(guid));

            if (team == null)
                continue;

            if (team.teamName != "Edmonton FC" &&
                team.teamName != "Calgary FC")
                continue;

            foreach (PlayerData player in team.squad)
            {
                if (player == null ||
                    !processed.Add(player))
                    continue;

                if (string.IsNullOrWhiteSpace(
                    player.playerName))
                {
                    Debug.LogWarning(
                        "Missing player name: " + player.name);
                    continue;
                }

                string path =
                    AssetDatabase.GetAssetPath(player);

                string newName =
                    player.shirtNumber.ToString("00") +
                    "_" +
                    player.playerName.Replace(" ", "_");

                string error =
                    AssetDatabase.RenameAsset(path, newName);

                if (string.IsNullOrEmpty(error))
                {
                    renamed++;
                }
                else
                {
                    Debug.LogWarning(error);
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "PLAYER NAME FIX COMPLETE! " +
            renamed + " players renamed.");
    }
}
