using Nova;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class DoctorChatManager : MonoBehaviour
{
    [Header("Chat Entry Points")]
    public Interactable chatButton;
    public UIBlock chatPanel;
    public Interactable closeChatButton;

    [Header("Doctor Connection UI")]
    public TMP_InputField doctorIdInputField;
    public Interactable connectDoctorButton;
    public TextBlock connectionStatusText;
    public UIBlock doctorLoginContainer;

    [Header("Chat UI")]
    public UIBlock chatContainer;
    public TextBlock activeDoctorNameText;
    public TMP_InputField messageInputField;
    public Interactable sendMessageButton;
    public TextBlock chatHistoryText;
    public ScrollRect chatScrollRect;

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
    [TextArea] public string defaultGreeting = "Doctor consult chat connected.";
    public bool simulateDoctorResponses = false;
    public float simulatedResponseDelay = 1.5f;

    [Serializable] public class DoctorEntry { public string doctorId; public string doctorName; }
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
            if (entry == null || string.IsNullOrWhiteSpace(entry.doctorId)) continue;
            string key = entry.doctorId.Trim().ToUpperInvariant();
            if (!doctorLookup.ContainsKey(key)) doctorLookup.Add(key, entry);
        }
    }

    void Start()
    {
        EnsureDependencies();
        if (chatPanel != null) chatPanel.gameObject.SetActive(false);
        if (chatContainer != null) chatContainer.gameObject.SetActive(false);
        if (sendMessageButton != null) sendMessageButton.enabled = false;
        AssignButtonListeners();
        ClearChatHistory();
    }

    void AssignButtonListeners()
    {
        if (chatButton?.UIBlock != null)
            chatButton.UIBlock.AddGestureHandler<Gesture.OnClick>(evt => ShowChatPanel());
        if (closeChatButton?.UIBlock != null)
            closeChatButton.UIBlock.AddGestureHandler<Gesture.OnClick>(evt => HideChatPanel());
        if (connectDoctorButton?.UIBlock != null)
            connectDoctorButton.UIBlock.AddGestureHandler<Gesture.OnClick>(evt => AttemptDoctorConnection());
        if (sendMessageButton?.UIBlock != null)
            sendMessageButton.UIBlock.AddGestureHandler<Gesture.OnClick>(evt => SendMessage());
    }

    public void ShowChatPanel()
    {
        if (chatPanel != null) chatPanel.gameObject.SetActive(true);
        if (doctorLoginContainer != null) doctorLoginContainer.gameObject.SetActive(true);
        if (chatContainer != null) chatContainer.gameObject.SetActive(false);
        if (connectionStatusText != null)
        {
            connectionStatusText.Text = "Enter a doctor ID to start chat.";
            connectionStatusText.TMP.color = Color.white;
        }
        ClearChatHistory();
        ResetChatState();
    }

    public void HideChatPanel()
    {
        if (chatPanel != null) chatPanel.gameObject.SetActive(false);
        if (useChatApi && chatApi != null) chatApi.StopAutoRefresh();
    }

    void AttemptDoctorConnection()
    {
        currentPatientId = patientInputManager?.GetCurrentPatientId() ?? string.Empty;
        if (string.IsNullOrEmpty(currentPatientId))
        {
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
        if (connectDoctorButton != null) connectDoctorButton.enabled = false;
        DisplayStatusMessage("Connecting to doctor...", Color.yellow);

        if (fetchDoctorNameFromApi)
            StartCoroutine(FetchDoctorNameAndConnect(currentDoctorId));
        else
            UseFallbackDoctorName();
    }

    IEnumerator FetchDoctorNameAndConnect(string doctorId)
    {
        string url = $"{apiUrl}?action=getDoctorName&doctorId={UnityWebRequest.EscapeURL(doctorId)}";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Accept", "application/json");
            yield return request.SendWebRequest();
            bool success = false;
            if (request.result == UnityWebRequest.Result.Success)
            {
                string text = request.downloadHandler.text.Trim();
                if (text.StartsWith("{"))
                {
                    var response = JsonUtility.FromJson<DoctorNameResponse>(text);
                    if (response.status == "success" && !string.IsNullOrWhiteSpace(response.doctorName))
                    {
                        currentDoctorName = response.doctorName.Trim();
                        success = true;
                    }
                }
            }
            if (!success) UseFallbackDoctorName();
            else CompleteDoctorConnection();
        }
    }

    void UseFallbackDoctorName()
    {
        string key = currentDoctorId.ToUpperInvariant();
        currentDoctorName = doctorLookup.TryGetValue(key, out var entry) ? entry.doctorName : $"Doctor {currentDoctorId}";
        CompleteDoctorConnection();
    }

    void CompleteDoctorConnection()
    {
        isDoctorConnected = true;
        if (useChatApi && chatApi != null)
        {
            chatApi.currentUserId = currentPatientId;
            chatApi.currentUserType = "patient";
        }
        if (doctorLoginContainer != null) doctorLoginContainer.gameObject.SetActive(false);
        if (chatContainer != null) chatContainer.gameObject.SetActive(true);
        if (sendMessageButton != null) sendMessageButton.enabled = true;
        if (activeDoctorNameText != null) activeDoctorNameText.Text = $"Chatting with {currentDoctorName}";
        DisplayStatusMessage($"Connected to {currentDoctorName}.", new Color(0.4f, 1f, 0.6f));
        ClearChatHistory();
        AppendMessage("System", defaultGreeting);
        if (useChatApi && chatApi != null)
        {
            chatApi.FetchMessages(currentDoctorId, "doctor", OnChatHistoryLoaded);
            chatApi.StartAutoRefresh(currentDoctorId, "doctor");
        }
        if (connectDoctorButton != null) connectDoctorButton.enabled = true;
    }

    void DisplayStatusMessage(string msg, Color c)
    {
        if (connectionStatusText != null)
        {
            connectionStatusText.Text = msg;
            connectionStatusText.TMP.color = c;
        }
    }

    void SendMessage()
    {
        if (!isDoctorConnected)
        {
            DisplayStatusMessage("Please connect to a doctor first.", Color.yellow);
            return;
        }
        string msg = messageInputField?.text.Trim() ?? "";
        if (string.IsNullOrEmpty(msg)) return;
        messageInputField.text = "";
        if (useChatApi && chatApi != null)
        {
            if (string.IsNullOrEmpty(currentPatientId) || string.IsNullOrEmpty(currentDoctorId))
            {
                DisplayStatusMessage("Missing IDs. Cannot send.", Color.red);
                return;
            }
            if (sendMessageButton != null) sendMessageButton.enabled = false;
            DisplayStatusMessage("Sending...", Color.white);
            chatApi.SendMessage(currentDoctorId, "doctor", msg, response =>
            {
                if (sendMessageButton != null) sendMessageButton.enabled = true;
                if (response == null || response.status != "success")
                {
                    DisplayStatusMessage($"Send failed: {response?.message ?? "No response"}", Color.red);
                    return;
                }
                AppendMessage("You", msg, response.timestamp);
                DisplayStatusMessage("Sent.", new Color(0.6f, 0.95f, 0.6f));
            });
        }
        else
        {
            AppendMessage("You", msg);
            if (simulateDoctorResponses) StartCoroutine(SimulateDoctorReply());
        }
    }

    IEnumerator SimulateDoctorReply()
    {
        yield return new WaitForSeconds(simulatedResponseDelay);
        if (isDoctorConnected) AppendMessage(currentDoctorName, "Thank you for the update. I'll review the latest metrics soon.");
    }

    void AppendMessage(string speaker, string msg, string timestampIso = null)
    {
        DateTime time = string.IsNullOrEmpty(timestampIso) ? DateTime.Now : DateTime.Parse(timestampIso).ToLocalTime();
        string line = $"{time:HH:mm} - {speaker}: {msg}";
        chatHistoryBuilder.AppendLine(line);
        if (chatHistoryText != null) chatHistoryText.Text = chatHistoryBuilder.ToString();
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
        if (chatHistoryText != null) chatHistoryText.Text = "";
    }

    void ResetChatState()
    {
        isDoctorConnected = false;
        currentDoctorId = currentDoctorName = "";
        if (useChatApi && chatApi != null) chatApi.StopAutoRefresh();
        if (sendMessageButton != null) sendMessageButton.enabled = false;
        if (doctorIdInputField != null) doctorIdInputField.text = "";
        if (messageInputField != null) messageInputField.text = "";
        if (doctorLoginContainer != null) doctorLoginContainer.gameObject.SetActive(true);
        if (chatContainer != null) chatContainer.gameObject.SetActive(false);
    }

    void OnChatHistoryLoaded(ChatMessagesResponse response)
    {
        if (connectDoctorButton != null) connectDoctorButton.enabled = true;
        if (response == null || response.status != "success")
        {
            DisplayStatusMessage($"Load failed: {response?.message}", Color.red);
            return;
        }
        ClearChatHistory();
        if (response.messages != null && response.messages.Length > 0)
        {
            foreach (var m in response.messages)
            {
                if (m == null) continue;
                string speaker = string.Equals(m.senderId, chatApi?.currentUserId, StringComparison.OrdinalIgnoreCase) ? "You" : currentDoctorName;
                AppendMessage(speaker, m.message, m.timestamp);
            }
        }
        else AppendMessage("System", "No previous messages.");
        DisplayStatusMessage($"Connected to {currentDoctorName}.", new Color(0.4f, 1f, 0.6f));
    }

    public void OnNewChatMessage(ChatMessage message)
    {
        if (!isDoctorConnected || message == null || chatApi == null) return;
        bool sameConv = (string.Equals(message.senderId, currentDoctorId) && string.Equals(message.receiverId, currentPatientId)) ||
                        (string.Equals(message.receiverId, currentDoctorId) && string.Equals(message.senderId, currentPatientId));
        if (!sameConv) return;
        string speaker = string.Equals(message.senderId, chatApi.currentUserId) ? "You" : currentDoctorName;
        AppendMessage(speaker, message.message, message.timestamp);
    }

    public void SetCurrentPatientId(string patientId)
    {
        if (string.IsNullOrWhiteSpace(patientId)) return;
        currentPatientId = patientId.Trim();
        if (chatApi != null)
        {
            chatApi.currentUserId = currentPatientId;
            chatApi.currentUserType = "patient";
        }
    }

    void EnsureDependencies()
    {
        if (chatApi == null) chatApi = FindObjectOfType<ChatAPI>();
        if (patientInputManager == null) patientInputManager = FindObjectOfType<PatientInputManager>();
    }
}

[Serializable] public class DoctorNameResponse { public string status; public string message; public string doctorName; public string doctorId; }