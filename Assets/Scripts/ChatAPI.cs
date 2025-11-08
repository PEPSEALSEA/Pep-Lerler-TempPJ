using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Chat API client for Unity - Handles fetching and sending chat messages
/// </summary>
public class ChatAPI : MonoBehaviour
{
    [Header("API Configuration")]
    [Tooltip("Your Google Apps Script Web App URL")]
    public string apiUrl = "https://script.google.com/macros/s/AKfycbwOIWUdbLHsMl4Bwgh8TiauBvPQKVOy7XEFXy6grauL8V55qaFS0D4xtKoTtXmJCAnmGw/exec";

    [Header("Chat Configuration")]
    public string currentUserId = "DOC-123"; // Set this to logged-in doctor ID or patient ID
    public int messageLimit = 50;
    public float autoRefreshInterval = 5f; // Auto-refresh messages every 5 seconds

    [Header("Debug")]
    public bool showDebugLogs = true;

    private bool isAutoRefreshing = false;
    private string lastMessageTimestamp = null;

    void Start()
    {
        // Optional: Start auto-refresh on start
        // StartAutoRefresh();
    }

    // ============== PUBLIC METHODS ==============

    /// <summary>
    /// Fetch all chat messages (or messages since last fetch)
    /// </summary>
    public void FetchMessages(Action<ChatMessagesResponse> callback)
    {
        StartCoroutine(GetChatMessages(messageLimit, lastMessageTimestamp, callback));
    }

    /// <summary>
    /// Send a new chat message
    /// </summary>
    public void SendMessage(string message, Action<SendMessageResponse> callback)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            Debug.LogError("[ChatAPI] Cannot send empty message");
            callback?.Invoke(new SendMessageResponse { status = "error", message = "Message cannot be empty" });
            return;
        }

        StartCoroutine(SendChatMessage(currentUserId, message, callback));
    }

    /// <summary>
    /// Start auto-refreshing messages at intervals
    /// </summary>
    public void StartAutoRefresh()
    {
        if (!isAutoRefreshing)
        {
            isAutoRefreshing = true;
            StartCoroutine(AutoRefreshCoroutine());
            if (showDebugLogs) Debug.Log("[ChatAPI] Auto-refresh started");
        }
    }

    /// <summary>
    /// Stop auto-refreshing messages
    /// </summary>
    public void StopAutoRefresh()
    {
        isAutoRefreshing = false;
        if (showDebugLogs) Debug.Log("[ChatAPI] Auto-refresh stopped");
    }


    // ============== COROUTINES ==============

    private IEnumerator AutoRefreshCoroutine()
    {
        while (isAutoRefreshing)
        {
            yield return new WaitForSeconds(autoRefreshInterval);

            // Fetch new messages since last timestamp
            StartCoroutine(GetChatMessages(messageLimit, lastMessageTimestamp, (response) =>
            {
                if (response.status == "success" && response.messages != null && response.messages.Length > 0)
                {
                    // Update last timestamp
                    lastMessageTimestamp = response.messages[response.messages.Length - 1].timestamp;

                    // Broadcast to listeners
                    BroadcastNewMessages(response.messages);
                }
            }));
        }
    }

    /// <summary>
    /// Get chat messages from server
    /// </summary>
    private IEnumerator GetChatMessages(int limit, string since, Action<ChatMessagesResponse> callback)
    {
        string url = $"{apiUrl}?action=getChatMessages&limit={limit}";
        if (!string.IsNullOrEmpty(since))
        {
            url += $"&since={UnityWebRequest.EscapeURL(since)}";
        }

        if (showDebugLogs) Debug.Log($"[ChatAPI] Fetching messages: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            ChatMessagesResponse response = new ChatMessagesResponse();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;

                if (showDebugLogs) Debug.Log($"[ChatAPI] Response: {responseText}");

                try
                {
                    response = JsonUtility.FromJson<ChatMessagesResponse>(responseText);

                    if (response.status == "success")
                    {
                        if (showDebugLogs) Debug.Log($"[ChatAPI] ✓ Fetched {response.messages.Length} messages");
                    }
                    else
                    {
                        Debug.LogError($"[ChatAPI] Server error: {response.message}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ChatAPI] Failed to parse response: {e.Message}");
                    response.status = "error";
                    response.message = "Failed to parse response";
                }
            }
            else
            {
                Debug.LogError($"[ChatAPI] Request failed: {request.error}");
                response.status = "error";
                response.message = request.error;
            }

            callback?.Invoke(response);
        }
    }

    /// <summary>
    /// Send a chat message to server
    /// </summary>
    private IEnumerator SendChatMessage(string sender, string message, Action<SendMessageResponse> callback)
    {
        SendChatMessageRequest requestData = new SendChatMessageRequest
        {
            action = "sendChatMessage",
            sender = sender,
            message = message
        };

        string jsonData = JsonUtility.ToJson(requestData);

        if (showDebugLogs) Debug.Log($"[ChatAPI] Sending message: {message.Substring(0, Math.Min(50, message.Length))}...");

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            SendMessageResponse response = new SendMessageResponse();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;

                if (showDebugLogs) Debug.Log($"[ChatAPI] Send response: {responseText}");

                try
                {
                    response = JsonUtility.FromJson<SendMessageResponse>(responseText);

                    if (response.status == "success")
                    {
                        if (showDebugLogs) Debug.Log($"[ChatAPI] ✓ Message sent successfully! ID: {response.messageId}");

                        // Update last timestamp to the sent message timestamp
                        lastMessageTimestamp = response.timestamp;
                    }
                    else
                    {
                        Debug.LogError($"[ChatAPI] Failed to send: {response.message}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ChatAPI] Failed to parse send response: {e.Message}");
                    response.status = "error";
                    response.message = "Failed to parse response";
                }
            }
            else
            {
                Debug.LogError($"[ChatAPI] Send failed: {request.error}");
                response.status = "error";
                response.message = request.error;
            }

            callback?.Invoke(response);
        }
    }

    /// <summary>
    /// Clear chat cache on server
    /// </summary>
    // ============== HELPER METHODS ==============

    /// <summary>
    /// Broadcast new messages to all listeners (can be used with Unity Events)
    /// </summary>
    private void BroadcastNewMessages(ChatMessage[] messages)
    {
        // Send message through Unity's messaging system
        foreach (ChatMessage msg in messages)
        {
            SendMessage("OnNewChatMessage", msg, SendMessageOptions.DontRequireReceiver);
        }
    }

    void OnDestroy()
    {
        StopAutoRefresh();
    }
}

// ============== DATA STRUCTURES ==============

[Serializable]
public class ChatMessage
{
    public string timestamp;
    public string messageId;
    public string sender;
    public string message;
    public bool isRead;
}

[Serializable]
public class ChatMessagesResponse
{
    public string status;
    public string message;
    public ChatMessage[] messages;
}

[Serializable]
public class SendChatMessageRequest
{
    public string action;
    public string sender;
    public string message;
}

[Serializable]
public class SendMessageResponse
{
    public string status;
    public string message;
    public string messageId;
    public string timestamp;
}