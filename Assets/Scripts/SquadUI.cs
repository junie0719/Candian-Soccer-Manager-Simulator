
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class SquadUI : MonoBehaviour
{
    [Header("References")]
    public LeagueManager leagueManager;
    public RectTransform squadTable;

    [Header("Appearance")]
    public float rowHeight = 26f;
    public float fontSize = 22f;

    public Color headerColor =
        new Color32(20, 34, 54, 255);

    public Color rowColor =
        new Color32(35, 55, 82, 255);

    public Color alternateRowColor =
        new Color32(27, 47, 70, 255);

    public Color startingColor =
        new Color32(100, 220, 140, 255);

    public Color selectedColor =
        new Color32(55, 110, 165, 255);

    public Color reserveColor =
        new Color32(190, 200, 215, 255);

    [Header("Selection")]
    public PlayerData selectedPlayer;
    public TMP_Text selectionMessageText;

    private string selectionMessage =
        "Select a player to change your lineup.";

    void OnEnable()
    {
        selectedPlayer = null;

        selectionMessage =
            "Select a player to change your lineup.";

        RefreshSquad();
    }

    void Start()
    {
        RefreshSquad();
    }

    public void RefreshSquad()
    {
        if (leagueManager == null ||
            squadTable == null ||
            leagueManager.managedTeam == null)
            return;

        TeamData team = leagueManager.managedTeam;

        if (team.squad == null)
            return;

        foreach (Transform child in squadTable)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        CreateRow(
            new string[]
            {
                "#", "PLAYER", "POS", "AGE",
                "OVR", "STATUS", "VALUE"
            },
            headerColor,
            true
        );

        var players = team.squad
            .Where(p => p != null)
            .Distinct()
            .OrderByDescending(p =>
                team.startingLineup.Contains(p))
            .ThenBy(p => p.preferredPosition)
            .ThenByDescending(p => GetOverall(p))
            .ToList();

        for (int i = 0; i < players.Count; i++)
        {
            PlayerData player = players[i];

            bool isStarting =
                team.startingLineup.Contains(player);

            string status = isStarting
                ? "STARTING"
                : "RESERVE";

            Color background = i % 2 == 0
                ? rowColor
                : alternateRowColor;

            if (selectedPlayer == player)
                background = selectedColor;

            CreateRow(
                new string[]
                {
                    player.shirtNumber.ToString(),
                    player.playerName,
                    GetPosition(player),
                    player.age.ToString(),
                    GetOverall(player).ToString(),
                    status,
                    "$" + player.marketValue.ToString("N0")
                },
                background,
                false,
                isStarting,
                player
            );
        }

        if (selectionMessageText != null)
        {
            selectionMessageText.text =
                selectionMessage;
        }
    }

    public void SelectPlayer(PlayerData player)
    {
        if (player == null ||
            leagueManager == null ||
            leagueManager.managedTeam == null)
            return;

        TeamData team = leagueManager.managedTeam;

        if (team.startingLineup == null ||
            team.startingLineup.Count != 11)
        {
            SetMessage("Starting lineup must contain 11 players.");
            return;
        }

        if (selectedPlayer == player)
        {
            selectedPlayer = null;

            SetMessage("Selection cancelled.");
            RefreshSquad();
            return;
        }

        if (selectedPlayer == null)
        {
            selectedPlayer = player;

            SetMessage(
                player.playerName +
                " selected. Choose a player to swap.");

            RefreshSquad();
            return;
        }

        bool firstStarting =
            team.startingLineup.Contains(selectedPlayer);

        bool secondStarting =
            team.startingLineup.Contains(player);

        // One player must be starting
        // and the other must be a reserve.
        if (firstStarting == secondStarting)
        {
            selectedPlayer = player;

            SetMessage(
                player.playerName +
                " selected. Choose a player with opposite status.");

            RefreshSquad();
            return;
        }

        // Preserve the current formation.
        if (selectedPlayer.preferredPosition !=
            player.preferredPosition)
        {
            SetMessage(
                "Players must have the same position.");

            return;
        }

        PlayerData outgoing = firstStarting
            ? selectedPlayer
            : player;

        PlayerData incoming = firstStarting
            ? player
            : selectedPlayer;

        int lineupIndex =
            team.startingLineup.IndexOf(outgoing);

        if (lineupIndex < 0)
            return;

        // Replace the player in the same formation slot.
        team.startingLineup[lineupIndex] = incoming;

        selectedPlayer = null;

        SetMessage(
            incoming.playerName +
            " replaces " +
            outgoing.playerName +
            " in the starting XI.");

        RefreshSquad();
    }

    void SetMessage(string message)
    {
        selectionMessage = message;

        if (selectionMessageText != null)
            selectionMessageText.text = message;

        Debug.Log(message);
    }

    void CreateRow(
        string[] values,
        Color background,
        bool isHeader,
        bool isStarting = false,
        PlayerData player = null)
    {
        GameObject row = new GameObject(
            isHeader ? "Header" : values[1],
            typeof(RectTransform),
            typeof(Image),
            typeof(HorizontalLayoutGroup),
            typeof(LayoutElement)
        );

        row.transform.SetParent(squadTable, false);

        Image image = row.GetComponent<Image>();
        image.color = background;
        image.raycastTarget = !isHeader;

        LayoutElement layout =
            row.GetComponent<LayoutElement>();

        layout.preferredHeight = rowHeight;
        layout.minHeight = rowHeight;
        layout.flexibleHeight = 0;

        HorizontalLayoutGroup horizontal =
            row.GetComponent<HorizontalLayoutGroup>();

        horizontal.padding =
            new RectOffset(10, 10, 0, 0);

        horizontal.spacing = 5;

        horizontal.childAlignment =
            TextAnchor.MiddleCenter;

        horizontal.childControlWidth = true;
        horizontal.childControlHeight = true;
        horizontal.childForceExpandWidth = false;
        horizontal.childForceExpandHeight = true;

        // Make each player row clickable.
        if (!isHeader && player != null)
        {
            Button button = row.AddComponent<Button>();

            button.targetGraphic = image;

            ColorBlock colors = button.colors;

            colors.normalColor = Color.white;

            colors.highlightedColor =
                new Color(1.25f, 1.25f, 1.25f, 1f);

            colors.pressedColor =
                new Color(0.8f, 0.8f, 0.8f, 1f);

            colors.selectedColor = Color.white;

            button.colors = colors;

            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            PlayerData clickedPlayer = player;

            button.onClick.AddListener(
                () => SelectPlayer(clickedPlayer));
        }

        float[] weights =
        {
            0.5f, 3.2f, 0.8f, 0.7f,
            0.7f, 1.5f, 1.6f
        };

        for (int i = 0; i < values.Length; i++)
        {
            GameObject cell = new GameObject(
                "Cell_" + i,
                typeof(RectTransform),
                typeof(TextMeshProUGUI),
                typeof(LayoutElement)
            );

            cell.transform.SetParent(
                row.transform, false);

            TextMeshProUGUI text =
                cell.GetComponent<TextMeshProUGUI>();

            text.text = values[i];
            text.fontSize = fontSize;
            text.color = Color.white;
            text.raycastTarget = false;

            text.alignment = i == 1
                ? TextAlignmentOptions.MidlineLeft
                : TextAlignmentOptions.Center;

            text.textWrappingMode =
                TextWrappingModes.NoWrap;

            text.overflowMode =
                TextOverflowModes.Truncate;

            if (isHeader)
                text.fontStyle = FontStyles.Bold;

            if (i == 5 && !isHeader)
            {
                text.color = isStarting
                    ? startingColor
                    : reserveColor;
            }

            LayoutElement cellLayout =
                cell.GetComponent<LayoutElement>();

            cellLayout.minWidth = 0;
            cellLayout.preferredWidth = 0;
            cellLayout.flexibleWidth = weights[i];
        }
    }

    string GetPosition(PlayerData player)
    {
        switch (player.preferredPosition)
        {
            case PlayerPosition.Goalkeeper:
                return "GK";

            case PlayerPosition.Defender:
                return "DEF";

            case PlayerPosition.Midfielder:
                return "MID";

            case PlayerPosition.Forward:
                return "FWD";

            default:
                return "-";
        }
    }

    int GetOverall(PlayerData p)
    {
        float rating = 0f;

        switch (p.preferredPosition)
        {
            case PlayerPosition.Goalkeeper:

                rating =
                    p.goalkeeping * 0.60f +
                    p.positioning * 0.25f +
                    p.decisionMaking * 0.15f;

                break;

            case PlayerPosition.Defender:

                rating =
                    p.defending * 0.40f +
                    p.positioning * 0.20f +
                    p.strength * 0.15f +
                    p.speed * 0.15f +
                    p.passing * 0.10f;

                break;

            case PlayerPosition.Midfielder:

                rating =
                    p.passing * 0.30f +
                    p.dribbling * 0.20f +
                    p.decisionMaking * 0.20f +
                    p.positioning * 0.15f +
                    p.stamina * 0.15f;

                break;

            case PlayerPosition.Forward:

                rating =
                    p.shooting * 0.40f +
                    p.dribbling * 0.20f +
                    p.speed * 0.20f +
                    p.positioning * 0.10f +
                    p.strength * 0.10f;

                break;
        }

        return Mathf.Clamp(
            Mathf.RoundToInt(rating), 0, 100);
    }
}
