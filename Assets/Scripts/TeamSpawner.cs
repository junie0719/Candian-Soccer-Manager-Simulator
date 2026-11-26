
using UnityEngine;
using System.Collections.Generic;

public class TeamSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject playerPrefab;

    [Header("Teams")]
    public TeamData homeTeam;
    public TeamData awayTeam;

    void Awake()
    {
        if (MatchSession.currentFixture != null)
        {
            homeTeam = MatchSession.currentFixture.homeTeam;
            awayTeam = MatchSession.currentFixture.awayTeam;
        }
    }

    void Start()
    {
        if (playerPrefab == null ||
            homeTeam == null ||
            awayTeam == null)
        {
            Debug.LogError("TeamSpawner references are missing.");
            return;
        }

        if (playerPrefab.GetComponent<PlayerController>() == null)
        {
            Debug.LogError("Player prefab has no PlayerController.");
            return;
        }

        if (!ValidateLineup(homeTeam) ||
            !ValidateLineup(awayTeam))
            return;

        SpawnTeam(homeTeam, true);
        SpawnTeam(awayTeam, false);
    }

    bool ValidateLineup(TeamData teamData)
    {
        if (teamData.startingLineup == null ||
            teamData.startingLineup.Count != 11)
        {
            Debug.LogError(
                teamData.teamName +
                " must have exactly 11 starting players.");

            return false;
        }

        PlayerPosition[] roles =
            FormationData.GetRoles(teamData.formation);

        Vector2[] positions =
            FormationData.GetPositions(teamData.formation);

        if (roles.Length != 11 ||
            positions.Length != 11)
        {
            Debug.LogError("Invalid formation.");
            return false;
        }

        HashSet<PlayerData> usedPlayers =
            new HashSet<PlayerData>();

        for (int i = 0; i < 11; i++)
        {
            PlayerData player =
                teamData.startingLineup[i];

            if (player == null)
            {
                Debug.LogError(
                    teamData.teamName +
                    " has a missing player.");

                return false;
            }

            if (!usedPlayers.Add(player))
            {
                Debug.LogError(
                    teamData.teamName +
                    " has duplicate players.");

                return false;
            }

            if (player.preferredPosition != roles[i])
            {
                Debug.LogError(
                    player.playerName +
                    " does not match the required position: " +
                    roles[i]);

                return false;
            }
        }

        return true;
    }

    void SpawnTeam(TeamData teamData, bool isHome)
    {
        GameObject teamObject =
            new GameObject(teamData.teamName);

        Vector2[] positions =
            FormationData.GetPositions(teamData.formation);

        PlayerPosition[] roles =
            FormationData.GetRoles(teamData.formation);

        for (int i = 0; i < 11; i++)
        {
            Vector2 spawnPosition = positions[i];

            if (!isHome)
                spawnPosition.x = -spawnPosition.x;

            GameObject playerObject = Instantiate(
                playerPrefab,
                spawnPosition,
                Quaternion.identity,
                teamObject.transform);

            SpriteRenderer sprite =
                playerObject.GetComponent<SpriteRenderer>();

            if (sprite != null)
                sprite.color = teamData.teamColor;

            PlayerController controller =
                playerObject.GetComponent<PlayerController>();

            controller.Initialize(
                teamData.startingLineup[i],
                isHome);

            // Assign tactical position and formation slot.
            controller.SetTacticalPosition(
                roles[i],
                spawnPosition,
                i);
        }

        Debug.Log(
            teamData.teamName +
            " spawned in " +
            FormationData.GetName(teamData.formation));
    }
}
