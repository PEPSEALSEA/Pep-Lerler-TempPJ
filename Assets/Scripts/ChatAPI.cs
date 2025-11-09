using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Chat API client for Unity - Handles fetching and sending chat messages
/// </summary>
public class ChatAPI : Singleton<ChatAPI>
{
    [Header("API Configuration")]
    [Tooltip("Your Google Apps Script Web App URL")]
    public string apiUrl = "https://script.google.com/macros/s/AKfycbwOIWUdbLHsMl4Bwgh8TiauBvPQKVOy7XEFXy6grauL8V55qaFS0D4xtKoTtXmJCAnmGw/exec";

    [Header("Chat Configuration")]
    public string currentUserId = "DOC-123"; // Current logged-in user ID
    public string currentUserType = "doctor"; // "doctor" or "patient"
    public string chatWithUserId = ""; // ID of user you're chatting with
    public string chatWithUserType = "doctor"; // Type of user you're chatting with
    public int messageLimit = 50;
    public float autoRefreshInterval = 5f;

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
    /// Fetch chat messages between current user and another user
    /// </summary>
    public void FetchMessages(string otherUserId, string otherUserType, Action<ChatMessagesResponse> callback)
    {
        chatWithUserId = otherUserId;
        chatWithUserType = otherUserType;
        StartCoroutine(GetChatMessages(currentUserId, otherUserId, messageLimit, lastMessageTimestamp, callback));
    }

    /// <summary>
    /// Fetch all conversations for current user
    /// </summary>
    public void FetchConversations(Action<ConversationsResponse> callback)
    {
        StartCoroutine(GetConversations(currentUserId, callback));
    }

