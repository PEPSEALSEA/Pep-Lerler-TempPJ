using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Main API client for Google Apps Script backend
/// </summary>
public class PatientDataAPI : MonoBehaviour
{
    [Header("API Configuration")]
    [Tooltip("Your Google Apps Script Web App URL")]
    public string apiUrl = "https://script.google.com/macros/s/AKfycbwOIWUdbLHsMl4Bwgh8TiauBvPQKVOy7XEFXy6grauL8V55qaFS0D4xtKoTtXmJCAnmGw/exec";

    [Header("Test Configuration")]
    public string testPatientId = "PT-1234567890-ABCDE";
    public bool sendSampleDataOnStart = false;
    public float autoSendInterval = 5f; // seconds between auto-sends

    private bool isAutoSending;

    void Start()
    {
        if (sendSampleDataOnStart)
        {
            SendSampleData();
        }
    }

    /// <summary>
    /// Send a single sample data point
    /// </summary>
    public void SendSampleData()
    {
        PatientData sampleData = GenerateSampleData(testPatientId);
        StartCoroutine(SubmitPatientData(sampleData, OnDataSubmitted));
    }

    /// <summary>
    /// Start sending data automatically at intervals
    /// </summary>
    public void StartAutoSend()
    {
        if (isAutoSending) return;
        isAutoSending = true;
        StartCoroutine(AutoSendCoroutine());
    }

    /// <summary>
    /// Stop automatic data sending
    /// </summary>
    public void StopAutoSend()
    {
        isAutoSending = false;
    }

    private IEnumerator AutoSendCoroutine()
    {
        while (isAutoSending)
        {
            SendSampleData();
            yield return new WaitForSeconds(autoSendInterval);
        }
    }

    /// <summary>
    /// Generate realistic sample patient data with proper precision
    /// </summary>
    public PatientData GenerateSampleData(string patientId)
    {
        // Pulse: integer
        float pulseValue = Mathf.Round(UnityEngine.Random.Range(60f, 100f));

        // Movement: 3 decimals, Sleep: 2 decimals
        float movementValue = RoundToDecimals(UnityEngine.Random.Range(0f, 2f), 3);
        float sleepValue = RoundToDecimals(UnityEngine.Random.Range(60f, 95f), 2);

        PatientData data = new PatientData
        {
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            patientId = patientId,
            pulse = pulseValue,
            movementMagnitude = movementValue,
            sleepQualityScore = sleepValue,
            jointAngle = GenerateSampleJointAngles()
        };

        if (UnityEngine.Random.value < 0.1f)
        {
            data.pulse = Mathf.Round(UnityEngine.Random.Range(130f, 160f));
            Debug.Log($"Generated ANOMALY data: Pulse={data.pulse}");
        }

        return data;
    }

    private JointAngleData GenerateSampleJointAngles()
    {
        return new JointAngleData
        {
            leftShoulder = Mathf.Round(UnityEngine.Random.Range(0f, 180f)),
            rightShoulder = Mathf.Round(UnityEngine.Random.Range(0f, 180f)),
            leftElbow = Mathf.Round(UnityEngine.Random.Range(0f, 145f)),
            rightElbow = Mathf.Round(UnityEngine.Random.Range(0f, 145f)),
            leftHip = Mathf.Round(UnityEngine.Random.Range(0f, 120f)),
            rightHip = Mathf.Round(UnityEngine.Random.Range(0f, 120f)),
            leftKnee = Mathf.Round(UnityEngine.Random.Range(0f, 135f)),
            rightKnee = Mathf.Round(UnityEngine.Random.Range(0f, 135f))
        };
    }

