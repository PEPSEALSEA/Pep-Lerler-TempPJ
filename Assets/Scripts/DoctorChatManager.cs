using Nova;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.UIElements;


public class DoctorChatManager : MonoBehaviour
{
    [Header("Chat Entry Points")]
    public Interactable chatButton;
    public UIBlock chatPanel;
    public Interactable closeChatButton;

    [Header("Doctor Connection UI")]
    public TextField doctorIdInputField;
    public Interactable connectDoctorButton;
    public TextBlock connectionStatusText;
    public UIBlock doctorLoginContainer;

    [Header("Chat UI")]
    public UIBlock chatContainer;
    public TextBlock activeDoctorNameText;
    public TextField messageInputField;
    public Interactable sendMessageButton;
    public TextBlock chatHistoryText;
    public ScrollView chatScrollRect;

    [Header("Input Field Settings")]
    public int maxInputLines = 4;
    public float minInputHeight = 40f;
    public float maxInputHeight = 120f;

    [Header("Dependencies")]
    public ChatAPI chatApi;
    public PatientInputManager patientInputManager;
    public bool useChatApi = true;

    [Header("API Configuration")]
    public string apiUrl = "https://script.google.com/macros/s/AKfycbwOIWUdbLHsMl4Bwgh8TiauBvPQKVOy7XEFXy6grauL8V55qaFS0D4xtKoTtXmJCAnmGw/exec";
    public bool fetchDoctorNameFromApi = true;

    [Header("Chat Settings")]
    [TextArea]
    public string defaultGreeting = "Doctor consult chat connected.";
    public bool simulateDoctorResponses = false;
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
        // Build local lookup table as fallback
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
        EnsureDependencies();

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

        EnsureDependencies();

