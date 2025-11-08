using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DoctorChatManager : MonoBehaviour
{
    [Header("Chat Entry Points")]
    public Button chatButton;
    public GameObject chatPanel;
    public Button closeChatButton;

    [Header("Doctor Connection UI")]
    public TMP_InputField doctorIdInputField;
    public Button connectDoctorButton;
    public TextMeshProUGUI connectionStatusText;
    public GameObject doctorLoginContainer;

    [Header("Chat UI")]
    public GameObject chatContainer;
    public TextMeshProUGUI activeDoctorNameText;
    public TMP_InputField messageInputField;
    public Button sendMessageButton;
    public TextMeshProUGUI chatHistoryText;
    public ScrollRect chatScrollRect;

    [Header("Chat API")]
    public ChatAPI chatApi;
    public bool useChatApi = true;

    [Header("Chat Settings")]
    [TextArea]
    public string defaultGreeting = "Doctor consult chat connected.";
    public bool simulateDoctorResponses = true;
    public float simulatedResponseDelay = 1.5f;

    [Serializable]
    public class DoctorEntry
    {
        public string doctorId;
        public string doctorName;
    }

    public List<DoctorEntry> doctorDirectory = new List<DoctorEntry>
    {
        new DoctorEntry { doctorId = "DOC001", doctorName = "Dr. Alice Johnson" },
        new DoctorEntry { doctorId = "DOC002", doctorName = "Dr. Brian Smith" },
        new DoctorEntry { doctorId = "DOC003", doctorName = "Dr. Carol Lee" }
    };

    private readonly Dictionary<string, DoctorEntry> doctorLookup = new Dictionary<string, DoctorEntry>();
    private readonly StringBuilder chatHistoryBuilder = new StringBuilder();

    private bool isDoctorConnected = false;
    private string currentDoctorId = string.Empty;
    private string currentDoctorName = string.Empty;
    private string currentPatientId = string.Empty;

    void Awake()
    {
        doctorLookup.Clear();
        foreach (var entry in doctorDirectory)
        {
            if (entry == null) continue;
            if (string.IsNullOrWhiteSpace(entry.doctorId)) continue;

            string key = entry.doctorId.Trim().ToUpperInvariant();
            if (!doctorLookup.ContainsKey(key))
            {
                doctorLookup.Add(key, entry);
            }
        }
    }

    void Start()
    {
        if (chatPanel != null)
        {
            chatPanel.SetActive(false);
        }

        if (chatContainer != null)
        {
            chatContainer.SetActive(false);
        }

        if (sendMessageButton != null)
        {
            sendMessageButton.interactable = false;
        }

        AssignButtonListeners();
        ClearChatHistory();
    }

    void AssignButtonListeners()
    {
        if (chatButton != null)
        {
            chatButton.onClick.AddListener(ShowChatPanel);
            UIDebug.Log(nameof(DoctorChatManager), "Chat open button listener assigned");
        }

        if (closeChatButton != null)
        {
            closeChatButton.onClick.AddListener(HideChatPanel);
        }

        if (connectDoctorButton != null)
        {
            connectDoctorButton.onClick.AddListener(AttemptDoctorConnection);
        }

        if (sendMessageButton != null)
        {
            sendMessageButton.onClick.AddListener(SendMessage);
        }
    }

    public void ShowChatPanel()
    {
        UIDebug.Log(nameof(DoctorChatManager), "ShowChatPanel");
        if (chatPanel != null)
        {
            chatPanel.SetActive(true);
        }

        if (doctorLoginContainer != null)
        {
            doctorLoginContainer.SetActive(true);
        }

        if (chatContainer != null)
        {
            chatContainer.SetActive(false);
        }

        if (connectionStatusText != null)
        {
            connectionStatusText.text = "Enter a doctor ID to start chat.";
            connectionStatusText.color = Color.white;
        }

        ClearChatHistory();
        ResetChatState();
    }

    public void HideChatPanel()
    {
        UIDebug.Log(nameof(DoctorChatManager), "HideChatPanel");
        if (chatPanel != null)
        {
            chatPanel.SetActive(false);
        }

        if (useChatApi && chatApi != null)
        {
            chatApi.StopAutoRefresh();
        }
    }

    void AttemptDoctorConnection()
    {
        string enteredId = doctorIdInputField != null ? doctorIdInputField.text.Trim().ToUpperInvariant() : string.Empty;

        UIDebug.Log(nameof(DoctorChatManager), $"AttemptDoctorConnection | ID = '{enteredId}'");

        if (string.IsNullOrEmpty(enteredId))
        {
            DisplayStatusMessage("Please enter a doctor ID.", Color.yellow);
            return;
        }

        if (!doctorLookup.TryGetValue(enteredId, out var doctorInfo))
        {
            doctorInfo = new DoctorEntry { doctorId = enteredId, doctorName = $"Doctor {enteredId}" };
        }

        currentDoctorId = doctorInfo.doctorId.Trim();
        currentDoctorName = string.IsNullOrWhiteSpace(doctorInfo.doctorName) ? $"Doctor {currentDoctorId}" : doctorInfo.doctorName;
        isDoctorConnected = true;
            UIDebug.Log(nameof(DoctorChatManager), $"Doctor connected | {currentDoctorName} ({currentDoctorId})");

        if (doctorLoginContainer != null)
        {
            doctorLoginContainer.SetActive(false);
        }

        if (chatContainer != null)
        {
            chatContainer.SetActive(true);
        }

        if (sendMessageButton != null)
        {
            sendMessageButton.interactable = true;
        }

        if (activeDoctorNameText != null)
        {
            activeDoctorNameText.text = $"Chatting with {currentDoctorName}";
        }

        DisplayStatusMessage($"Connected to {currentDoctorName} ({currentDoctorId}).", new Color(0.4f, 1f, 0.6f));

        ClearChatHistory();
        AppendMessage("System", defaultGreeting);

        if (useChatApi && chatApi != null)
        {
            if (string.IsNullOrEmpty(currentPatientId))
            {
                UIDebug.Warn(nameof(DoctorChatManager), "Current patient ID missing when connecting to doctor.");
                DisplayStatusMessage("Patient ID not set. Please enter patient ID first.", Color.yellow);
            }
            else
            {
                chatApi.currentUserId = currentPatientId;
                chatApi.currentUserType = "patient";
                DisplayStatusMessage("Fetching chat history...", Color.white);
                if (connectDoctorButton != null) connectDoctorButton.interactable = false;
                chatApi.FetchMessages(currentDoctorId, "doctor", OnChatHistoryLoaded);
                chatApi.StartAutoRefresh(currentDoctorId, "doctor");
            }
        }
    }

    void DisplayStatusMessage(string message, Color color)
    {
        if (connectionStatusText != null)
        {
            connectionStatusText.text = message;
            connectionStatusText.color = color;
        }
    }

    void SendMessage()
    {
        if (!isDoctorConnected)
        {
            DisplayStatusMessage("Please connect to a doctor first.", Color.yellow);
            UIDebug.Warn(nameof(DoctorChatManager), "SendMessage attempted without doctor connection");
            return;
        }

        if (messageInputField == null) return;

        string message = messageInputField.text.Trim();
        if (string.IsNullOrEmpty(message))
        {
            UIDebug.Warn(nameof(DoctorChatManager), "SendMessage skipped - empty message");
            return;
        }

        if (useChatApi && chatApi != null)
        {
            if (string.IsNullOrEmpty(currentPatientId))
            {
                DisplayStatusMessage("Patient ID missing. Cannot send message.", Color.red);
                return;
            }

            sendMessageButton.interactable = false;
            DisplayStatusMessage("Sending message...", Color.white);

            chatApi.SendMessage(currentDoctorId, "doctor", message, (response) =>
            {
                sendMessageButton.interactable = true;
                if (response == null || response.status != "success")
                {
                    string errorMessage = response != null ? response.message : "Unknown error";
                    DisplayStatusMessage($"Failed to send: {errorMessage}", Color.red);
                    UIDebug.Error(nameof(DoctorChatManager), $"Chat API send failed: {errorMessage}");
                    return;
                }

                UIDebug.Log(nameof(DoctorChatManager), $"Message sent via API: {message}");
                AppendMessage("You", message, response.timestamp);
                DisplayStatusMessage("Message sent.", new Color(0.6f, 0.95f, 0.6f));
            });
        }
        else
        {
            UIDebug.Log(nameof(DoctorChatManager), $"Message sent: {message}");
            AppendMessage("You", message);

            if (simulateDoctorResponses)
            {
                StartCoroutine(SimulateDoctorReply());
            }
        }

        messageInputField.text = string.Empty;
    }

    System.Collections.IEnumerator SimulateDoctorReply()
    {
        yield return new WaitForSeconds(simulatedResponseDelay);

        if (!isDoctorConnected) yield break;

        string reply = $"Thank you for the update. I'll review the latest metrics soon.";
        UIDebug.Log(nameof(DoctorChatManager), "Simulated doctor reply");
        AppendMessage(currentDoctorName, reply);
    }

    void AppendMessage(string speaker, string message, string timestampIso = null)
    {
        DateTime displayTime = DateTime.Now;
        if (!string.IsNullOrEmpty(timestampIso) && DateTime.TryParse(timestampIso, out DateTime parsed))
        {
            displayTime = parsed.ToLocalTime();
        }
        chatHistoryBuilder.AppendLine($"{displayTime:HH:mm} - {speaker}: {message}");

        if (chatHistoryText != null)
        {
            chatHistoryText.text = chatHistoryBuilder.ToString();
        }

        if (chatScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    void ClearChatHistory()
    {
        chatHistoryBuilder.Clear();
        if (chatHistoryText != null)
        {
            chatHistoryText.text = string.Empty;
        }
    }

    void ResetChatState()
    {
        isDoctorConnected = false;
        currentDoctorId = string.Empty;
        currentDoctorName = string.Empty;

        if (useChatApi && chatApi != null)
        {
            chatApi.StopAutoRefresh();
        }

        if (sendMessageButton != null)
        {
            sendMessageButton.interactable = false;
        }

        if (doctorIdInputField != null)
        {
            doctorIdInputField.text = string.Empty;
        }

        if (messageInputField != null)
        {
            messageInputField.text = string.Empty;
        }

        if (doctorLoginContainer != null)
        {
            doctorLoginContainer.SetActive(true);
        }

        if (chatContainer != null)
        {
            chatContainer.SetActive(false);
        }
    }

    void OnChatHistoryLoaded(ChatMessagesResponse response)
    {
        if (connectDoctorButton != null)
        {
            connectDoctorButton.interactable = true;
        }

        if (response == null)
        {
            DisplayStatusMessage("No response from chat service.", Color.red);
            return;
        }

        if (response.status != "success")
        {
            DisplayStatusMessage($"Failed to load chat: {response.message}", Color.red);
            UIDebug.Error(nameof(DoctorChatManager), $"Chat history load failed: {response.message}");
            return;
        }

        ClearChatHistory();

        if (response.messages != null && response.messages.Length > 0)
        {
            foreach (var msg in response.messages)
            {
                bool isUserMessage = chatApi != null && string.Equals(msg.senderId, chatApi.currentUserId, StringComparison.OrdinalIgnoreCase);
                string speaker = isUserMessage ? "You" : currentDoctorName;
                AppendMessage(speaker, msg.message, msg.timestamp);
            }
        }
        else
        {
            AppendMessage("System", "No previous messages found.");
        }

        DisplayStatusMessage($"Connected to {currentDoctorName}.", new Color(0.4f, 1f, 0.6f));
    }

    void OnNewChatMessage(ChatMessage message)
    {
        if (!isDoctorConnected || chatApi == null || message == null) return;

        bool isSameConversation =
            (string.Equals(message.senderId, currentDoctorId, StringComparison.OrdinalIgnoreCase) &&
             string.Equals(message.receiverId, currentPatientId, StringComparison.OrdinalIgnoreCase)) ||
            (string.Equals(message.receiverId, currentDoctorId, StringComparison.OrdinalIgnoreCase) &&
             string.Equals(message.senderId, currentPatientId, StringComparison.OrdinalIgnoreCase));

        if (!isSameConversation) return;

        bool isUserMessage = string.Equals(message.senderId, chatApi.currentUserId, StringComparison.OrdinalIgnoreCase);
        string speaker = isUserMessage ? "You" : currentDoctorName;
        AppendMessage(speaker, message.message, message.timestamp);
    }

    public void SetCurrentPatientId(string patientId)
    {
        if (string.IsNullOrWhiteSpace(patientId)) return;
        currentPatientId = patientId.Trim();
        UIDebug.Log(nameof(DoctorChatManager), $"SetCurrentPatientId -> {currentPatientId}");

        if (chatApi != null)
        {
            chatApi.currentUserId = currentPatientId;
            chatApi.currentUserType = "patient";
        }
    }
}
