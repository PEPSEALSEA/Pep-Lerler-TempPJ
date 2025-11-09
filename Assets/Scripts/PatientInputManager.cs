using Nova;
using UnityEngine;
using UnityEngine.UI;

public class PatientInputManager : MonoBehaviour
{
    [Header("UI Components")]
    public UIBlock patientInputPanel;
    public InputField patientIdInput;
    public UIBlock submitBlock;
    public UIBlock skipBlock;
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
        UIDebug.Log(nameof(PatientInputManager), $"Start | Saved ID = '{currentPatientId}'");

        if (submitBlock != null)
            submitBlock.AddGestureHandler<Gesture.OnClick>(OnSubmitClicked);
        if (skipBlock != null)
            skipBlock.AddGestureHandler<Gesture.OnClick>(OnSkipClicked);

        if (patientInputPanel != null)
            patientInputPanel.gameObject.SetActive(string.IsNullOrEmpty(currentPatientId));

        UpdateSavedIdDisplay();
    }

    void OnDestroy()
    {
        if (submitBlock != null)
            submitBlock.RemoveGestureHandler<Gesture.OnClick>(OnSubmitClicked);
        if (skipBlock != null)
            skipBlock.RemoveGestureHandler<Gesture.OnClick>(OnSkipClicked);
    }

    void LoadSavedPatientId()
    {
        if (PlayerPrefs.HasKey(savedPatientIdKey))
        {
            currentPatientId = PlayerPrefs.GetString(savedPatientIdKey);
            if (patientIdInput != null && !string.IsNullOrEmpty(currentPatientId))
                patientIdInput.text = currentPatientId;
        }
    }

    void SavePatientId(string patientId)
    {
        if (!string.IsNullOrEmpty(patientId))
        {
            currentPatientId = patientId;
            PlayerPrefs.SetString(savedPatientIdKey, patientId);
            PlayerPrefs.Save();
            EnsureDependencies();
            if (patientDataAPI != null)
                patientDataAPI.testPatientId = currentPatientId;
            if (chatApi != null)
            {
                chatApi.currentUserId = currentPatientId;
                chatApi.currentUserType = "patient";
            }
            UpdateSavedIdDisplay();
        }
    }

    void UpdateSavedIdDisplay()
    {
        if (savedPatientIdText != null)
        {
            savedPatientIdText.Text = !string.IsNullOrEmpty(currentPatientId)
                ? $"Saved Patient ID: {currentPatientId}"
                : "No saved Patient ID";
        }
    }

    void OnSubmitClicked(Gesture.OnClick evt)
    {
        string inputId = patientIdInput != null ? patientIdInput.text : "";
        UIDebug.Log(nameof(PatientInputManager), $"Submit clicked | Input = '{inputId}'");
        if (string.IsNullOrEmpty(inputId))
        {
            Debug.LogWarning("Please enter a Patient ID");
            return;
        }
        SavePatientId(inputId);
        if (patientInputPanel != null)
            patientInputPanel.gameObject.SetActive(false);
        OnPatientIdSubmitted?.Invoke(currentPatientId);
        EnsureDependencies();
        if (doctorChatManager != null)
            doctorChatManager.SetCurrentPatientId(currentPatientId);
    }

    void OnSkipClicked(Gesture.OnClick evt)
    {
        UIDebug.Log(nameof(PatientInputManager), "Skip clicked");
        if (patientInputPanel != null)
            patientInputPanel.gameObject.SetActive(false);
        string idToUse = !string.IsNullOrEmpty(currentPatientId) ? currentPatientId : "DEFAULT_PATIENT";
        EnsureDependencies();
        if (chatApi != null)
        {
            chatApi.currentUserId = idToUse;
            chatApi.currentUserType = "patient";
        }
        OnPatientIdSubmitted?.Invoke(idToUse);
        if (doctorChatManager != null)
            doctorChatManager.SetCurrentPatientId(idToUse);
    }

    public string GetCurrentPatientId()
    {
        return currentPatientId;
    }

    public void ShowInputPanel()
    {
        if (patientInputPanel != null)
        {
            patientInputPanel.gameObject.SetActive(true);
            UIDebug.Log(nameof(PatientInputManager), "Patient input panel shown");
        }
    }

    void EnsureDependencies()
    {
        if (patientDataAPI == null)
            patientDataAPI = FindObjectOfType<PatientDataAPI>();
        if (chatApi == null)
            chatApi = FindObjectOfType<ChatAPI>();
        if (doctorChatManager == null)
            doctorChatManager = FindObjectOfType<DoctorChatManager>();
    }
}