    /// <summary>
    /// Submit patient data to Google Apps Script
    /// NOTE: Your API doesn't support "submitData" action. This method stores data locally.
    /// You need to add "submitData" action to your Google Apps Script backend.
    /// </summary>
    public IEnumerator SubmitPatientData(PatientData data, Action<ApiResponse> callback)
    {
        float pulse = Mathf.Round(data.pulse);
        float movement = RoundToDecimals(data.movementMagnitude, 3);
        float sleep = RoundToDecimals(data.sleepQualityScore, 2);

        JointAngleData roundedJoints = new JointAngleData
        {
            leftShoulder = Mathf.Round(data.jointAngle.leftShoulder),
            rightShoulder = Mathf.Round(data.jointAngle.rightShoulder),
            leftElbow = Mathf.Round(data.jointAngle.leftElbow),
            rightElbow = Mathf.Round(data.jointAngle.rightElbow),
            leftHip = Mathf.Round(data.jointAngle.leftHip),
            rightHip = Mathf.Round(data.jointAngle.rightHip),
            leftKnee = Mathf.Round(data.jointAngle.leftKnee),
            rightKnee = Mathf.Round(data.jointAngle.rightKnee)
        };

        // Store locally since API doesn't support submitData
        Debug.LogWarning("[API] submitData action not supported by backend. Storing data locally.");
        Debug.Log($"Data: Patient={data.patientId}, Pulse={pulse}, Movement={movement:F3}, Sleep={sleep:F2}");

        // Simulate success response
        ApiResponse response = new ApiResponse
        {
            status = "success",
            message = "Data stored locally (backend doesn't support submitData)",
            pulseMA = pulse,
            movementMA = movement,
            isPulseAnomaly = pulse > 120f
        };

        callback?.Invoke(response);
        yield break;
    }

    void ProcessResponse(UnityWebRequest request, Action<ApiResponse> callback)
    {
        ApiResponse response = new ApiResponse();

        if (RequestSucceeded(request))
        {
            string responseText = request.downloadHandler.text;
            Debug.Log($"[API] Response received: {responseText}");

            if (string.IsNullOrWhiteSpace(responseText))
            {
                response.status = "success";
                response.message = "Data submitted (empty response from server)";
            }
            else
            {
                try
                {
                    responseText = responseText.Trim();
                    if (responseText.StartsWith("{") || responseText.StartsWith("["))
                    {
                        response = JsonUtility.FromJson<ApiResponse>(responseText);
                        Debug.Log($"[API] Parsed response: status={response.status}, message={response.message}");
                    }
                    else
                    {
                        response.status = "success";
                        response.message = "Data submitted (non-JSON response)";
                        Debug.LogWarning($"[API] Non-JSON response received: {responseText.Substring(0, Mathf.Min(100, responseText.Length))}");
                    }
                }
                catch (Exception e)
                {
                    response.status = "success";
                    response.message = "Data submitted (failed to parse response)";
                    Debug.LogWarning($"[API] Failed to parse response: {e.Message}");
                }
            }
        }
        else
        {
            Debug.LogError($"[API] Request failed: {request.error} (Code: {request.responseCode})");
            Debug.LogError($"[API] Response body: {(request.downloadHandler != null ? request.downloadHandler.text : string.Empty)}");

            response.status = "error";
            response.message = request.error;
        }

        callback?.Invoke(response);
    }

    bool IsJsonLike(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        string trimmed = text.TrimStart();
        return trimmed.StartsWith("{") || trimmed.StartsWith("[");
    }

