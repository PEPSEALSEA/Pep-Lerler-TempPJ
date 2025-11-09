using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using Nova;

public class RehabDashboardController : MonoBehaviour
{
    [Header("Patient Input")]
    public PatientInputManager patientInputManager;

    [Header("Doctor Chat")]
    public DoctorChatManager doctorChatManager;

    [Header("Device Connection")]
    public DeviceConnectionManager deviceConnectionManager;

    [Header("API")]
    public PatientDataAPI patientDataAPI;

    [Header("Top Metrics")]
    public TextBlock pulseValueText;
    public TextBlock pulseChangeText;
    public UIBlock pulseFillBar;

    public TextBlock movementValueText;
    public TextBlock movementChangeText;
    public UIBlock movementFillBar;

    public TextBlock sleepValueText;
    public TextBlock sleepChangeText;
    public UIBlock sleepFillBar;

    [Header("Joint Angles")]
    public UIBlock kneeAngleBar;
    public UIBlock elbowAngleBar;
    public UIBlock shoulderAngleBar;
    public UIBlock hipAngleBar;

    [Header("Activity Graph")]
    public UIBlock lineGraph;
    public Button toggleAll;
    public Button togglePulse;
    public Button toggleMovement;
    public Button toggleSleep;

    [Header("UI Panels")]
    public UIBlock dashboardPanel;
    public Button refreshButton;
    public Button exportReportButton;

    private PatientData currentPatientData;
    private PatientData previousPatientData;
    private string currentPatientId = "";

    // Local tracking for UI display
    private float pulseChange = 0f;
    private float movementChange = 0f;
    private float sleepScoreChange = 0f;

    // Graph data storage
    private List<float> pulseData = new List<float>();
    private List<float> movementData = new List<float>();
    private List<float> sleepData = new List<float>();

    void Start()
    {
        // Find PatientDataAPI if not assigned
        if (patientDataAPI == null)
        {
            patientDataAPI = FindObjectOfType<PatientDataAPI>();
            if (patientDataAPI == null)
            {
                Debug.LogWarning("PatientDataAPI not found! Creating one...");
                GameObject apiObj = new GameObject("PatientDataAPI");
                patientDataAPI = apiObj.AddComponent<PatientDataAPI>();
            }
        }

        // Setup patient input manager
        if (patientInputManager != null)
        {
            patientInputManager.OnPatientIdSubmitted += OnPatientIdSubmitted;
        }

        // Setup device connection manager
        if (deviceConnectionManager != null)
        {
            deviceConnectionManager.OnDeviceConnected += OnDeviceConnected;
            deviceConnectionManager.OnDataUpdated += OnDeviceDataUpdated;
        }

        // Setup toggle listeners
        if (toggleAll != null)
            toggleAll.onValueChanged.AddListener(OnToggleAll);
        if (togglePulse != null)
            togglePulse.onValueChanged.AddListener(OnTogglePulse);
        if (toggleMovement != null)
            toggleMovement.onValueChanged.AddListener(OnToggleMovement);
        if (toggleSleep != null)
            toggleSleep.onValueChanged.AddListener(OnToggleSleep);

        // Setup button listeners
        if (refreshButton != null)
            refreshButton.onClick.AddListener(OnRefreshClicked);
        if (exportReportButton != null)
            exportReportButton.onClick.AddListener(OnExportReport);

        // Show dashboard initially with empty data
        if (dashboardPanel != null)
            dashboardPanel.SetActive(true);

        // Initialize with empty data
        InitializeEmptyData();
    }

    void OnPatientIdSubmitted(string patientId)
    {
        currentPatientId = patientId;
        // Reduced logging
        // Debug.Log($"Patient ID submitted: {patientId}");

        if (doctorChatManager != null)
        {
            doctorChatManager.SetCurrentPatientId(patientId);
        }

        // Update API test patient ID
        if (patientDataAPI != null)
        {
            patientDataAPI.testPatientId = patientId;
        }

        // Load existing data for this patient
        LoadPatientData(patientId);
    }

    void OnDeviceConnected()
    {
        Debug.Log("[DEVICE] Connected - data generation started");
    }

    void OnDeviceDataUpdated()
    {
        if (deviceConnectionManager != null && deviceConnectionManager.IsConnected())
        {
            GenerateRandomData();
        }
    }

