using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AppUIFlowManager : MonoBehaviour
{
    [Header("Page Roots")]
    public GameObject patientInputPage;
    public GameObject deviceConnectionPage;
    public GameObject dashboardPage;
    public GameObject doctorChatPage;
    public GameObject bottomNavigationBar;

    [Header("Navigation Buttons")]
    public Button navDashboardButton;
    public Button navDeviceButton;
    public Button navDoctorButton;

    [Header("Managers")]
    public PatientInputManager patientInputManager;
    public DeviceConnectionManager deviceConnectionManager;
    public DoctorChatManager doctorChatManager;

    [Header("Doctor Chat Buttons")]
    public Button closeChatButton;

    public enum Page
    {
        PatientInput,
        DeviceConnection,
        Dashboard,
        DoctorChat
    }

    private Page currentPage = Page.PatientInput;
    private CanvasGroup patientInputGroup;
    private CanvasGroup deviceConnectionGroup;
    private CanvasGroup dashboardGroup;
    private CanvasGroup doctorChatGroup;
    private CanvasGroup bottomNavGroup;

    void Awake()
    {
        UIDebug.Log(nameof(AppUIFlowManager), "Awake - initializing UI flow");
        PlayerPrefs.DeleteKey("SavedPatientId"); //important don't remove
        PlayerPrefs.Save(); //important don't remove

        if (patientInputManager != null)
        {
            patientInputManager.OnPatientIdSubmitted += HandlePatientIdSubmitted;
        }

        if (deviceConnectionManager != null)
        {
            deviceConnectionManager.OnContinueToDashboard += HandleDeviceContinue;
        }

        if (navDashboardButton != null)
        {
            navDashboardButton.onClick.AddListener(() => ShowPage(Page.Dashboard, "NavDashboardButton"));
        }

        if (navDeviceButton != null)
        {
            navDeviceButton.onClick.AddListener(() => ShowPage(Page.DeviceConnection, "NavDeviceButton"));
        }

        if (navDoctorButton != null)
        {
            navDoctorButton.onClick.AddListener(() => StartCoroutine(ShowPageNextFrame(Page.DoctorChat, "NavDoctorButton")));
        }

        if (closeChatButton != null)
        {
            closeChatButton.onClick.AddListener(() => ShowPage(Page.Dashboard, "CloseChatButton"));
        }

        CacheCanvasGroups();

        ShowPage(Page.PatientInput);
    }

    void OnDestroy()
    {
        if (patientInputManager != null)
        {
            patientInputManager.OnPatientIdSubmitted -= HandlePatientIdSubmitted;
        }

        if (deviceConnectionManager != null)
        {
            deviceConnectionManager.OnContinueToDashboard -= HandleDeviceContinue;
        }
    }

    private IEnumerator ShowPageNextFrame(Page page, string source)
    {
        yield return null; // Wait one frame
        ShowPage(page, source);
    }

    void HandlePatientIdSubmitted(string patientId)
    {
        UIDebug.Log(nameof(AppUIFlowManager), $"Patient ID submitted: {patientId}");
        ShowPage(Page.DeviceConnection);
    }

    void HandleDeviceContinue()
    {
        UIDebug.Log(nameof(AppUIFlowManager), "Device continue pressed - switching to Dashboard");
        ShowPage(Page.Dashboard, "DeviceContinue");
    }

    public void ShowPage(Page page, string source = null)
    {
        currentPage = page;
        if (string.IsNullOrEmpty(source))
        {
            UIDebug.Log(nameof(AppUIFlowManager), $"ShowPage -> {page}");
        }
        else
        {
            UIDebug.Log(nameof(AppUIFlowManager), $"ShowPage -> {page} (triggered by {source})");
        }

        // Update page visibility without disabling GameObjects (keeps managers alive)
        SetPageVisibility(patientInputGroup, page == Page.PatientInput);
        SetPageVisibility(deviceConnectionGroup, page == Page.DeviceConnection);
        SetPageVisibility(dashboardGroup, page == Page.Dashboard);
        SetPageVisibility(doctorChatGroup, page == Page.DoctorChat);

        switch (page)
        {
            case Page.PatientInput:
                break;
            case Page.DeviceConnection:
                break;
            case Page.Dashboard:
                break;
            case Page.DoctorChat:
                UIDebug.Log(nameof(AppUIFlowManager), "DoctorChatPage activated");
                break;
        }

        // Update navigation bar visibility
        if (bottomNavGroup != null)
        {
            bool showNav = page == Page.Dashboard || page == Page.DeviceConnection || page == Page.DoctorChat;
            SetPageVisibility(bottomNavGroup, showNav);
        }

        UpdateNavButtonStates();
    }

    void UpdateNavButtonStates()
    {
        SetButtonState(navDashboardButton, currentPage == Page.Dashboard);
        SetButtonState(navDeviceButton, currentPage == Page.DeviceConnection);
        SetButtonState(navDoctorButton, currentPage == Page.DoctorChat);
    }

    void SetButtonState(Button button, bool active)
    {
        if (button == null) return;

        var colors = button.colors;
        colors.colorMultiplier = active ? 0.7f : 1f;
        button.colors = colors;
    }

    void CacheCanvasGroups()
    {
        patientInputGroup = GetCanvasGroup(patientInputPage);
        deviceConnectionGroup = GetCanvasGroup(deviceConnectionPage);
        dashboardGroup = GetCanvasGroup(dashboardPage);
        doctorChatGroup = GetCanvasGroup(doctorChatPage);
        bottomNavGroup = GetCanvasGroup(bottomNavigationBar);
    }

    CanvasGroup GetCanvasGroup(GameObject target)
    {
        if (target == null) return null;
        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = target.AddComponent<CanvasGroup>();
        }
        return group;
    }

    void SetPageVisibility(CanvasGroup group, bool visible)
    {
        if (group == null) return;
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }
}