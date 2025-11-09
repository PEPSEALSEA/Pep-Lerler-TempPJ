using UnityEngine;
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
    public UIBlock2D kneeAngleBar;
    public UIBlock2D elbowAngleBar;
    public UIBlock2D shoulderAngleBar;
    public UIBlock2D hipAngleBar;
    [Header("Activity Graph")]
    public LineGraphRenderer lineGraph;
    public Interactable toggleAll;
    public Interactable togglePulse;
    public Interactable toggleMovement;
    public Interactable toggleSleep;
    [Header("UI Panels")]
    public UIBlock dashboardPanel;
    public Interactable refreshButton;
    public Interactable exportReportButton;

    private PatientData currentPatientData;
    private PatientData previousPatientData;
    private string currentPatientId = "";
    private float pulseChange = 0f;
    private float movementChange = 0f;
    private float sleepScoreChange = 0f;
    private List<float> pulseData = new List<float>();
    private List<float> movementData = new List<float>();
    private List<float> sleepData = new List<float>();

    void Start()
    {
        if (patientDataAPI == null)
        {
            patientDataAPI = FindObjectOfType<PatientDataAPI>();
            if (patientDataAPI == null)
            {
                GameObject apiObj = new GameObject("PatientDataAPI");
                patientDataAPI = apiObj.AddComponent<PatientDataAPI>();
            }
        }

        if (patientInputManager != null)
            patientInputManager.OnPatientIdSubmitted += OnPatientIdSubmitted;

        if (deviceConnectionManager != null)
        {
            deviceConnectionManager.OnDeviceConnected += OnDeviceConnected;
            deviceConnectionManager.OnDataUpdated += OnDeviceDataUpdated;
        }

        if (toggleAll != null && toggleAll.UIBlock != null)
            toggleAll.UIBlock.AddGestureHandler<Gesture.OnClick>(evt => OnToggleAll(toggleAll.enabled));
        if (togglePulse != null && togglePulse.UIBlock != null)
            togglePulse.UIBlock.AddGestureHandler<Gesture.OnClick>(evt => OnTogglePulse(togglePulse.enabled));
        if (toggleMovement != null && toggleMovement.UIBlock != null)
            toggleMovement.UIBlock.AddGestureHandler<Gesture.OnClick>(evt => OnToggleMovement(toggleMovement.enabled));
        if (toggleSleep != null && toggleSleep.UIBlock != null)
            toggleSleep.UIBlock.AddGestureHandler<Gesture.OnClick>(evt => OnToggleSleep(toggleSleep.enabled));

        if (refreshButton != null && refreshButton.UIBlock != null)
            refreshButton.UIBlock.AddGestureHandler<Gesture.OnClick>(evt => OnRefreshClicked());
        if (exportReportButton != null && exportReportButton.UIBlock != null)
            exportReportButton.UIBlock.AddGestureHandler<Gesture.OnClick>(evt => OnExportReport());

        if (dashboardPanel != null)
            dashboardPanel.gameObject.SetActive(true);

        InitializeEmptyData();
    }

    void OnPatientIdSubmitted(string patientId)
    {
        currentPatientId = patientId;
        if (doctorChatManager != null) doctorChatManager.SetCurrentPatientId(patientId);
        if (patientDataAPI != null) patientDataAPI.testPatientId = patientId;
        LoadPatientData(patientId);
    }

    void OnDeviceConnected() { }

    void OnDeviceDataUpdated()
    {
        if (deviceConnectionManager != null && deviceConnectionManager.IsConnected())
            GenerateRandomData();
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
        pulseChange = movementChange = sleepScoreChange = 0;
        pulseData.Clear(); movementData.Clear(); sleepData.Clear();
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
        StartCoroutine(patientDataAPI.GetPatientData(patientId, response =>
        {
            if (response.status == "success" && response.latestRecord != null)
            {
                currentPatientData = new PatientData
                {
                    timestamp = response.latestRecord.timestamp,
                    patientId = response.latestRecord.patientId,
                    pulse = RoundToIntFloat(response.latestRecord.pulse),
                    movementMagnitude = RoundToTwoDecimals(response.latestRecord.movementMagnitude),
                    sleepQualityScore = RoundToTwoDecimals(response.latestRecord.sleepQualityScore),
                    jointAngle = response.latestRecord.jointAngle
                };
                if (response.lastSevenDays != null && response.lastSevenDays.Length > 1)
                    CalculateChanges(response.lastSevenDays[1]);
                BuildGraphData(response.lastSevenDays);
                UpdateUI();
            }
            else InitializeEmptyData();
        }));
    }

    void GenerateRandomData()
    {
        if (string.IsNullOrEmpty(currentPatientId))
            currentPatientId = patientInputManager != null ? patientInputManager.GetCurrentPatientId() : "DEFAULT_PATIENT";

        if (doctorChatManager != null) doctorChatManager.SetCurrentPatientId(currentPatientId);
        if (patientDataAPI == null) return;

        if (currentPatientData != null)
            previousPatientData = new PatientData { pulse = currentPatientData.pulse, movementMagnitude = currentPatientData.movementMagnitude, sleepQualityScore = currentPatientData.sleepQualityScore };

        currentPatientData = patientDataAPI.GenerateSampleData(currentPatientId);
        if (previousPatientData != null) CalculateChangesFromPrevious();

        currentPatientData.pulse = RoundToIntFloat(currentPatientData.pulse);
        currentPatientData.movementMagnitude = RoundToTwoDecimals(currentPatientData.movementMagnitude);
        currentPatientData.sleepQualityScore = RoundToTwoDecimals(currentPatientData.sleepQualityScore);

        pulseData.Add(currentPatientData.pulse);
        movementData.Add(currentPatientData.movementMagnitude);
        sleepData.Add(currentPatientData.sleepQualityScore);

        if (pulseData.Count > 24) { pulseData.RemoveAt(0); movementData.RemoveAt(0); sleepData.RemoveAt(0); }

        StartCoroutine(patientDataAPI.SubmitPatientData(currentPatientData, _ => { }));
        UpdateUI();
    }

    void CalculateChanges(PatientDataRecord prev)
    {
        if (prev == null || currentPatientData == null) return;
        pulseChange = prev.pulse > 0 ? RoundToTwoDecimals((currentPatientData.pulse - prev.pulse) / prev.pulse * 100f) : 0;
        movementChange = prev.movementMagnitude > 0 ? RoundToTwoDecimals((currentPatientData.movementMagnitude - prev.movementMagnitude) / prev.movementMagnitude * 100f) : 0;
        sleepScoreChange = prev.sleepQualityScore > 0 ? RoundToTwoDecimals((currentPatientData.sleepQualityScore - prev.sleepQualityScore) / prev.sleepQualityScore * 100f) : 0;
    }

    void CalculateChangesFromPrevious()
    {
        if (previousPatientData == null || currentPatientData == null) return;
        pulseChange = previousPatientData.pulse > 0 ? RoundToTwoDecimals((currentPatientData.pulse - previousPatientData.pulse) / previousPatientData.pulse * 100f) : 0;
        movementChange = previousPatientData.movementMagnitude > 0 ? RoundToTwoDecimals((currentPatientData.movementMagnitude - previousPatientData.movementMagnitude) / previousPatientData.movementMagnitude * 100f) : 0;
        sleepScoreChange = previousPatientData.sleepQualityScore > 0 ? RoundToTwoDecimals((currentPatientData.sleepQualityScore - previousPatientData.sleepQualityScore) / previousPatientData.sleepQualityScore * 100f) : 0;
    }

    void BuildGraphData(PatientDataRecord[] records)
    {
        pulseData.Clear(); movementData.Clear(); sleepData.Clear();
        if (records == null) return;
        foreach (var r in records)
        {
            pulseData.Add(RoundToIntFloat(r.pulse));
            movementData.Add(RoundToTwoDecimals(r.movementMagnitude));
            sleepData.Add(RoundToTwoDecimals(r.sleepQualityScore));
        }
    }

    public void UpdateUI()
    {
        if (currentPatientData == null) return;

        if (pulseValueText != null)
            pulseValueText.Text = currentPatientData.pulse > 0 ? Mathf.RoundToInt(currentPatientData.pulse).ToString() : "--";
        if (pulseChangeText != null)
            UpdateChangeText(pulseChangeText, pulseChange);
        if (pulseFillBar != null)
        {
            float fill = Mathf.Clamp01(currentPatientData.pulse / 120f) * 100f;
            //pulseFillBar.Size = new Length3 { Y = Length.Percent(fill) };
        }

        if (movementValueText != null)
            movementValueText.Text = currentPatientData.movementMagnitude > 0 ? currentPatientData.movementMagnitude.ToString("F2") : "--";
        if (movementChangeText != null)
            UpdateChangeText(movementChangeText, movementChange);
        if (movementFillBar != null)
        {
            float fill = Mathf.Clamp01(currentPatientData.movementMagnitude / 2f) * 100f;
            //movementFillBar.Size = new Length3 { Y = Length.Percent(fill) };
        }

        if (sleepValueText != null)
            sleepValueText.Text = currentPatientData.sleepQualityScore > 0 ? currentPatientData.sleepQualityScore.ToString("F2") : "--";
        if (sleepChangeText != null)
            UpdateChangeText(sleepChangeText, sleepScoreChange);
        if (sleepFillBar != null)
        {
            float fill = currentPatientData.sleepQualityScore / 100f * 100f;
            //sleepFillBar.Size = new Length3 { Y = Length.Percent(fill) };
        }

        if (currentPatientData.jointAngle != null)
        {
            float avgKnee = RoundToIntFloat((currentPatientData.jointAngle.leftKnee + currentPatientData.jointAngle.rightKnee) / 2f);
            float avgElbow = RoundToIntFloat((currentPatientData.jointAngle.leftElbow + currentPatientData.jointAngle.rightElbow) / 2f);
            float avgShoulder = RoundToIntFloat((currentPatientData.jointAngle.leftShoulder + currentPatientData.jointAngle.rightShoulder) / 2f);
            float avgHip = RoundToIntFloat((currentPatientData.jointAngle.leftHip + currentPatientData.jointAngle.rightHip) / 2f);

            if (kneeAngleBar != null) SetGaugeAngle(kneeAngleBar, avgKnee);
            if (elbowAngleBar != null) SetGaugeAngle(elbowAngleBar, avgElbow);
            if (shoulderAngleBar != null) SetGaugeAngle(shoulderAngleBar, avgShoulder);
            if (hipAngleBar != null) SetGaugeAngle(hipAngleBar, avgHip);
        }

        UpdateGraph();
    }

    void UpdateChangeText(TextBlock text, float change)
    {
        if (text == null) return;
        if (Mathf.Abs(change) < 0.01f)
        {
            text.Text = "No change";
            text.TMP.color = Color.gray;
            return;
        }
        string arrow = change >= 0 ? "Up" : "Down";
        text.Text = $"{arrow} {Mathf.Abs(change):F2}% from yesterday";
        text.TMP.color = change >= 0 ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.8f, 0.3f, 0.3f);
    }

    void SetGaugeAngle(UIBlock2D gauge, float angle)
    {
        if (gauge == null) return;
        angle = Mathf.Clamp(angle, 0f, 180f);
        float t = angle / 180f;
        gauge.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-90f, 90f, t));
    }

    void UpdateGraph()
    {
        if (lineGraph == null) return;
        lineGraph.ClearLines();

        bool showAll = toggleAll != null && toggleAll.enabled;
        bool showPulse = showAll || (togglePulse != null && togglePulse.enabled);
        bool showMove = showAll || (toggleMovement != null && toggleMovement.enabled);
        bool showSleep = showAll || (toggleSleep != null && toggleSleep.enabled);

        if (showPulse && pulseData.Count > 0)
            lineGraph.AddLine(pulseData, Color.blue, "Pulse");
        if (showMove && movementData.Count > 0)
            lineGraph.AddLine(movementData, new Color(0.3f, 0.8f, 0.9f), "Movement");
        if (showSleep && sleepData.Count > 0)
            lineGraph.AddLine(sleepData, Color.yellow, "Sleep");

        lineGraph.DrawGraph();
    }

    void OnToggleAll(bool on) { UpdateGraph(); }
    void OnTogglePulse(bool on) { UpdateGraph(); }
    void OnToggleMovement(bool on) { UpdateGraph(); }
    void OnToggleSleep(bool on) { UpdateGraph(); }

    public void OnRefreshClicked()
    {
        if (!string.IsNullOrEmpty(currentPatientId))
            LoadPatientData(currentPatientId);
        else
            GenerateRandomData();
    }

    public void OnExportReport()
    {
        Debug.Log("=== EXPORT REPORT (Nova) ===");
        if (currentPatientData != null)
            Debug.Log($"Patient: {currentPatientData.patientId} | Pulse: {currentPatientData.pulse} | Move: {currentPatientData.movementMagnitude:F2} | Sleep: {currentPatientData.sleepQualityScore:F2}");
    }

    float RoundToTwoDecimals(float v) => Mathf.Round(v * 100f) / 100f;
    float RoundToIntFloat(float v) => Mathf.Round(v);
}