    void InitializeEmptyData()
    {
        currentPatientData = new PatientData
        {
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            patientId = currentPatientId,
            pulse = 0,
            movementMagnitude = 0,
            sleepQualityScore = 0,
            jointAngle = new JointAngleData()
        };

        pulseChange = 0;
        movementChange = 0;
        sleepScoreChange = 0;

        // Initialize with some sample data so graph shows something
        pulseData.Clear();
        movementData.Clear();
        sleepData.Clear();

        // Add a few initial data points so graph is visible
        for (int i = 0; i < 5; i++)
        {
            pulseData.Add(RoundToIntFloat(UnityEngine.Random.Range(60f, 100f)));
            movementData.Add(RoundToTwoDecimals(UnityEngine.Random.Range(0.5f, 2f)));
            sleepData.Add(RoundToTwoDecimals(UnityEngine.Random.Range(70f, 95f)));
        }

        UpdateUI();
    }

    void LoadPatientData(string patientId)
    {
        if (patientDataAPI == null) return;

        StartCoroutine(patientDataAPI.GetPatientData(patientId, (response) =>
        {
            if (response.status == "success" && response.latestRecord != null)
            {
                // Convert PatientDataRecord to PatientData
                currentPatientData = new PatientData
                {
                    timestamp = response.latestRecord.timestamp,
                    patientId = response.latestRecord.patientId,
                    pulse = RoundToIntFloat(response.latestRecord.pulse),
                    movementMagnitude = RoundToTwoDecimals(response.latestRecord.movementMagnitude),
                    sleepQualityScore = RoundToTwoDecimals(response.latestRecord.sleepQualityScore),
                    jointAngle = response.latestRecord.jointAngle
                };

                // Calculate changes if we have previous data
                if (response.lastSevenDays != null && response.lastSevenDays.Length > 1)
                {
                    var previous = response.lastSevenDays[1];
                    CalculateChanges(previous);
                }

                // Build graph data from last 7 days
                BuildGraphData(response.lastSevenDays);

                UpdateUI();
            }
            else
            {
                InitializeEmptyData();
            }
        }));
    }

    void GenerateRandomData()
    {
        if (string.IsNullOrEmpty(currentPatientId))
        {
            currentPatientId = patientInputManager != null ? patientInputManager.GetCurrentPatientId() : "DEFAULT_PATIENT";
            if (doctorChatManager != null)
            {
                doctorChatManager.SetCurrentPatientId(currentPatientId);
            }
        }

        if (patientDataAPI == null) return;

        // Store previous data for comparison
        if (currentPatientData != null)
        {
            previousPatientData = new PatientData
            {
                pulse = currentPatientData.pulse,
                movementMagnitude = currentPatientData.movementMagnitude,
                sleepQualityScore = currentPatientData.sleepQualityScore
            };
        }

        // Generate new random data using PatientDataAPI
        currentPatientData = patientDataAPI.GenerateSampleData(currentPatientId);

        // Calculate changes from previous data
        if (previousPatientData != null)
        {
            CalculateChangesFromPrevious();
        }

        // Add to graph data
        currentPatientData.pulse = RoundToIntFloat(currentPatientData.pulse);
        currentPatientData.movementMagnitude = RoundToTwoDecimals(currentPatientData.movementMagnitude);
        currentPatientData.sleepQualityScore = RoundToTwoDecimals(currentPatientData.sleepQualityScore);

        pulseData.Add(currentPatientData.pulse);
        movementData.Add(currentPatientData.movementMagnitude);
        sleepData.Add(currentPatientData.sleepQualityScore);
        UIDebug.Log(nameof(RehabDashboardController), $"Generated mock data | Pulse={currentPatientData.pulse} Movement={currentPatientData.movementMagnitude:F2} Sleep={currentPatientData.sleepQualityScore:F2}");

        // Keep only last 24 data points
        if (pulseData.Count > 24)
        {
            pulseData.RemoveAt(0);
            movementData.RemoveAt(0);
            sleepData.RemoveAt(0);
        }

        // Submit to API
        StartCoroutine(patientDataAPI.SubmitPatientData(currentPatientData, (response) =>
        {
            if (response.status == "success")
            {
                Debug.Log($"[API] ✓ Data sent successfully to API | Patient: {currentPatientData.patientId} | Pulse: {Mathf.RoundToInt(currentPatientData.pulse)}");
                UIDebug.Log(nameof(RehabDashboardController), "Mock data submitted to API");
            }
            else
            {
                Debug.LogError($"[API] ✗ Submit failed: {response.message}");
                UIDebug.Error(nameof(RehabDashboardController), $"Submit failed: {response.message}");
            }
        }));

        UpdateUI();
    }

