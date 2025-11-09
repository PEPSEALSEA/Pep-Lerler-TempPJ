using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DeviceConnectionManager : MonoBehaviour
{
    [Header("UI Components")]
    public Button connectButton;
    public TextMeshProUGUI connectButtonText;
    public TextMeshProUGUI connectionStatusText;
    public Button continueButton;

    [Header("Settings")]
    public float connectionDelay = 1f;
    public float dataUpdateInterval = 2f;

    private bool isConnected = false;
    private bool isUpdatingData = false;

    public System.Action OnDeviceConnected;
    public System.Action OnDataUpdated;
    public System.Action OnContinueToDashboard;

    void Start()
    {
        if (connectButton != null)
        {
            connectButton.onClick.AddListener(OnConnectButtonClicked);
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
            continueButton.interactable = false;
        }

        UpdateConnectionUI();
        UIDebug.Log(nameof(DeviceConnectionManager), "Initialized Device Connection Manager");
    }

    void OnConnectButtonClicked()
    {
        UIDebug.Log(nameof(DeviceConnectionManager), $"Connect button clicked | Connected = {isConnected}");
        if (!isConnected)
        {
            StartCoroutine(ConnectDevice());
        }
        else
        {
            DisconnectDevice();
        }
    }

    IEnumerator ConnectDevice()
    {
        UIDebug.Log(nameof(DeviceConnectionManager), "Connecting device...");
        if (connectionStatusText != null)
        {
            connectionStatusText.text = "Connecting...";
        }

        yield return new WaitForSeconds(connectionDelay);

        isConnected = true;
        UpdateConnectionUI();

        if (continueButton != null)
        {
            continueButton.interactable = true;
        }
        UIDebug.Log(nameof(DeviceConnectionManager), "Device connected");

        if (connectionStatusText != null)
        {
            connectionStatusText.text = "Connected!";
            StartCoroutine(HideStatusTextAfterDelay(2f));
        }

        OnDeviceConnected?.Invoke();

        // Start updating data
        if (!isUpdatingData)
        {
            StartCoroutine(UpdateDataRoutine());
        }
    }

    void DisconnectDevice()
    {
        UIDebug.Log(nameof(DeviceConnectionManager), "Device disconnected");
        isConnected = false;
        isUpdatingData = false;
        UpdateConnectionUI();

        if (continueButton != null)
        {
            continueButton.interactable = false;
        }

        if (connectionStatusText != null)
        {
            connectionStatusText.text = "Disconnected";
            StartCoroutine(HideStatusTextAfterDelay(2f));
        }
    }

    IEnumerator UpdateDataRoutine()
    {
        isUpdatingData = true;
        UIDebug.Log(nameof(DeviceConnectionManager), "Starting data update routine");

        while (isConnected)
        {
            yield return new WaitForSeconds(dataUpdateInterval);
            
            if (isConnected)
            {
                // Generate random data and notify
                OnDataUpdated?.Invoke();
            }
        }

        isUpdatingData = false;
        UIDebug.Log(nameof(DeviceConnectionManager), "Data update routine stopped");
    }

    IEnumerator HideStatusTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (connectionStatusText != null && isConnected)
        {
            connectionStatusText.text = "";
        }
    }

    void UpdateConnectionUI()
    {
        if (connectButtonText != null)
        {
            connectButtonText.text = isConnected ? "Disconnect" : "Connect Device";
        }

        if (connectButton != null)
        {
            // Change button color based on connection status
            ColorBlock colors = connectButton.colors;
            colors.normalColor = isConnected ? new Color(0.8f, 0.3f, 0.3f) : new Color(0.2f, 0.5f, 1f);
            connectButton.colors = colors;
        }
    }

    public bool IsConnected()
    {
        return isConnected;
    }

    void OnContinueButtonClicked()
    {
        UIDebug.Log(nameof(DeviceConnectionManager), $"Continue clicked | Connected = {isConnected}");
        if (!isConnected)
        {
            DisplayTemporaryStatus("Please connect a device first.", Color.yellow);
            return;
        }

        OnContinueToDashboard?.Invoke();
    }

    void DisplayTemporaryStatus(string message, Color color)
    {
        if (connectionStatusText == null) return;

        connectionStatusText.text = message;
        connectionStatusText.color = color;
        StartCoroutine(HideStatusTextAfterDelay(2f));
    }
}
