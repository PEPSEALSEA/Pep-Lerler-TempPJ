using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PatientInputManager : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject patientInputPanel;
    public TMP_InputField patientIdInput;
    public Button submitButton;
    public Button skipButton;
    public TextMeshProUGUI savedPatientIdText;

    [Header("Settings")]
    public string savedPatientIdKey = "SavedPatientId";

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
            PatientDataAPI.Instance.testPatientId = currentPatientId;
            ChatAPI.Instance.currentUserId = currentPatientId;
            ChatAPI.Instance.currentUserType = "patient"; // ADD THIS LINE
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

        DoctorChatManager.Instance?.SetCurrentPatientId(currentPatientId);
    }

    void OnSkipClicked()
    {
        UIDebug.Log(nameof(PatientInputManager), "Skip clicked");

        if (patientInputPanel != null)
        {
            patientInputPanel.SetActive(false);
        }

        string idToUse = !string.IsNullOrEmpty(currentPatientId) ? currentPatientId : "DEFAULT_PATIENT";
        ChatAPI.Instance.currentUserId = idToUse;
        ChatAPI.Instance.currentUserType = "patient";
        OnPatientIdSubmitted?.Invoke(idToUse);

        DoctorChatManager.Instance?.SetCurrentPatientId(idToUse);
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
}
