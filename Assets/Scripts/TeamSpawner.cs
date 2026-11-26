using UnityEngine;

public class TeamSpawner : MonoBehaviour
{
    [Header("Player Prefab")]
    public GameObject playerPrefab;

    [Header("Teams")]
    public TeamData homeTeam;
    public TeamData awayTeam;

    private Vector2[] formation = new Vector2[]
    {
        // Goalkeeper
        new Vector2(-2.8f, 0f),

        // Defenders
        new Vector2(-2.1f, 1.3f),
        new Vector2(-2.1f, 0.45f),
        new Vector2(-2.1f, -0.45f),
        new Vector2(-2.1f, -1.3f),

        // Midfielders
        new Vector2(-1.2f, 1f),
        new Vector2(-1.2f, 0f),
        new Vector2(-1.2f, -1f),

        // Forwards
        new Vector2(-0.4f, 1.2f),
        new Vector2(-0.4f, 0f),
        new Vector2(-0.4f, -1.2f)
    };

    void Awake()
    {
        if (MatchSession.currentFixture != null)
        {
            homeTeam =
                MatchSession.currentFixture.homeTeam;

            awayTeam =
                MatchSession.currentFixture.awayTeam;
        }
    }

    void Start()
    {
        if (playerPrefab == null ||
            homeTeam == null ||
            awayTeam == null)
        {
            Debug.LogError("Missing prefab or team data!");
            return;
        }

        if (playerPrefab.GetComponent<PlayerController>() == null)
        {
            Debug.LogError("Player prefab needs PlayerController!");
            return;
        }

        SpawnTeam(homeTeam, true);
        SpawnTeam(awayTeam, false);

        Debug.Log(
            "Playing fixture: " +
            homeTeam.teamName + " vs " +
            awayTeam.teamName
        );
    }

    void SpawnTeam(TeamData teamData, bool isHome)
    {
        if (teamData.startingLineup == null ||
            teamData.startingLineup.Count != 11 ||
            teamData.startingLineup.Contains(null))
        {
            Debug.LogError(
                teamData.teamName +
                " needs 11 valid starting players!"
            );

            return;
        }

        GameObject team =
            new GameObject(teamData.teamName);

        for (int i = 0; i < formation.Length; i++)
        {
            Vector2 position = formation[i];

            if (!isHome)
                position.x = -position.x;

            GameObject player = Instantiate(
                playerPrefab,
                position,
                Quaternion.identity,
                team.transform
            );

            player.GetComponent<SpriteRenderer>().color =
                teamData.teamColor;

            PlayerController controller =
                player.GetComponent<PlayerController>();

            controller.Initialize(
                teamData.startingLineup[i],
                isHome
            );
        }
    }
}