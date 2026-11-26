
using UnityEngine;

public class DashboardNavigation : MonoBehaviour
{
    [Header("Dashboard Panels")]
    public GameObject upcomingMatchPanel;
    public GameObject standingsPanel;
    public GameObject fixturesPanel;
    public GameObject squadPanel;

    void Start()
    {
        ShowOverview();
    }

    void HideAllPanels()
    {
        if (upcomingMatchPanel != null)
            upcomingMatchPanel.SetActive(false);

        if (standingsPanel != null)
            standingsPanel.SetActive(false);

        if (fixturesPanel != null)
            fixturesPanel.SetActive(false);

        if (squadPanel != null)
            squadPanel.SetActive(false);
    }

    public void ShowOverview()
    {
        HideAllPanels();

        if (upcomingMatchPanel != null)
            upcomingMatchPanel.SetActive(true);
    }

    public void ShowStandings()
    {
        HideAllPanels();

        if (standingsPanel != null)
            standingsPanel.SetActive(true);
    }

    public void ShowFixtures()
    {
        HideAllPanels();

        if (fixturesPanel != null)
            fixturesPanel.SetActive(true);
    }

    public void ShowSquad()
    {
        HideAllPanels();

        if (squadPanel != null)
            squadPanel.SetActive(true);
    }
}