        if (useChatApi && chatApi != null)
        {
            chatApi.StopAutoRefresh();
        }
    }

    void AttemptDoctorConnection()
    {
        // GET PATIENT ID FIRST
        EnsureDependencies();
        currentPatientId = patientInputManager != null ? patientInputManager.GetCurrentPatientId() : string.Empty;
        if (string.IsNullOrEmpty(currentPatientId))
        {
            UIDebug.Warn(nameof(DoctorChatManager), "Current patient ID missing when connecting to doctor.");
            DisplayStatusMessage("Please enter your Patient ID first.", Color.yellow);
            return;
        }

        string enteredId = doctorIdInputField?.text.Trim() ?? "";
        if (string.IsNullOrEmpty(enteredId))
        {
            DisplayStatusMessage("Please enter a doctor ID.", Color.yellow);
            return;
        }

        currentDoctorId = enteredId.Trim();

        // Disable button while connecting
        if (connectDoctorButton != null) connectDoctorButton.interactable = false;
        DisplayStatusMessage("Connecting to doctor...", Color.yellow);

        // Fetch doctor name from API or use fallback
        if (fetchDoctorNameFromApi)
        {
            StartCoroutine(FetchDoctorNameAndConnect(currentDoctorId));
        }
        else
        {
            // Use local lookup as fallback
            string upperKey = currentDoctorId.ToUpperInvariant();
            if (doctorLookup.TryGetValue(upperKey, out var doctorInfo))
            {
                currentDoctorName = doctorInfo.doctorName;
            }
            else
            {
                currentDoctorName = $"Doctor {currentDoctorId}";
            }

            CompleteDoctorConnection();
        }
    }

    IEnumerator FetchDoctorNameAndConnect(string doctorId)
    {
        string url = $"{apiUrl}?action=getDoctorName&doctorId={UnityWebRequest.EscapeURL(doctorId)}";

        UIDebug.Log(nameof(DoctorChatManager), $"Fetching doctor name from: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Accept", "application/json");
            yield return request.SendWebRequest();

            bool success = false;

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                UIDebug.Log(nameof(DoctorChatManager), $"Doctor name response: {responseText}");

                try
                {
                    // Try to parse the response
                    if (!string.IsNullOrWhiteSpace(responseText))
                    {
                        responseText = responseText.Trim();

                        // Check if it's JSON
                        if (responseText.StartsWith("{"))
                        {
                            DoctorNameResponse response = JsonUtility.FromJson<DoctorNameResponse>(responseText);

                            if (response.status == "success" && !string.IsNullOrWhiteSpace(response.doctorName))
                            {
                                currentDoctorName = response.doctorName.Trim();
                                success = true;
                                UIDebug.Log(nameof(DoctorChatManager), $"✓ Doctor name fetched: {currentDoctorName}");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    UIDebug.Warn(nameof(DoctorChatManager), $"Failed to parse doctor name response: {e.Message}");
                }
            }
            else
            {
                UIDebug.Warn(nameof(DoctorChatManager), $"Failed to fetch doctor name: {request.error}");
            }

            // Fallback if API failed
            if (!success)
            {
                UIDebug.Log(nameof(DoctorChatManager), "Using fallback doctor name lookup");

                string upperKey = currentDoctorId.ToUpperInvariant();
                if (doctorLookup.TryGetValue(upperKey, out var doctorInfo))
                {
                    currentDoctorName = doctorInfo.doctorName;
                }
                else
                {
                    currentDoctorName = $"Doctor {currentDoctorId}";
                }
            }

            CompleteDoctorConnection();
        }
    }

    void CompleteDoctorConnection()
    {
        isDoctorConnected = true;

        // SET CHAT API USER
        EnsureDependencies();
        if (useChatApi && chatApi != null)
        {
            chatApi.currentUserId = currentPatientId;
            chatApi.currentUserType = "patient";
        }

        // UI UPDATE
        if (doctorLoginContainer != null) doctorLoginContainer.SetActive(false);
        if (chatContainer != null) chatContainer.SetActive(true);
        if (sendMessageButton != null) sendMessageButton.interactable = true;
        if (activeDoctorNameText != null) activeDoctorNameText.text = $"Chatting with {currentDoctorName}";

        DisplayStatusMessage($"Connected to {currentDoctorName}.", new Color(0.4f, 1f, 0.6f));
        ClearChatHistory();
        AppendMessage("System", defaultGreeting);

        // FETCH CHAT HISTORY
        if (useChatApi && chatApi != null)
        {
            chatApi.FetchMessages(currentDoctorId, "doctor", OnChatHistoryLoaded);
            chatApi.StartAutoRefresh(currentDoctorId, "doctor");
        }

        // Re-enable connect button
        if (connectDoctorButton != null) connectDoctorButton.interactable = true;
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

        // Clear input immediately
        messageInputField.text = string.Empty;

        EnsureDependencies();
        if (useChatApi && chatApi != null)
        {
            if (string.IsNullOrEmpty(currentPatientId))
            {
                DisplayStatusMessage("Patient ID missing. Cannot send message.", Color.red);
                return;
            }

            if (string.IsNullOrEmpty(currentDoctorId))
            {
                DisplayStatusMessage("Doctor ID missing. Cannot send message.", Color.red);
                return;
            }

            if (sendMessageButton != null) sendMessageButton.interactable = false;
            DisplayStatusMessage("Sending message...", Color.white);

            UIDebug.Log(nameof(DoctorChatManager), $"Sending: sender={currentPatientId}, receiver={currentDoctorId}, message={message}");

            chatApi.SendMessage(currentDoctorId, "doctor", message, (response) =>
            {
                if (sendMessageButton != null) sendMessageButton.interactable = true;

                if (response == null)
                {
                    DisplayStatusMessage("Failed to send: No response", Color.red);
                    UIDebug.Error(nameof(DoctorChatManager), "Chat API returned null response");
                    return;
                }

                if (response.status != "success")
                {
                    string errorMessage = response.message ?? "Unknown error";
                    DisplayStatusMessage($"Failed to send: {errorMessage}", Color.red);
                    UIDebug.Error(nameof(DoctorChatManager), $"Chat API send failed: {errorMessage}");
                    return;
                }

                UIDebug.Log(nameof(DoctorChatManager), $"Message sent successfully via API: {message}");
                AppendMessage("You", message, response.timestamp);
                DisplayStatusMessage("Message sent.", new Color(0.6f, 0.95f, 0.6f));
            });
        }
        else
        {
            UIDebug.Log(nameof(DoctorChatManager), $"Message sent (local): {message}");
            AppendMessage("You", message);

            if (simulateDoctorResponses)
            {
                StartCoroutine(SimulateDoctorReply());
            }
        }
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
        if (!string.IsNullOrEmpty(timestampIso))
        {
            try
            {
                displayTime = DateTime.Parse(timestampIso).ToLocalTime();
            }
            catch (Exception e)
            {
                UIDebug.Warn(nameof(DoctorChatManager), $"Failed to parse timestamp: {timestampIso} - {e.Message}");
            }
        }

        string formattedMessage = $"{displayTime:HH:mm} - {speaker}: {message}";
        chatHistoryBuilder.AppendLine(formattedMessage);

        if (chatHistoryText != null)
        {
            chatHistoryText.text = chatHistoryBuilder.ToString();
            UIDebug.Log(nameof(DoctorChatManager), $"Appended message to chat: {formattedMessage}");
        }

        // Force scroll to bottom
        if (chatScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 0f;
            Canvas.ForceUpdateCanvases();
        }
    }

    void ClearChatHistory()
    {
        chatHistoryBuilder.Clear();
        if (chatHistoryText != null)
        {
            chatHistoryText.text = string.Empty;
        }
        UIDebug.Log(nameof(DoctorChatManager), "Chat history cleared");
    }

    void ResetChatState()
    {
        isDoctorConnected = false;
        currentDoctorId = string.Empty;
        currentDoctorName = string.Empty;

        EnsureDependencies();
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
            UIDebug.Error(nameof(DoctorChatManager), "OnChatHistoryLoaded: response is null");
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
            UIDebug.Log(nameof(DoctorChatManager), $"Loading {response.messages.Length} chat messages");

            foreach (var msg in response.messages)
            {
                if (msg == null) continue;

                bool isUserMessage = chatApi != null && string.Equals(msg.senderId, chatApi.currentUserId, StringComparison.OrdinalIgnoreCase);
                string speaker = isUserMessage ? "You" : currentDoctorName;
                AppendMessage(speaker, msg.message, msg.timestamp);
            }
        }
        else
        {
            UIDebug.Log(nameof(DoctorChatManager), "No previous messages found");
            AppendMessage("System", "No previous messages found.");
        }

        DisplayStatusMessage($"Connected to {currentDoctorName}.", new Color(0.4f, 1f, 0.6f));
    }

    void OnNewChatMessage(ChatMessage message)
    {
        EnsureDependencies();
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
        UIDebug.Log(nameof(DoctorChatManager), $"New message received: {speaker}: {message.message}");
    }

    public void SetCurrentPatientId(string patientId)
    {
        if (string.IsNullOrWhiteSpace(patientId)) return;
        EnsureDependencies();
        currentPatientId = patientId.Trim();
        UIDebug.Log(nameof(DoctorChatManager), $"SetCurrentPatientId -> {currentPatientId}");

        if (chatApi != null)
        {
            chatApi.currentUserId = currentPatientId;
            chatApi.currentUserType = "patient";
        }
    }

    void EnsureDependencies()
    {
        if (chatApi == null)
        {
            chatApi = FindObjectOfType<ChatAPI>();
        }
        if (patientInputManager == null)
        {
            patientInputManager = FindObjectOfType<PatientInputManager>();
        }
    }
}

// Data structure for doctor name API response
[Serializable]
public class DoctorNameResponse
{
    public string status;
    public string message;
    public string doctorName;
    public string doctorId;
}