
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class MatchTacticsUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject matchSquadPanel;
    public Button tacticsButton;
    public Button resumeButton;
    public Button cancelButton;

    public RectTransform squadTable;
    public RectTransform benchTable;

    public TMP_Dropdown formationDropdown;
    public TMP_Text squadTitleText;
    public TMP_Text substitutionText;

    [Header("Match References")]
    public MatchRules matchRules;
    public TeamSpawner teamSpawner;
    public BallController ballController;

    [Header("Substitution Settings")]
    public int maximumSubstitutions = 5;
    public int substitutionsUsed;

    [Header("Appearance")]
    public float rowHeight = 55f;
    public float fontSize = 26f;

    public Color headerColor =
        new Color32(20, 34, 54, 255);

    public Color rowColor =
        new Color32(35, 55, 82, 255);

    public Color alternateRowColor =
        new Color32(27, 47, 70, 255);

    public Color selectedColor =
        new Color32(55, 110, 165, 255);

    public Color naturalPositionColor =
        new Color32(100, 220, 140, 255);

    public Color outOfPositionColor =
        new Color32(255, 190, 80, 255);

    [Header("Match Status")]
    public bool tacticsOpen;
    public FormationType currentFormation;

    private FormationType pendingFormation;
    private TeamData managedTeam;
    private bool managedTeamIsHome;
    private bool formationInitialized;

    private float previousTimeScale = 1f;

    private int selectedSlot = -1;
    private PlayerData selectedBenchPlayer;

    private string statusMessage = "";

    private class LineupEntry
    {
        public PlayerController controller;
        public PlayerData player;
    }

    private List<LineupEntry> pendingLineup =
        new List<LineupEntry>();

    private HashSet<PlayerData> substitutedOut =
        new HashSet<PlayerData>();

    private readonly Dictionary<Behaviour, bool>
        pausedComponents =
        new Dictionary<Behaviour, bool>();

    private readonly FormationType[] formations =
    {
        FormationType.Formation433,
        FormationType.Formation442,
        FormationType.Formation4231,
        FormationType.Formation352,
        FormationType.Formation532,
        FormationType.Formation343
    };

    void Start()
    {
        if (matchRules == null)
            matchRules =
                FindFirstObjectByType<MatchRules>();

        if (teamSpawner == null)
            teamSpawner =
                FindFirstObjectByType<TeamSpawner>();

        if (ballController == null)
            ballController =
                FindFirstObjectByType<BallController>();

        tacticsOpen = false;
        substitutionsUsed = 0;

        substitutedOut.Clear();

        if (matchSquadPanel != null)
            matchSquadPanel.SetActive(false);

        if (tacticsButton != null)
            tacticsButton.onClick.AddListener(OpenTactics);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeMatch);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelTactics);

        SetupFormationDropdown();

        SetButtonText(resumeButton, "Apply & Resume");
        SetButtonText(cancelButton, "Cancel");
    }

    void SetButtonText(Button button, string value)
    {
        if (button == null)
            return;

        TMP_Text text =
            button.GetComponentInChildren<TMP_Text>(true);

        if (text != null)
            text.text = value;
    }

    void Update()
    {
        if (tacticsButton == null)
            return;

        tacticsButton.interactable =
            !tacticsOpen &&
            Time.timeScale > 0f &&
            matchSquadPanel != null &&
            matchRules != null &&
            !matchRules.isRestarting &&
            !matchRules.isHalfTime &&
            !matchRules.isFullTime;
    }

    void SetupFormationDropdown()
    {
        if (formationDropdown == null)
            return;

        formationDropdown.onValueChanged.RemoveListener(
            ChangeFormation);

        formationDropdown.ClearOptions();

        formationDropdown.AddOptions(
            formations.Select(
                f => FormationData.GetName(f)).ToList());

        formationDropdown.onValueChanged.AddListener(
            ChangeFormation);
    }

    bool IdentifyManagedTeam()
    {
        if (teamSpawner == null)
            return false;

        managedTeam = MatchSession.managedTeam;

        if (managedTeam == null)
            managedTeam = teamSpawner.homeTeam;

        if (managedTeam == null)
            return false;

        if (managedTeam == teamSpawner.homeTeam)
            managedTeamIsHome = true;

        else if (managedTeam == teamSpawner.awayTeam)
            managedTeamIsHome = false;

        else
            return false;

        if (!formationInitialized)
        {
            currentFormation = managedTeam.formation;
            formationInitialized = true;
        }

        return true;
    }

    public void OpenTactics()
    {
        if (tacticsOpen ||
            matchSquadPanel == null ||
            matchRules == null ||
            Time.timeScale <= 0f)
            return;

        if (matchRules.isRestarting ||
            matchRules.isHalfTime ||
            matchRules.isFullTime)
            return;

        if (!IdentifyManagedTeam())
            return;

        List<PlayerController> players =
            FindObjectsByType<PlayerController>(
                FindObjectsSortMode.None)
            .Where(p =>
                p.data != null &&
                p.isHomeTeam == managedTeamIsHome)
            .OrderBy(p => p.formationSlot)
            .ToList();

        if (players.Count != 11)
        {
            Debug.LogError("Expected 11 active players.");
            return;
        }

        for (int i = 0; i < 11; i++)
        {
            if (players[i].formationSlot != i)
            {
                Debug.LogError("Invalid formation slots.");
                return;
            }
        }

        pendingLineup.Clear();

        foreach (PlayerController player in players)
        {
            pendingLineup.Add(new LineupEntry
            {
                controller = player,
                player = player.data
            });
        }

        pendingFormation = currentFormation;

        selectedSlot = -1;
        selectedBenchPlayer = null;
        statusMessage = "";

        previousTimeScale = Time.timeScale;

        tacticsOpen = true;

        Time.timeScale = 0f;

        PauseGameplayComponents();

        matchSquadPanel.transform.SetAsLastSibling();
        matchSquadPanel.SetActive(true);

        SyncFormationDropdown();
        RefreshSquad();
    }

    void PauseGameplayComponents()
    {
        pausedComponents.Clear();

        PauseComponent(matchRules);

        PauseComponent(
            FindFirstObjectByType<MatchAI>());

        PauseComponent(
            FindFirstObjectByType<PassingAI>());

        PauseComponent(
            FindFirstObjectByType<ShootingAI>());

        PauseComponent(
            FindFirstObjectByType<GoalkeeperAI>());

        PauseComponent(ballController);

        PauseComponent(
            FindFirstObjectByType<ScoreboardUI>());
    }

    void PauseComponent(Behaviour component)
    {
        if (component == null ||
            pausedComponents.ContainsKey(component))
            return;

        pausedComponents.Add(
            component, component.enabled);

        component.enabled = false;
    }

    void RestoreGameplayComponents()
    {
        foreach (var pair in pausedComponents)
        {
            if (pair.Key != null)
                pair.Key.enabled = pair.Value;
        }

        pausedComponents.Clear();
    }

    void SyncFormationDropdown()
    {
        if (formationDropdown == null)
            return;

        int index = System.Array.IndexOf(
            formations, pendingFormation);

        formationDropdown.SetValueWithoutNotify(
            Mathf.Max(0, index));

        formationDropdown.RefreshShownValue();
    }

    int GetPendingSubstitutions()
    {
        return pendingLineup.Count(
            entry => entry.player != entry.controller.data);
    }

    bool IsPendingStarter(PlayerData player)
    {
        return pendingLineup.Any(
            entry => entry.player == player);
    }

    bool IsOriginalStarter(PlayerData player)
    {
        return pendingLineup.Any(
            entry => entry.controller.data == player);
    }

    List<PlayerData> GetBenchPlayers()
    {
        if (managedTeam == null ||
            managedTeam.squad == null)
            return new List<PlayerData>();

        return managedTeam.squad
            .Where(p =>
                p != null &&
                !IsPendingStarter(p))
            .Distinct()
            .OrderBy(p =>
                substitutedOut.Contains(p) ||
                IsOriginalStarter(p))
            .ThenBy(p => p.preferredPosition)
            .ThenByDescending(p =>
                GetPositionRating(
                    p, p.preferredPosition))
            .ToList();
    }

    public void ChangeFormation(int index)
    {
        if (!tacticsOpen ||
            index < 0 ||
            index >= formations.Length)
            return;

        FormationType chosen = formations[index];

        if (chosen == pendingFormation)
            return;

        PlayerPosition[] roles =
            FormationData.GetRoles(chosen);

        if (roles.Length != 11)
            return;

        List<LineupEntry> available =
            new List<LineupEntry>(pendingLineup);

        LineupEntry[] newLineup =
            new LineupEntry[11];

        LineupEntry goalkeeper = available
            .FirstOrDefault(entry =>
                entry.player.preferredPosition ==
                PlayerPosition.Goalkeeper);

        if (goalkeeper == null)
        {
            SyncFormationDropdown();
            return;
        }

        newLineup[0] = goalkeeper;
        available.Remove(goalkeeper);

        for (int i = 1; i < 11; i++)
        {
            PlayerPosition role = roles[i];

            LineupEntry candidate = available
                .Where(entry =>
                    entry.player.preferredPosition == role)
                .OrderByDescending(entry =>
                    GetPositionRating(entry.player, role))
                .FirstOrDefault();

            if (candidate == null)
                continue;

            newLineup[i] = candidate;
            available.Remove(candidate);
        }

        for (int i = 1; i < 11; i++)
        {
            if (newLineup[i] != null)
                continue;

            PlayerPosition role = roles[i];

            LineupEntry candidate = available
                .OrderByDescending(entry =>
                    GetPositionRating(entry.player, role))
                .FirstOrDefault();

            if (candidate == null)
            {
                SyncFormationDropdown();
                return;
            }

            newLineup[i] = candidate;
            available.Remove(candidate);
        }

        pendingLineup = newLineup.ToList();
        pendingFormation = chosen;

        selectedSlot = -1;
        selectedBenchPlayer = null;

        statusMessage =
            "Formation: " +
            FormationData.GetName(chosen);

        RefreshSquad();
    }

    public void SelectPlayer(int slot)
    {
        if (!tacticsOpen ||
            slot < 0 ||
            slot >= pendingLineup.Count)
            return;

        if (selectedBenchPlayer != null)
        {
            MakeSubstitution(
                slot, selectedBenchPlayer);

            return;
        }

        if (selectedSlot == slot)
        {
            selectedSlot = -1;
            statusMessage = "";
            RefreshSquad();
            return;
        }

        if (selectedSlot == -1)
        {
            selectedSlot = slot;

            statusMessage =
                pendingLineup[slot].player.playerName +
                " selected.";

            RefreshSquad();
            return;
        }

        if (slot == 0 || selectedSlot == 0)
        {
            selectedSlot = slot;

            statusMessage =
                "Goalkeeper cannot swap with outfield players.";

            RefreshSquad();
            return;
        }

        LineupEntry first =
            pendingLineup[selectedSlot];

        pendingLineup[selectedSlot] =
            pendingLineup[slot];

        pendingLineup[slot] = first;

        selectedSlot = -1;

        statusMessage = "Positions swapped.";

        RefreshSquad();
    }

    public void SelectBenchPlayer(PlayerData player)
    {
        if (!tacticsOpen || player == null)
            return;

        if (substitutedOut.Contains(player) ||
            IsOriginalStarter(player) ||
            IsPendingStarter(player))
        {
            statusMessage =
                "This player is unavailable.";

            RefreshSquad();
            return;
        }

        if (selectedSlot >= 0)
        {
            MakeSubstitution(selectedSlot, player);
            return;
        }

        if (selectedBenchPlayer == player)
        {
            selectedBenchPlayer = null;
            statusMessage = "";
        }
        else
        {
            selectedBenchPlayer = player;

            statusMessage =
                player.playerName +
                " selected. Choose a starter.";
        }

        RefreshSquad();
    }

    void MakeSubstitution(int slot, PlayerData incoming)
    {
        if (slot < 0 ||
            slot >= pendingLineup.Count ||
            incoming == null)
            return;

        if (substitutedOut.Contains(incoming) ||
            IsPendingStarter(incoming) ||
            IsOriginalStarter(incoming))
        {
            statusMessage = "Player unavailable.";
            RefreshSquad();
            return;
        }

        LineupEntry entry = pendingLineup[slot];

        bool alreadyPending =
            entry.player != entry.controller.data;

        int additionalSubstitution =
            alreadyPending ? 0 : 1;

        if (substitutionsUsed +
            GetPendingSubstitutions() +
            additionalSubstitution >
            maximumSubstitutions)
        {
            statusMessage =
                "Substitution limit reached.";

            RefreshSquad();
            return;
        }

        bool incomingIsGoalkeeper =
            incoming.preferredPosition ==
            PlayerPosition.Goalkeeper;

        if ((slot == 0) != incomingIsGoalkeeper)
        {
            statusMessage =
                "Goalkeepers must replace goalkeepers.";

            RefreshSquad();
            return;
        }

        // Do not replace a player currently holding the ball.
        if (ballController != null &&
            ballController.owner == entry.controller)
        {
            statusMessage =
                "Cannot replace the current ball owner.";

            RefreshSquad();
            return;
        }

        string outgoingName =
            entry.player.playerName;

        entry.player = incoming;

        selectedSlot = -1;
        selectedBenchPlayer = null;

        statusMessage =
            incoming.playerName +
            " replaces " + outgoingName;

        RefreshSquad();
    }

    void RefreshSquad()
    {
        if (squadTitleText != null)
        {
            squadTitleText.text =
                managedTeam.teamName +
                " | Squad & Tactics";
        }

        RefreshStartingTable();
        RefreshBenchTable();
        RefreshSubstitutionText();
    }

    void ClearTable(RectTransform table)
    {
        if (table == null)
            return;

        foreach (Transform child in table)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    void RefreshStartingTable()
    {
        if (squadTable == null)
            return;

        ClearTable(squadTable);

        CreateRow(
            squadTable,
            new string[]
            {
                "STARTING XI", "", "", "", ""
            },
            headerColor,
            true,
            Color.white,
            null);

        CreateRow(
            squadTable,
            new string[]
            {
                "#", "PLAYER", "POS", "OVR", "STA"
            },
            headerColor,
            true,
            Color.white,
            null);

        PlayerPosition[] roles =
            FormationData.GetRoles(pendingFormation);

        for (int i = 0; i < pendingLineup.Count; i++)
        {
            LineupEntry entry = pendingLineup[i];

            PlayerData player = entry.player;

            PlayerPosition role = roles[i];

            bool pendingSub =
                player != entry.controller.data;

            float stamina = pendingSub
                ? player.stamina
                : entry.controller.currentStamina;

            Color positionColor =
                role == player.preferredPosition
                ? naturalPositionColor
                : outOfPositionColor;

            Color background = i % 2 == 0
                ? rowColor
                : alternateRowColor;

            if (selectedSlot == i)
                background = selectedColor;

            string playerName = player.playerName;

            if (pendingSub)
                playerName = "+ " + playerName;

            int clickedSlot = i;

            CreateRow(
                squadTable,
                new string[]
                {
                    player.shirtNumber.ToString(),
                    playerName,
                    GetPositionName(role),
                    GetPositionRating(
                        player, role).ToString(),
                    Mathf.RoundToInt(stamina) + "%"
                },
                background,
                false,
                positionColor,
                () => SelectPlayer(clickedSlot));
        }
    }

    void RefreshBenchTable()
    {
        if (benchTable == null)
            return;

        ClearTable(benchTable);

        CreateRow(
            benchTable,
            new string[]
            {
                "BENCH", "", "", "", ""
            },
            headerColor,
            true,
            Color.white,
            null);

        CreateRow(
            benchTable,
            new string[]
            {
                "#", "PLAYER", "POS", "OVR", "STA"
            },
            headerColor,
            true,
            Color.white,
            null);

        List<PlayerData> benchPlayers =
            GetBenchPlayers();

        for (int i = 0; i < benchPlayers.Count; i++)
        {
            PlayerData player = benchPlayers[i];

            bool unavailable =
                substitutedOut.Contains(player) ||
                IsOriginalStarter(player);

            Color background = i % 2 == 0
                ? rowColor
                : alternateRowColor;

            if (selectedBenchPlayer == player)
                background = selectedColor;

            if (unavailable)
                background =
                    new Color32(55, 45, 50, 255);

            string staminaText = unavailable
                ? "OUT"
                : Mathf.RoundToInt(player.stamina) + "%";

            PlayerData clickedPlayer = player;

            CreateRow(
                benchTable,
                new string[]
                {
                    player.shirtNumber.ToString(),
                    player.playerName,
                    GetPositionName(
                        player.preferredPosition),
                    GetPositionRating(
                        player,
                        player.preferredPosition).ToString(),
                    staminaText
                },
                background,
                false,
                unavailable
                    ? new Color32(180, 130, 130, 255)
                    : naturalPositionColor,
                unavailable
                    ? null
                    : () => SelectBenchPlayer(clickedPlayer));
        }
    }

    void RefreshSubstitutionText()
    {
        if (substitutionText == null)
            return;

        int pending = GetPendingSubstitutions();

        substitutionText.text =
            "Substitutions: " +
            (substitutionsUsed + pending) +
            "/" + maximumSubstitutions;

        if (pending > 0)
        {
            substitutionText.text +=
                " (" + pending + " pending)";
        }

        if (!string.IsNullOrEmpty(statusMessage))
        {
            substitutionText.text +=
                "\n" + statusMessage;
        }
    }

    void CreateRow(
        RectTransform table,
        string[] values,
        Color background,
        bool isHeader,
        Color positionColor,
        System.Action onClick)
    {
        GameObject row = new GameObject(
            isHeader ? "Header" : values[1],
            typeof(RectTransform),
            typeof(Image),
            typeof(HorizontalLayoutGroup),
            typeof(LayoutElement));

        row.transform.SetParent(table, false);

        Image image = row.GetComponent<Image>();

        image.color = background;
        image.raycastTarget = onClick != null;

        LayoutElement layout =
            row.GetComponent<LayoutElement>();

        layout.preferredHeight = rowHeight;
        layout.minHeight = rowHeight;
        layout.flexibleHeight = 0f;

        HorizontalLayoutGroup horizontal =
            row.GetComponent<HorizontalLayoutGroup>();

        horizontal.padding =
            new RectOffset(8, 8, 0, 0);

        horizontal.spacing = 5f;

        horizontal.childAlignment =
            TextAnchor.MiddleCenter;

        horizontal.childControlWidth = true;
        horizontal.childControlHeight = true;
        horizontal.childForceExpandWidth = false;
        horizontal.childForceExpandHeight = true;

        if (onClick != null)
        {
            Button button =
                row.AddComponent<Button>();

            button.targetGraphic = image;

            ColorBlock colors = button.colors;

            colors.normalColor = Color.white;

            colors.highlightedColor =
                new Color(1.2f, 1.2f, 1.2f, 1f);

            colors.pressedColor =
                new Color(0.8f, 0.8f, 0.8f, 1f);

            colors.selectedColor = Color.white;

            button.colors = colors;

            Navigation navigation = button.navigation;

            navigation.mode = Navigation.Mode.None;

            button.navigation = navigation;

            button.onClick.AddListener(
                () => onClick());
        }

        bool sectionHeader =
            isHeader && values[1] == "";

        if (sectionHeader)
        {
            CreateCell(
                row.transform,
                values[0],
                1f,
                Color.white,
                true,
                TextAlignmentOptions.Center);

            return;
        }

        float[] weights =
        {
            0.6f,
            3.4f,
            1f,
            0.8f,
            1.1f
        };

        for (int i = 0; i < values.Length; i++)
        {
            Color color = Color.white;

            if (!isHeader && i == 2)
                color = positionColor;

            CreateCell(
                row.transform,
                values[i],
                weights[i],
                color,
                isHeader,
                i == 1
                    ? TextAlignmentOptions.MidlineLeft
                    : TextAlignmentOptions.Center);
        }
    }

    void CreateCell(
        Transform parent,
        string value,
        float weight,
        Color color,
        bool bold,
        TextAlignmentOptions alignment)
    {
        GameObject cell = new GameObject(
            "Cell",
            typeof(RectTransform),
            typeof(TextMeshProUGUI),
            typeof(LayoutElement));

        cell.transform.SetParent(parent, false);

        TextMeshProUGUI text =
            cell.GetComponent<TextMeshProUGUI>();

        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;

        text.textWrappingMode =
            TextWrappingModes.NoWrap;

        text.overflowMode =
            TextOverflowModes.Truncate;

        if (bold)
            text.fontStyle = FontStyles.Bold;

        LayoutElement layout =
            cell.GetComponent<LayoutElement>();

        layout.minWidth = 0f;
        layout.preferredWidth = 0f;
        layout.flexibleWidth = weight;
    }

    string GetPositionName(PlayerPosition position)
    {
        switch (position)
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

    int GetPositionRating(
        PlayerData player,
        PlayerPosition position)
    {
        float rating = 0f;

        switch (position)
        {
            case PlayerPosition.Goalkeeper:

                rating =
                    player.goalkeeping * 0.60f +
                    player.positioning * 0.25f +
                    player.decisionMaking * 0.15f;

                break;

            case PlayerPosition.Defender:

                rating =
                    player.defending * 0.40f +
                    player.positioning * 0.20f +
                    player.strength * 0.15f +
                    player.speed * 0.15f +
                    player.passing * 0.10f;

                break;

            case PlayerPosition.Midfielder:

                rating =
                    player.passing * 0.30f +
                    player.dribbling * 0.20f +
                    player.decisionMaking * 0.20f +
                    player.positioning * 0.15f +
                    player.stamina * 0.15f;

                break;

            case PlayerPosition.Forward:

                rating =
                    player.shooting * 0.40f +
                    player.dribbling * 0.20f +
                    player.speed * 0.20f +
                    player.positioning * 0.10f +
                    player.strength * 0.10f;

                break;
        }

        return Mathf.Clamp(
            Mathf.RoundToInt(rating), 0, 100);
    }

    bool ApplyTactics()
    {
        if (pendingLineup.Count != 11)
            return false;

        int pendingSubs =
            GetPendingSubstitutions();

        if (substitutionsUsed + pendingSubs >
            maximumSubstitutions)
            return false;

        Vector2[] positions =
            FormationData.GetPositions(pendingFormation);

        PlayerPosition[] roles =
            FormationData.GetRoles(pendingFormation);

        if (positions.Length != 11 ||
            roles.Length != 11)
            return false;

        HashSet<PlayerData> usedPlayers =
            new HashSet<PlayerData>();

        HashSet<PlayerController> usedControllers =
            new HashSet<PlayerController>();

        // Validate before making any changes.
        for (int i = 0; i < 11; i++)
        {
            LineupEntry entry = pendingLineup[i];

            if (entry.controller == null ||
                entry.player == null ||
                entry.controller.isHomeTeam !=
                    managedTeamIsHome ||
                !usedPlayers.Add(entry.player) ||
                !usedControllers.Add(entry.controller) ||
                substitutedOut.Contains(entry.player))
                return false;

            bool isGoalkeeper =
                entry.player.preferredPosition ==
                PlayerPosition.Goalkeeper;

            if ((i == 0) != isGoalkeeper)
                return false;

            if (entry.player != entry.controller.data &&
                ballController != null &&
                ballController.owner == entry.controller)
            {
                statusMessage =
                    "Cannot substitute the ball owner.";

                return false;
            }
        }

        for (int i = 0; i < 11; i++)
        {
            LineupEntry entry = pendingLineup[i];

            PlayerController controller =
                entry.controller;

            if (entry.player != controller.data)
            {
                PlayerData outgoing = controller.data;

                substitutedOut.Add(outgoing);

                Debug.Log(
                    "Substitution: " +
                    outgoing.playerName +
                    " OFF, " +
                    entry.player.playerName +
                    " ON");

                // Reuse the existing GameObject.
                controller.Initialize(
                    entry.player,
                    managedTeamIsHome);

                substitutionsUsed++;
            }

            Vector2 position = positions[i];

            if (!managedTeamIsHome)
                position.x = -position.x;

            controller.SetTacticalPosition(
                roles[i],
                position,
                i);
        }

        currentFormation = pendingFormation;

        return true;
    }

    public void ResumeMatch()
    {
        if (!tacticsOpen)
            return;

        if (!ApplyTactics())
        {
            if (string.IsNullOrEmpty(statusMessage))
                statusMessage =
                    "Unable to apply changes.";

            RefreshSubstitutionText();

            Debug.LogError(
                "Unable to apply tactics.");

            return;
        }

        CloseTactics();
    }

    public void CancelTactics()
    {
        if (!tacticsOpen)
            return;

        Debug.Log("Pending tactical changes discarded.");

        CloseTactics();
    }

    void CloseTactics()
    {
        tacticsOpen = false;

        selectedSlot = -1;
        selectedBenchPlayer = null;

        pendingLineup.Clear();

        if (matchSquadPanel != null)
            matchSquadPanel.SetActive(false);

        RestoreGameplayComponents();

        Time.timeScale = previousTimeScale;
    }

    void OnDestroy()
    {
        if (tacticsButton != null)
            tacticsButton.onClick.RemoveListener(
                OpenTactics);

        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(
                ResumeMatch);

        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(
                CancelTactics);

        if (formationDropdown != null)
            formationDropdown.onValueChanged.RemoveListener(
                ChangeFormation);

        if (tacticsOpen)
        {
            RestoreGameplayComponents();

            Time.timeScale = previousTimeScale;
        }
    }
}
