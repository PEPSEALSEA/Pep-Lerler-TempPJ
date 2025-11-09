using UnityEngine;
using TMPro;
using Nova;

public class PatientInputManager : MonoBehaviour
{
    [Header("UI Components")]
    public UIBlock patientInputPanel;
    public TMP_InputField patientIdInput;
    public Interactable submitButton;
    public Interactable skipButton;
    public TextBlock savedPatientIdText;

    [Header("Settings")]
    public string savedPatientIdKey = "SavedPatientId";

    [Header("Dependencies")]
    public PatientDataAPI patientDataAPI;
    public ChatAPI chatApi;
    public DoctorChatManager doctorChatManager;

    private string currentPatientId = "";
    public System.Action<string> OnPatientIdSubmitted;

    void Start()
    {
        LoadSavedPatientId();
        if (patientInputPanel != null)
            patientInputPanel.gameObject.SetActive(string.IsNullOrEmpty(currentPatientId));
        UpdateSavedIdDisplay();
        AssignButtonListeners();
    }

    void AssignButtonListeners()
    {
        if (submitButton?.UIBlock != null)
            submitButton.UIBlock.AddGestureHandler<Gesture.OnClick>(evt => OnSubmitClicked());
        if (skipButton?.UIBlock != null)
            skipButton.UIBlock.AddGestureHandler<Gesture.OnClick>(evt => OnSkipClicked());
    }

    void LoadSavedPatientId()
    {
        if (PlayerPrefs.HasKey(savedPatientIdKey))
        {
            currentPatientId = PlayerPrefs.GetString(savedPatientIdKey);
            if (patientIdInput != null) patientIdInput.text = currentPatientId;
        }
    }

    void SavePatientId(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        currentPatientId = id;
        PlayerPrefs.SetString(savedPatientIdKey, id);
        PlayerPrefs.Save();
        EnsureDependencies();
        patientDataAPI.testPatientId = id;
        if (chatApi != null)
        {
            chatApi.currentUserId = id;
            chatApi.currentUserType = "patient";
        }
        UpdateSavedIdDisplay();
    }

    void UpdateSavedIdDisplay()
    {
        if (savedPatientIdText != null)
            savedPatientIdText.Text = !string.IsNullOrEmpty(currentPatientId)
                ? $"Saved Patient ID: {currentPatientId}"
                : "No saved Patient ID";
    }

    void OnSubmitClicked()
    {
        string id = patientIdInput?.text.Trim() ?? "";
        if (string.IsNullOrEmpty(id)) return;
        SavePatientId(id);
        if (patientInputPanel != null) patientInputPanel.gameObject.SetActive(false);
        OnPatientIdSubmitted?.Invoke(id);
        doctorChatManager?.SetCurrentPatientId(id);
    }

    void OnSkipClicked()
    {
        if (patientInputPanel != null) patientInputPanel.gameObject.SetActive(false);
        string id = !string.IsNullOrEmpty(currentPatientId) ? currentPatientId : "DEFAULT_PATIENT";
        EnsureDependencies();
        if (chatApi != null)
        {
            chatApi.currentUserId = id;
            chatApi.currentUserType = "patient";
        }
        OnPatientIdSubmitted?.Invoke(id);
        doctorChatManager?.SetCurrentPatientId(id);
    }

    public string GetCurrentPatientId() => currentPatientId;

    public void ShowInputPanel()
    {
        if (patientInputPanel != null)
            patientInputPanel.gameObject.SetActive(true);
    }

    void EnsureDependencies()
    {
        if (patientDataAPI == null) patientDataAPI = FindObjectOfType<PatientDataAPI>();
        if (chatApi == null) chatApi = FindObjectOfType<ChatAPI>();
        if (doctorChatManager == null) doctorChatManager = FindObjectOfType<DoctorChatManager>();
    }
}