    void CalculateChanges(PatientDataRecord previous)
    {
        if (previous != null && currentPatientData != null)
        {
            if (previous.pulse > 0)
                pulseChange = RoundToTwoDecimals(((currentPatientData.pulse - previous.pulse) / previous.pulse) * 100f);

            if (previous.movementMagnitude > 0)
                movementChange = RoundToTwoDecimals(((currentPatientData.movementMagnitude - previous.movementMagnitude) / previous.movementMagnitude) * 100f);

            if (previous.sleepQualityScore > 0)
                sleepScoreChange = RoundToTwoDecimals(((currentPatientData.sleepQualityScore - previous.sleepQualityScore) / previous.sleepQualityScore) * 100f);
        }
    }

    void CalculateChangesFromPrevious()
    {
        if (previousPatientData != null && currentPatientData != null)
        {
            if (previousPatientData.pulse > 0)
                pulseChange = RoundToTwoDecimals(((currentPatientData.pulse - previousPatientData.pulse) / previousPatientData.pulse) * 100f);

            if (previousPatientData.movementMagnitude > 0)
                movementChange = RoundToTwoDecimals(((currentPatientData.movementMagnitude - previousPatientData.movementMagnitude) / previousPatientData.movementMagnitude) * 100f);

            if (previousPatientData.sleepQualityScore > 0)
                sleepScoreChange = RoundToTwoDecimals(((currentPatientData.sleepQualityScore - previousPatientData.sleepQualityScore) / previousPatientData.sleepQualityScore) * 100f);
        }
    }
    void BuildGraphData(PatientDataRecord[] records)
    {
        pulseData.Clear();
        movementData.Clear();
        sleepData.Clear();

        if (records != null && records.Length > 0)
        {
            foreach (var record in records)
            {
                pulseData.Add(RoundToIntFloat(record.pulse));
                movementData.Add(RoundToTwoDecimals(record.movementMagnitude));
                sleepData.Add(RoundToTwoDecimals(record.sleepQualityScore));
            }
        }
    }

    public void UpdateUI()
    {
        if (currentPatientData == null) return;

        // Update top metrics
        if (pulseValueText != null)
        {
            pulseValueText.text = currentPatientData.pulse > 0 ?
                Mathf.RoundToInt(currentPatientData.pulse).ToString() : "--";
        }
        if (pulseChangeText != null)
        {
            UpdateChangeText(pulseChangeText, pulseChange);
        }
        if (pulseFillBar != null)
        {
            pulseFillBar.fillAmount = Mathf.Clamp01(currentPatientData.pulse / 120f);
        }

        if (movementValueText != null)
        {
            movementValueText.text = currentPatientData.movementMagnitude > 0 ?
                currentPatientData.movementMagnitude.ToString("F2") : "--";
        }
        if (movementChangeText != null)
        {
            UpdateChangeText(movementChangeText, movementChange);
        }
        if (movementFillBar != null)
        {
            movementFillBar.fillAmount = Mathf.Clamp01(currentPatientData.movementMagnitude / 2f);
        }

        if (sleepValueText != null)
        {
            sleepValueText.text = currentPatientData.sleepQualityScore > 0 ?
                currentPatientData.sleepQualityScore.ToString("F2") : "--";
        }
        if (sleepChangeText != null)
        {
            UpdateChangeText(sleepChangeText, sleepScoreChange);
        }
        if (sleepFillBar != null)
        {
            sleepFillBar.fillAmount = currentPatientData.sleepQualityScore / 100f;
        }

        // Update joint angles (using average of left/right)
        if (currentPatientData.jointAngle != null)
        {
            if (kneeAngleBar != null)
            {
                float avgKnee = RoundToIntFloat((currentPatientData.jointAngle.leftKnee + currentPatientData.jointAngle.rightKnee) / 2f);
                kneeAngleBar.SetAngle(avgKnee, "Knee");
            }

            if (elbowAngleBar != null)
            {
                float avgElbow = RoundToIntFloat((currentPatientData.jointAngle.leftElbow + currentPatientData.jointAngle.rightElbow) / 2f);
                elbowAngleBar.SetAngle(avgElbow, "Elbow");
            }

            if (shoulderAngleBar != null)
            {
                float avgShoulder = RoundToIntFloat((currentPatientData.jointAngle.leftShoulder + currentPatientData.jointAngle.rightShoulder) / 2f);
                shoulderAngleBar.SetAngle(avgShoulder, "Shoulder");
            }

            if (hipAngleBar != null)
            {
                float avgHip = RoundToIntFloat((currentPatientData.jointAngle.leftHip + currentPatientData.jointAngle.rightHip) / 2f);
                hipAngleBar.SetAngle(avgHip, "Hip");
            }
        }

        // Update graph
        if (lineGraph != null)
        {
            UpdateGraph();
        }
    }

