using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Nova;

public class AppUIFlowManager : MonoBehaviour
{
    [Header("Page Roots")]
    public UIBlock patientInputPage;
    public UIBlock deviceConnectionPage;
    public UIBlock dashboardPage;
    public UIBlock doctorChatPage;
    public UIBlock bottomNavigationBar;

    [Header("Navigation Buttons")]
    public Interactable navDashboardButton;
    public Interactable navDeviceButton;
    public Interactable navDoctorButton;

    [Header("Managers")]
    public PatientInputManager patientInputManager;
    public DeviceConnectionManager deviceConnectionManager;
    public DoctorChatManager doctorChatManager;

    [Header("Doctor Chat Buttons")]
    public Interactable closeChatButton;

    public enum Page
    {
        PatientInput,
        DeviceConnection,
        Dashboard,
        DoctorChat
    }

    private Page currentPage = Page.PatientInput;

    void Awake()
    {
        if (patientInputManager != null)
            patientInputManager.OnPatientIdSubmitted += HandlePatientIdSubmitted;

        if (deviceConnectionManager != null)
            deviceConnectionManager.OnContinueToDashboard += HandleDeviceContinue;

        if (navDashboardButton?.UIBlock != null)
            navDashboardButton.UIBlock.AddGestureHandler<Gesture.OnClick>(evt => ShowPage(Page.Dashboard, "NavDashboard"));

        if (navDeviceButton?.UIBlock != null)
            navDeviceButton.UIBlock.AddGestureHandler<Gesture.OnClick>(evt => ShowPage(Page.DeviceConnection, "NavDevice"));

        if (navDoctorButton?.UIBlock != null)
            navDoctorButton.UIBlock.AddGestureHandler<Gesture.OnClick>(evt => StartCoroutine(ShowPageNextFrame(Page.DoctorChat, "NavDoctor")));

        if (closeChatButton?.UIBlock != null)
            closeChatButton.UIBlock.AddGestureHandler<Gesture.OnClick>(evt => ShowPage(Page.Dashboard, "CloseChat"));

        ShowPage(Page.PatientInput);
    }

    void OnDestroy()
    {
        if (patientInputManager != null)
            patientInputManager.OnPatientIdSubmitted -= HandlePatientIdSubmitted;

        if (deviceConnectionManager != null)
            deviceConnectionManager.OnContinueToDashboard -= HandleDeviceContinue;
    }

    private IEnumerator ShowPageNextFrame(Page page, string source)
    {
        yield return null;
        ShowPage(page, source);
    }

    void HandlePatientIdSubmitted(string patientId)
    {
        ShowPage(Page.DeviceConnection);
    }

    void HandleDeviceContinue()
    {
        ShowPage(Page.Dashboard, "DeviceContinue");
    }

    public void ShowPage(Page page, string source = null)
    {
        currentPage = page;

        SetPageVisibility(patientInputPage, page == Page.PatientInput);
        SetPageVisibility(deviceConnectionPage, page == Page.DeviceConnection);
        SetPageVisibility(dashboardPage, page == Page.Dashboard);
        SetPageVisibility(doctorChatPage, page == Page.DoctorChat);

        if (bottomNavigationBar != null)
        {
            bool showNav = page == Page.Dashboard || page == Page.DeviceConnection || page == Page.DoctorChat;
            SetPageVisibility(bottomNavigationBar, showNav);
        }

        UpdateNavButtonStates();
    }

    void UpdateNavButtonStates()
    {
        SetButtonState(navDashboardButton, currentPage == Page.Dashboard);
        SetButtonState(navDeviceButton, currentPage == Page.DeviceConnection);
        SetButtonState(navDoctorButton, currentPage == Page.DoctorChat);
    }

    void SetButtonState(Interactable button, bool active)
    {
        if (button?.UIBlock != null)
            button.UIBlock.Color = active ? new Color(1f, 1f, 1f, 0.5f) : Color.white;
    }

    void SetPageVisibility(UIBlock block, bool visible)
    {
        if (block != null)
            block.gameObject.SetActive(visible);
    }
}