    /// <summary>
    /// Send a new chat message
    /// </summary>
    public void SendMessage(string receiverId, string receiverType, string message, Action<SendMessageResponse> callback)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            Debug.LogError("[ChatAPI] Cannot send empty message");
            callback?.Invoke(new SendMessageResponse { status = "error", message = "Message cannot be empty" });
            return;
        }

        if (string.IsNullOrWhiteSpace(receiverId))
        {
            Debug.LogError("[ChatAPI] Receiver ID is required");
            callback?.Invoke(new SendMessageResponse { status = "error", message = "Receiver ID is required" });
            return;
        }

        StartCoroutine(SendChatMessage(currentUserId, currentUserType, receiverId, receiverType, message, callback));
    }

    /// <summary>
    /// Start auto-refreshing messages at intervals
    /// </summary>
    public void StartAutoRefresh(string otherUserId, string otherUserType)
    {
        if (!isAutoRefreshing)
        {
            chatWithUserId = otherUserId;
            chatWithUserType = otherUserType;
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

            if (string.IsNullOrEmpty(chatWithUserId))
            {
                if (showDebugLogs) Debug.LogWarning("[ChatAPI] No chat user set for auto-refresh");
                continue;
            }

            // Fetch new messages since last timestamp
            StartCoroutine(GetChatMessages(currentUserId, chatWithUserId, messageLimit, lastMessageTimestamp, (response) =>
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
    /// Get chat messages between two users
    /// </summary>
    private IEnumerator GetChatMessages(string senderId, string receiverId, int limit, string since, Action<ChatMessagesResponse> callback)
    {
        string url = $"{apiUrl}?action=getChatMessages&senderId={UnityWebRequest.EscapeURL(senderId)}&receiverId={UnityWebRequest.EscapeURL(receiverId)}&limit={limit}";

        if (!string.IsNullOrEmpty(since))
        {
            url += $"&since={UnityWebRequest.EscapeURL(since)}";
        }

        if (showDebugLogs) Debug.Log($"[ChatAPI] Fetching messages: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Accept", "application/json");
            yield return request.SendWebRequest();

            ChatMessagesResponse response = new ChatMessagesResponse();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;

                if (showDebugLogs) Debug.Log($"[ChatAPI] Response: {responseText}");

                // Guard against HTML error pages or non-JSON responses
                if (!IsLikelyJson(responseText))
                {
                    Debug.LogError($"[ChatAPI] Expected JSON but got non-JSON response. Raw: {responseText}");
                    response.status = "error";
                    response.message = "Invalid response from server";
                }
                else
                {
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
    /// Get all conversations for a user
    /// </summary>
    private IEnumerator GetConversations(string doctorId, Action<ConversationsResponse> callback)
    {
        string url = $"{apiUrl}?action=getConversations&doctorId={UnityWebRequest.EscapeURL(doctorId)}";

        if (showDebugLogs) Debug.Log($"[ChatAPI] Fetching conversations: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Accept", "application/json");
            yield return request.SendWebRequest();

            ConversationsResponse response = new ConversationsResponse();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;

                if (showDebugLogs) Debug.Log($"[ChatAPI] Conversations response: {responseText}");

                // Guard against HTML error pages or non-JSON responses
                if (!IsLikelyJson(responseText))
                {
                    Debug.LogError($"[ChatAPI] Expected JSON but got non-JSON response. Raw: {responseText}");
                    response.status = "error";
                    response.message = "Invalid response from server";
                }
                else
                {
                    try
                    {
                        response = JsonUtility.FromJson<ConversationsResponse>(responseText);

                        if (response.status == "success")
                        {
                            if (showDebugLogs) Debug.Log($"[ChatAPI] ✓ Fetched {response.conversations.Length} conversations");
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
    private IEnumerator SendChatMessage(string sender, string senderType, string receiver, string receiverType, string message, Action<SendMessageResponse> callback)
    {
        SendChatMessageRequest requestData = new SendChatMessageRequest
        {
            action = "sendChatMessage",
            sender = sender,
            senderType = senderType,
            receiver = receiver,
            receiverType = receiverType,
            message = message
        };

        string jsonData = JsonUtility.ToJson(requestData);

        if (showDebugLogs) Debug.Log($"[ChatAPI] Sending message: {jsonData}");

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();

            bool requestSucceeded = request.result == UnityWebRequest.Result.Success;
            string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            bool looksJson = IsLikelyJson(responseText);

            if (requestSucceeded && looksJson)
            {
                HandleSendMessageResponse(request, callback);
                yield break;
            }

            Debug.LogWarning("[ChatAPI] Primary POST failed or returned non-JSON. Attempting GET fallback...");
        }

        yield return SendChatMessageViaGet(requestData, callback);
    }

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

    void HandleSendMessageResponse(UnityWebRequest request, Action<SendMessageResponse> callback)
    {
        SendMessageResponse response = new SendMessageResponse();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

            if (showDebugLogs) Debug.Log($"[ChatAPI] Send response: {responseText}");

            if (!IsLikelyJson(responseText))
            {
                Debug.LogError($"[ChatAPI] Expected JSON but got non-JSON response. Raw: {responseText}");
                response.status = "error";
                response.message = "Invalid response from server";
            }
            else
            {
                try
                {
                    response = JsonUtility.FromJson<SendMessageResponse>(responseText);

                    if (string.IsNullOrEmpty(response.message) && response.status == "success")
                    {
                        response.message = "Message sent successfully";
                    }

                    if (response.status == "success")
                    {
                        if (showDebugLogs) Debug.Log($"[ChatAPI] Message sent! ID: {response.messageId}");
                        lastMessageTimestamp = response.timestamp;
                    }
                    else
                    {
                        Debug.LogError($"[ChatAPI] Failed to send: {response.message}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ChatAPI] Failed to parse send response: {e.Message}\nResponse: {responseText}");
                    response.status = "error";
                    response.message = "Invalid response from server";
                }
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

    IEnumerator SendChatMessageViaGet(SendChatMessageRequest data, Action<SendMessageResponse> callback)
    {
        // FIXED: Use correct parameter names that match the API
        string url =
            $"{apiUrl}?action=sendChatMessage" +
            $"&sender={UnityWebRequest.EscapeURL(data.sender)}" +
            $"&senderType={UnityWebRequest.EscapeURL(data.senderType)}" +
            $"&receiver={UnityWebRequest.EscapeURL(data.receiver)}" +
            $"&receiverType={UnityWebRequest.EscapeURL(data.receiverType)}" +
            $"&message={UnityWebRequest.EscapeURL(data.message)}";

        if (showDebugLogs) Debug.Log($"[ChatAPI] GET fallback URL: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            HandleSendMessageResponse(request, callback);
        }
    }

    // Simple heuristic to check if a string looks like JSON to avoid parsing HTML error pages
    private static bool IsLikelyJson(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (char.IsWhiteSpace(c)) continue;
            return c == '{' || c == '[';
        }
        return false;
    }
}

// ============== DATA STRUCTURES ==============

[Serializable]
public class ChatMessage
{
    public string timestamp;
    public string messageId;
    public string senderId;
    public string senderType;
    public string receiverId;
    public string receiverType;
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
    public string senderType;
    public string receiver;
    public string receiverType;
    public string message;
}

[Serializable]
public class SendMessageResponse
{
    public string status;
    public string message;
    public string messageId;
    public string timestamp;
    public SendMessageResponse()
    {
        message = "";
    }

}

[Serializable]
public class Conversation
{
    public string otherId;
    public string otherType;
    public string otherName;
    public string lastMessage;
    public string lastMessageTime;
    public int unreadCount;
    public bool hasMessages;
}

[Serializable]
public class ConversationsResponse
{
    public string status;
    public string message;
    public Conversation[] conversations;
}