    void UpdateChangeText(TextMeshProUGUI text, float change)
    {
        if (text == null) return;

        if (Mathf.Abs(change) < 0.01f)
        {
            text.text = "No change";
            text.color = Color.gray;
            return;
        }

        string arrow = change >= 0 ? "↑" : "↓";
        text.text = $"{arrow} {Mathf.Abs(change):F2}% from yesterday";
        text.color = change >= 0 ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.8f, 0.3f, 0.3f);
    }

    void UpdateGraph()
    {
        if (lineGraph == null)
        {
            Debug.LogWarning("[GRAPH] LineGraphRenderer is null! Assign it in Inspector.");
            return;
        }

        if (lineGraph.graphContainer == null)
        {
            Debug.LogWarning("[GRAPH] GraphContainer is null! Assign RectTransform in LineGraphRenderer.");
            return;
        }

        lineGraph.ClearLines();

        // Check if we have any data
        bool hasPulseData = pulseData != null && pulseData.Count > 0;
        bool hasMovementData = movementData != null && movementData.Count > 0;
        bool hasSleepData = sleepData != null && sleepData.Count > 0;

        int linesAdded = 0;

        if (toggleAll != null && toggleAll.isOn)
        {
            if (hasPulseData)
            {
                lineGraph.AddLine(pulseData, new Color(0.2f, 0.5f, 1f), "Pulse");
                linesAdded++;
            }
            if (hasMovementData)
            {
                lineGraph.AddLine(movementData, new Color(0.3f, 0.8f, 0.9f), "Movement");
                linesAdded++;
            }
            if (hasSleepData)
            {
                lineGraph.AddLine(sleepData, new Color(1f, 0.8f, 0.2f), "Sleep Score");
                linesAdded++;
            }
        }
        else
        {
            if (togglePulse != null && togglePulse.isOn && hasPulseData)
            {
                lineGraph.AddLine(pulseData, new Color(0.2f, 0.5f, 1f), "Pulse");
                linesAdded++;
            }
            if (toggleMovement != null && toggleMovement.isOn && hasMovementData)
            {
                lineGraph.AddLine(movementData, new Color(0.3f, 0.8f, 0.9f), "Movement");
                linesAdded++;
            }
            if (toggleSleep != null && toggleSleep.isOn && hasSleepData)
            {
                lineGraph.AddLine(sleepData, new Color(1f, 0.8f, 0.2f), "Sleep Score");
                linesAdded++;
            }
        }

        if (linesAdded > 0)
        {
            lineGraph.DrawGraph();
            Debug.Log($"[GRAPH] Updated: {linesAdded} line(s) | Data: Pulse={pulseData?.Count ?? 0}, Movement={movementData?.Count ?? 0}, Sleep={sleepData?.Count ?? 0}");
        }
        else
        {
            Debug.LogWarning("[GRAPH] No data to display! Make sure toggles are ON and data is being generated.");
        }
    }

    void OnToggleAll(bool isOn)
    {
        if (isOn && toggleAll != null)
        {
            if (togglePulse != null) togglePulse.isOn = false;
            if (toggleMovement != null) toggleMovement.isOn = false;
            if (toggleSleep != null) toggleSleep.isOn = false;
        }
        UpdateGraph();
    }

    void OnTogglePulse(bool isOn)
    {
        if (isOn && toggleAll != null) toggleAll.isOn = false;
        UpdateGraph();
    }

    void OnToggleMovement(bool isOn)
    {
        if (isOn && toggleAll != null) toggleAll.isOn = false;
        UpdateGraph();
    }

    void OnToggleSleep(bool isOn)
    {
        if (isOn && toggleAll != null) toggleAll.isOn = false;
        UpdateGraph();
    }

    public void OnRefreshClicked()
    {
        if (!string.IsNullOrEmpty(currentPatientId))
        {
            LoadPatientData(currentPatientId);
        }
        else
        {
            GenerateRandomData();
        }
    }

    public void OnExportReport()
    {
        Debug.Log("=== EXPORT REPORT ===");
        Debug.Log("Note: Export Report button does NOT send data to API.");
        Debug.Log("Data is automatically sent when you click 'Connect Device'.");
        Debug.Log("This button is for exporting a report file (PDF/CSV) - not yet implemented.");
        Debug.Log("=====================");

        // TODO: Implement export functionality (PDF/CSV generation)
        // For now, just log current data
        if (currentPatientData != null)
        {
            Debug.Log($"Current Patient: {currentPatientData.patientId}");
            Debug.Log($"Pulse: {Mathf.RoundToInt(currentPatientData.pulse)}");
            Debug.Log($"Movement: {currentPatientData.movementMagnitude:F2}");
            Debug.Log($"Sleep Score: {currentPatientData.sleepQualityScore:F2}");
        }
    }

    public void LoadMockData()
    {
        GenerateRandomData();
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
