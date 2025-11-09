using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nova;


public class PatientInputManager : MonoBehaviour
{
    [Header("UI Components")]
    public UIBlock patientInputPanel;
    public TMP_InputField patientIdInput;
    public Button submitButton;
    public Button skipButton;
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
        // Load saved patient ID
        LoadSavedPatientId();
        UIDebug.Log(nameof(PatientInputManager), $"Start | Saved ID = '{currentPatientId}'");

        // Setup button listeners
        if (submitButton != null)
        {
            submitButton.onClick.AddListener(OnSubmitClicked);
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipClicked);
        }

        // Show input panel if no saved ID
        if (patientInputPanel != null)
        {
            patientInputPanel.SetActive(string.IsNullOrEmpty(currentPatientId));
        }

        // Update saved ID display
        UpdateSavedIdDisplay();
    }

    void LoadSavedPatientId()
    {
        if (PlayerPrefs.HasKey(savedPatientIdKey))
        {
            currentPatientId = PlayerPrefs.GetString(savedPatientIdKey);
            if (patientIdInput != null && !string.IsNullOrEmpty(currentPatientId))
            {
                patientIdInput.text = currentPatientId;
            }
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
            {
                patientDataAPI.testPatientId = currentPatientId;
            }
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
            if (!string.IsNullOrEmpty(currentPatientId))
            {
                savedPatientIdText.text = $"Saved Patient ID: {currentPatientId}";
            }
            else
            {
                savedPatientIdText.text = "No saved Patient ID";
            }
        }
    }

    void OnSubmitClicked()
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
        {
            patientInputPanel.SetActive(false);
        }

        OnPatientIdSubmitted?.Invoke(currentPatientId);

        EnsureDependencies();
        if (doctorChatManager != null)
        {
            doctorChatManager.SetCurrentPatientId(currentPatientId);
        }
    }

    void OnSkipClicked()
    {
        UIDebug.Log(nameof(PatientInputManager), "Skip clicked");

        if (patientInputPanel != null)
        {
            patientInputPanel.SetActive(false);
        }

        string idToUse = !string.IsNullOrEmpty(currentPatientId) ? currentPatientId : "DEFAULT_PATIENT";
        EnsureDependencies();
        if (chatApi != null)
        {
            chatApi.currentUserId = idToUse;
            chatApi.currentUserType = "patient";
        }
        OnPatientIdSubmitted?.Invoke(idToUse);

        if (doctorChatManager != null)
        {
            doctorChatManager.SetCurrentPatientId(idToUse);
        }
    }

    public string GetCurrentPatientId()
    {
        return currentPatientId;
    }

    public void ShowInputPanel()
    {
        if (patientInputPanel != null)
        {
            patientInputPanel.SetActive(true);
            UIDebug.Log(nameof(PatientInputManager), "Patient input panel shown");
        }
    }

    void EnsureDependencies()
    {
        if (patientDataAPI == null)
        {
            patientDataAPI = FindObjectOfType<PatientDataAPI>();
        }
        if (chatApi == null)
        {
            chatApi = FindObjectOfType<ChatAPI>();
        }
        if (doctorChatManager == null)
        {
            doctorChatManager = FindObjectOfType<DoctorChatManager>();
        }
    }
}
