using System;
using System.Collections;
using System.Collections.Generic;
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

    private bool isAutoSending = false;

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
        if (!isAutoSending)
        {
            isAutoSending = true;
            StartCoroutine(AutoSendCoroutine());
        }
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
    /// Generate realistic sample patient data
    /// </summary>
    public PatientData GenerateSampleData(string patientId)
    {
        float pulseValue = RoundToIntFloat(UnityEngine.Random.Range(60f, 100f) + UnityEngine.Random.Range(-5f, 5f));
        float movementValue = RoundToTwoDecimals(UnityEngine.Random.Range(0f, 2f));
        float sleepValue = RoundToTwoDecimals(UnityEngine.Random.Range(60f, 95f));

        PatientData data = new PatientData
        {
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            patientId = patientId,
            pulse = pulseValue,
            movementMagnitude = movementValue,
            sleepQualityScore = sleepValue,
            jointAngle = GenerateSampleJointAngles()
        };

        // Occasionally generate anomaly data for testing
        if (UnityEngine.Random.value < 0.1f) // 10% chance
        {
            data.pulse = RoundToIntFloat(UnityEngine.Random.Range(130f, 160f)); // Anomalous pulse
            Debug.Log($"Generated ANOMALY data: Pulse={data.pulse:F1}");
        }

        return data;
    }

    /// <summary>
    /// Generate sample joint angle data
    /// </summary>
    private JointAngleData GenerateSampleJointAngles()
    {
        return new JointAngleData
        {
            leftShoulder = RoundToIntFloat(UnityEngine.Random.Range(0f, 180f)),
            rightShoulder = RoundToIntFloat(UnityEngine.Random.Range(0f, 180f)),
            leftElbow = RoundToIntFloat(UnityEngine.Random.Range(0f, 145f)),
            rightElbow = RoundToIntFloat(UnityEngine.Random.Range(0f, 145f)),
            leftHip = RoundToIntFloat(UnityEngine.Random.Range(0f, 120f)),
            rightHip = RoundToIntFloat(UnityEngine.Random.Range(0f, 120f)),
            leftKnee = RoundToIntFloat(UnityEngine.Random.Range(0f, 135f)),
            rightKnee = RoundToIntFloat(UnityEngine.Random.Range(0f, 135f))
        };
    }

    /// <summary>
    /// Submit patient data to Google Apps Script
    /// </summary>
    public IEnumerator SubmitPatientData(PatientData data, Action<ApiResponse> callback)
    {
        SubmitDataRequest requestData = new SubmitDataRequest
        {
            action = "addPatient",  // Changed from "submitData" to "addPatient" - this is what the API supports
            timestamp = data.timestamp,
            patientId = data.patientId,
            pulse = data.pulse,
            movementMagnitude = data.movementMagnitude,
            sleepQualityScore = data.sleepQualityScore,
            jointAngle = data.jointAngle
        };

        string jsonData = JsonUtility.ToJson(requestData);

        // Reduced logging - only log if there's an issue
        // Debug.Log($"Sending data to API: Patient={data.patientId}, Pulse={data.pulse:F1}");

        // Try POST first, but also support GET with query parameters for Google Apps Script
        // Google Apps Script Web Apps can accept both methods, but sometimes POST has issues
        string url = apiUrl;

        // Option 1: Try POST with JSON body (standard way)
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            // If POST fails with 405, try GET method instead
            if (request.responseCode == 405)
            {
                Debug.LogWarning("POST method not allowed (405). Trying GET method with query parameters...");
                request.Dispose();

                // Try GET method with action in URL
                string getUrl = $"{url}?action={UnityWebRequest.EscapeURL("addPatient")}&data={UnityWebRequest.EscapeURL(jsonData)}";

                using (UnityWebRequest getRequest = UnityWebRequest.Get(getUrl))
                {
                    yield return getRequest.SendWebRequest();
                    ProcessResponse(getRequest, callback);
                    yield break;
                }
            }

            ProcessResponse(request, callback);
        }
    }

    void ProcessResponse(UnityWebRequest request, System.Action<ApiResponse> callback)
    {
        ApiResponse response = new ApiResponse();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;

            // Check if response is empty or just whitespace
            if (string.IsNullOrWhiteSpace(responseText))
            {
                response.status = "success";
                response.message = "Data submitted (empty response from server)";
            }
            else
            {
                // Try to parse as JSON
                try
                {
                    responseText = responseText.Trim();

                    if (responseText.StartsWith("{") || responseText.StartsWith("["))
                    {
                        response = JsonUtility.FromJson<ApiResponse>(responseText);
                    }
                    else
                    {
                        // Response is not JSON - might be HTML or plain text
                        response.status = "success";
                        response.message = "Data submitted (non-JSON response)";
                    }
                }
                catch (Exception e)
                {
                    // Even if JSON parsing fails, the request was successful
                    response.status = "success";
                    response.message = "Data submitted (non-JSON response from server)";
                }
            }
        }
        else
        {
            // Only log errors, not every request
            Debug.LogError($"[API] Request failed: {request.error} (Code: {request.responseCode})");

            response.status = "error";
            response.message = request.error;
        }

        callback?.Invoke(response);
    }

    /// <summary>
    /// Get patient data from the server
    /// </summary>
    public IEnumerator GetPatientData(string patientId, Action<PatientDataResponse> callback)
    {
        string url = $"{apiUrl}?action=getPatientData&patientId={patientId}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            PatientDataResponse response = new PatientDataResponse();

            if (request.result == UnityWebRequest.Result.Success)
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

                        if (responseText.StartsWith("{") || responseText.StartsWith("["))
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
            // Only log anomalies, not every successful submission
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

    float RoundToTwoDecimals(float value)
    {
        return Mathf.Round(value * 100f) / 100f;
    }

    float RoundToIntFloat(float value)
    {
        return Mathf.Round(value);
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