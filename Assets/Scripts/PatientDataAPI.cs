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
        // Pulse: No decimals (integer)
        float pulseValue = Mathf.Round(UnityEngine.Random.Range(60f, 100f));

        // Movement Magnitude: 3 decimals
        float movementValue = RoundToDecimals(UnityEngine.Random.Range(0f, 2f), 3);

        // Sleep Quality Score: 2 decimals
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

        // Occasionally generate anomaly data for testing
        if (UnityEngine.Random.value < 0.1f) // 10% chance
        {
            data.pulse = Mathf.Round(UnityEngine.Random.Range(130f, 160f)); // Anomalous pulse (no decimals)
            Debug.Log($"Generated ANOMALY data: Pulse={data.pulse}");
        }

        return data;
    }

    /// <summary>
    /// Generate sample joint angle data (no decimals)
    /// </summary>
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
    /// </summary>
    public IEnumerator SubmitPatientData(PatientData data, Action<ApiResponse> callback)
    {
        // Create the request with action "submitData" to match the API
        SubmitDataRequest requestData = new SubmitDataRequest
        {
            action = "submitData",
            timestamp = data.timestamp,
            patientId = data.patientId,
            pulse = data.pulse,
            movementMagnitude = data.movementMagnitude,
            sleepQualityScore = data.sleepQualityScore,
            jointAngle = data.jointAngle
        };

        string jsonData = JsonUtility.ToJson(requestData);

        Debug.Log($"Sending data: Patient={data.patientId}, Pulse={data.pulse}, Movement={data.movementMagnitude:F3}, Sleep={data.sleepQualityScore:F2}");

        // Use POST method with JSON body
        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            ProcessResponse(request, callback);
        }
    }

    void ProcessResponse(UnityWebRequest request, System.Action<ApiResponse> callback)
    {
        ApiResponse response = new ApiResponse();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;
            Debug.Log($"[API] Response received: {responseText}");

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
                        Debug.Log($"[API] Parsed response: status={response.status}, message={response.message}");
                    }
                    else
                    {
                        // Response is not JSON - might be HTML or plain text
                        response.status = "success";
                        response.message = "Data submitted (non-JSON response)";
                        Debug.LogWarning($"[API] Non-JSON response received: {responseText.Substring(0, Math.Min(100, responseText.Length))}");
                    }
                }
                catch (Exception e)
                {
                    // Even if JSON parsing fails, the request was successful
                    response.status = "success";
                    response.message = "Data submitted (failed to parse response)";
                    Debug.LogWarning($"[API] Failed to parse response: {e.Message}");
                }
            }
        }
        else
        {
            Debug.LogError($"[API] Request failed: {request.error} (Code: {request.responseCode})");
            Debug.LogError($"[API] Response body: {request.downloadHandler.text}");

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
            Debug.Log($"✓ Data submitted successfully! Message: {response.message}");

            // Only log anomalies
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

    /// <summary>
    /// Round to specific number of decimal places
    /// </summary>
    float RoundToDecimals(float value, int decimals)
    {
        float multiplier = Mathf.Pow(10f, decimals);
        return Mathf.Round(value * multiplier) / multiplier;
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