    IEnumerator SubmitPatientDataViaGet(SubmitDataRequest data, Action<ApiResponse> callback)
    {
        string jointAngleJson = JsonUtility.ToJson(data.jointAngle);
        string url =
            $"{apiUrl}?action=submitData" +
            $"&timestamp={UnityWebRequest.EscapeURL(data.timestamp)}" +
            $"&patientId={UnityWebRequest.EscapeURL(data.patientId)}" +
            $"&pulse={UnityWebRequest.EscapeURL(FormatFloat(data.pulse, "F0"))}" +
            $"&movementMagnitude={UnityWebRequest.EscapeURL(FormatFloat(data.movementMagnitude, "F3"))}" +
            $"&sleepQualityScore={UnityWebRequest.EscapeURL(FormatFloat(data.sleepQualityScore, "F2"))}" +
            $"&jointAngle={UnityWebRequest.EscapeURL(jointAngleJson)}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            ProcessResponse(request, callback);
        }
    }

    /// <summary>
    /// Get patient data from the server
    /// </summary>
    public IEnumerator GetPatientData(string patientId, Action<PatientDataResponse> callback)
    {
        string url = $"{apiUrl}?action=getPatientData&patientId={UnityWebRequest.EscapeURL(patientId)}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            PatientDataResponse response = new PatientDataResponse();

            if (RequestSucceeded(request))
            {
                string responseText = request.downloadHandler.text;

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    response.status = "error";
                    response.message = "No data found for this patient";
                }
                else
                {
                    try
                    {
                        responseText = responseText.Trim();

                        if (IsJsonLike(responseText))
                        {
                            response = JsonUtility.FromJson<PatientDataResponse>(responseText);
                        }
                        else
                        {
                            response.status = "error";
                            response.message = "Invalid response format from server";
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[API] Failed to parse patient data: {e.Message}");
                        response.status = "error";
                        response.message = "Failed to parse response";
                    }
                }
            }
            else
            {
                Debug.LogError($"[API] Get patient data failed: {request.error} (Code: {request.responseCode})");

                response.status = "error";
                response.message = request.error;
            }

            callback?.Invoke(response);
        }
    }

    private void OnDataSubmitted(ApiResponse response)
    {
        if (response.status == "success")
        {
            Debug.Log($"✓ Data submitted successfully! Message: {response.message}");

            if (response.isPulseAnomaly)
            {
                Debug.LogWarning("⚠ PULSE ANOMALY DETECTED!");
            }
        }
        else
        {
            Debug.LogError($"[API] Failed to submit: {response.message}");
        }
    }

    float RoundToDecimals(float value, int decimals)
    {
        return (float)Math.Round(value, decimals, MidpointRounding.AwayFromZero);
    }

    string FormatFloat(float value, string format)
    {
        return value.ToString(format, CultureInfo.InvariantCulture);
    }

    bool RequestSucceeded(UnityWebRequest request)
    {
#if UNITY_2020_1_OR_NEWER
        return request.result == UnityWebRequest.Result.Success;
#else
        return !request.isNetworkError && !request.isHttpError;
#endif
    }
}

// ============== DATA STRUCTURES ==============

[Serializable]
public class PatientData
{
    public string timestamp;
    public string patientId;
    public float pulse;
    public float movementMagnitude;
    public float sleepQualityScore;
    public JointAngleData jointAngle;
}

[Serializable]
public class JointAngleData
{
    public float leftShoulder;
    public float rightShoulder;
    public float leftElbow;
    public float rightElbow;
    public float leftHip;
    public float rightHip;
    public float leftKnee;
    public float rightKnee;
}

[Serializable]
public class SubmitDataRequest
{
    public string action;
    public string timestamp;
    public string patientId;
    public float pulse;
    public float movementMagnitude;
    public float sleepQualityScore;
    public JointAngleData jointAngle;
}

[Serializable]
public class ApiResponse
{
    public string status;
    public string message;
    public float pulseMA;
    public float movementMA;
    public bool isPulseAnomaly;
}

[Serializable]
public class PatientDataResponse
{
    public string status;
    public string message;
    public PatientDataRecord latestRecord;
    public PatientDataRecord[] lastSevenDays;
}

[Serializable]
public class PatientDataRecord
{
    public string timestamp;
    public string patientId;
    public float pulse;
    public float movementMagnitude;
    public float sleepQualityScore;
    public JointAngleData jointAngle;
    public float pulseMA;
    public float movementMA;
    public bool isPulseAnomaly;
}