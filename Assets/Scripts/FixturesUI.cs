
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FixturesUI : MonoBehaviour
{
    [Header("References")]
    public LeagueManager leagueManager;
    public Transform fixturesTable;

    [Header("Appearance")]
    public float rowHeight = 36f;
    public float fontSize = 24f;

    public Color headerColor =
        new Color32(18, 32, 52, 255);

    public Color rowColor =
        new Color32(37, 59, 89, 255);

    public Color alternateRowColor =
        new Color32(30, 49, 77, 255);

    public Color upcomingColor =
        new Color32(44, 85, 130, 255);

    void OnEnable()
    {
        RefreshFixtures();
    }

    public void RefreshFixtures()
    {
        if (leagueManager == null ||
            fixturesTable == null)
            return;

        foreach (Transform child in fixturesTable)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        CreateRow(
            new string[]
            {
                "MD", "OPPONENT", "VENUE",
                "SCORE", "RESULT"
            },
            headerColor,
            Color.white,
            true
        );

        TeamData managedTeam =
            leagueManager.managedTeam;

        for (int day = 1;
             day <= leagueManager.totalMatchdays;
             day++)
        {
            var fixtures =
                leagueManager.GetMatchdayFixtures(day);

            foreach (LeagueFixture fixture in fixtures)
            {
                bool isHome =
                    fixture.homeTeam == managedTeam;

                bool isAway =
                    fixture.awayTeam == managedTeam;

                if (!isHome && !isAway)
                    continue;

                TeamData opponent = isHome
                    ? fixture.awayTeam
                    : fixture.homeTeam;

                if (opponent == null)
                    continue;

                string score = "-";
                string result = "-";

                Color resultColor = Color.white;

                Color backgroundColor =
                    day % 2 == 0
                    ? alternateRowColor
                    : rowColor;

                bool upcoming =
                    !fixture.played &&
                    day == leagueManager.currentMatchday;

                if (fixture.played)
                {
                    int ourGoals = isHome
                        ? fixture.homeScore
                        : fixture.awayScore;

                    int theirGoals = isHome
                        ? fixture.awayScore
                        : fixture.homeScore;

                    score = ourGoals + " - " + theirGoals;

                    if (ourGoals > theirGoals)
                    {
                        result = "WIN";
                        resultColor =
                            new Color32(100, 220, 130, 255);
                    }
                    else if (ourGoals < theirGoals)
                    {
                        result = "LOSS";
                        resultColor =
                            new Color32(240, 100, 100, 255);
                    }
                    else
                    {
                        result = "DRAW";
                        resultColor =
                            new Color32(240, 200, 90, 255);
                    }
                }
                else if (upcoming)
                {
                    result = "UPCOMING";
                    backgroundColor = upcomingColor;
                }

                CreateRow(
                    new string[]
                    {
                        day.ToString(),
                        opponent.teamName,
                        isHome ? "HOME" : "AWAY",
                        score,
                        result
                    },
                    backgroundColor,
                    resultColor,
                    upcoming
                );
            }
        }
    }

    void CreateRow(
        string[] values,
        Color backgroundColor,
        Color resultColor,
        bool bold)
    {
        GameObject row = new GameObject(
            "FixtureRow",
            typeof(RectTransform),
            typeof(Image),
            typeof(HorizontalLayoutGroup),
            typeof(LayoutElement)
        );

        row.transform.SetParent(
            fixturesTable, false);

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

        layout.spacing = 10;
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
            text.color = i == 4
                ? resultColor
                : Color.white;

            text.raycastTarget = false;

            text.textWrappingMode =
                TextWrappingModes.NoWrap;

            text.alignment = i == 1
                ? TextAlignmentOptions.MidlineLeft
                : TextAlignmentOptions.Center;

            text.fontStyle = bold
                ? FontStyles.Bold
                : FontStyles.Normal;

            LayoutElement cellLayout =
                cell.GetComponent<LayoutElement>();

            cellLayout.minWidth =
                i == 1 ? 350f : 100f;

            cellLayout.preferredWidth =
                i == 1 ? 500f : 180f;

            cellLayout.flexibleWidth =
                i == 1 ? 4f : 1f;
        }
    }
}
