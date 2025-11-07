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

    [Header("Settings")]
    public float connectionDelay = 1f;
    public float dataUpdateInterval = 2f;

    private bool isConnected = false;
    private bool isUpdatingData = false;

    public System.Action OnDeviceConnected;
    public System.Action OnDataUpdated;

    void Start()
    {
        if (connectButton != null)
        {
            connectButton.onClick.AddListener(OnConnectButtonClicked);
        }

        UpdateConnectionUI();
    }

    void OnConnectButtonClicked()
    {
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
        if (connectionStatusText != null)
        {
            connectionStatusText.text = "Connecting...";
        }

        yield return new WaitForSeconds(connectionDelay);

        isConnected = true;
        UpdateConnectionUI();

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
        isConnected = false;
        isUpdatingData = false;
        UpdateConnectionUI();

        if (connectionStatusText != null)
        {
            connectionStatusText.text = "Disconnected";
            StartCoroutine(HideStatusTextAfterDelay(2f));
        }
    }

    IEnumerator UpdateDataRoutine()
    {
        isUpdatingData = true;

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
}



