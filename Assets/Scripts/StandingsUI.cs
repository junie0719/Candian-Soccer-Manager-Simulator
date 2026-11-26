
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StandingsUI : MonoBehaviour
{
    [Header("References")]
    public LeagueStandings leagueStandings;
    public Transform standingsTable;

    [Header("Appearance")]
    public float rowHeight = 60f;
    public float fontSize = 28f;

    public Color headerColor =
        new Color32(18, 32, 52, 255);

    public Color rowColor =
        new Color32(37, 59, 89, 255);

    public Color alternateRowColor =
        new Color32(30, 49, 77, 255);

    public Color managedTeamColor =
        new Color32(44, 85, 130, 255);

    void OnEnable()
    {
        RefreshStandings();
    }

    public void RefreshStandings()
    {
        if (leagueStandings == null ||
            standingsTable == null)
            return;

        leagueStandings.RecalculateStandings();

        // Remove previous rows.
        foreach (Transform child in standingsTable)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        // Create table header.
        CreateRow(
            new string[]
            {
                "#", "CLUB", "P", "W", "D",
                "L", "GF", "GA", "GD", "PTS"
            },
            headerColor,
            true
        );

        // Generate one row for each club.
        foreach (LeagueStanding standing
                 in leagueStandings.standings)
        {
            bool isManagedTeam =
                standing.team ==
                leagueStandings.leagueManager.managedTeam;

            Color backgroundColor =
                standing.position % 2 == 0
                ? alternateRowColor
                : rowColor;

            if (isManagedTeam)
                backgroundColor = managedTeamColor;

            CreateRow(
                new string[]
                {
                    standing.position.ToString(),
                    standing.team.teamName,
                    standing.played.ToString(),
                    standing.wins.ToString(),
                    standing.draws.ToString(),
                    standing.losses.ToString(),
                    standing.goalsFor.ToString(),
                    standing.goalsAgainst.ToString(),
                    standing.goalDifference.ToString(),
                    standing.points.ToString()
                },
                backgroundColor,
                isManagedTeam
            );
        }
    }

    void CreateRow(
        string[] values,
        Color backgroundColor,
        bool bold)
    {
        GameObject row = new GameObject(
            "StandingsRow",
            typeof(RectTransform),
            typeof(Image),
            typeof(HorizontalLayoutGroup),
            typeof(LayoutElement)
        );

        row.transform.SetParent(
            standingsTable, false);

        row.GetComponent<Image>().color =
            backgroundColor;

        LayoutElement rowLayout =
            row.GetComponent<LayoutElement>();

        rowLayout.minHeight = rowHeight;
        rowLayout.preferredHeight = rowHeight;
        rowLayout.flexibleHeight = 0;

        HorizontalLayoutGroup layout =
            row.GetComponent<HorizontalLayoutGroup>();

        layout.padding =
            new RectOffset(15, 15, 0, 0);

        layout.spacing = 5;
        layout.childAlignment =
            TextAnchor.MiddleCenter;

        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

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
            text.textWrappingMode =
                TextWrappingModes.NoWrap;

            text.alignment = i == 1
                ? TextAlignmentOptions.MidlineLeft
                : TextAlignmentOptions.Center;

            text.fontStyle = bold || i == 9
                ? FontStyles.Bold
                : FontStyles.Normal;

            LayoutElement cellLayout =
                cell.GetComponent<LayoutElement>();

            cellLayout.minWidth =
                i == 1 ? 300f : 65f;

            cellLayout.preferredWidth =
                i == 1 ? 450f : 90f;

            cellLayout.flexibleWidth =
                i == 1 ? 4f : 1f;
        }
    }
}
