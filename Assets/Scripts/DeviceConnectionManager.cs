using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Nova;

public class DeviceConnectionManager : MonoBehaviour
{
    [Header("UI Components")]
    public UIBlock connectButton;
    public TextBlock connectButtonText;
    public TextBlock connectionStatusText;
    public UIBlock continueButton;

    [Header("Connection Icon")]
    public UIBlock connectionIconImage;
    public Sprite disconnectedIconSprite;
    public Sprite connectedIconSprite;

    [Header("Settings")]
    public float connectionDelay = 1f;
    public float dataUpdateInterval = 2f;
    public bool animateIconOnConnect = true;
    public float iconPulseSpeed = 2f;

    private bool isConnected = false;
    private bool isUpdatingData = false;
    private Coroutine iconPulseCoroutine;

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

        // Show connecting status
        if (connectionStatusText != null)
        {
            connectionStatusText.text = "Connecting...";
            connectionStatusText.color = Color.yellow;
        }

        // Disable button during connection
        if (connectButton != null)
        {
            connectButton.interactable = false;
        }

        // Start pulsing animation during connection
        if (animateIconOnConnect && connectionIconImage != null)
        {
            StartCoroutine(PulseIconWhileConnecting());
        }

        yield return new WaitForSeconds(connectionDelay);

        isConnected = true;
        UpdateConnectionUI();

        // Re-enable button
        if (connectButton != null)
        {
            connectButton.interactable = true;
        }

        if (continueButton != null)
        {
            continueButton.interactable = true;
        }

        UIDebug.Log(nameof(DeviceConnectionManager), "Device connected");

        if (connectionStatusText != null)
        {
            connectionStatusText.text = "Connected!";
            connectionStatusText.color = new Color(0.4f, 1f, 0.6f); // Green
            StartCoroutine(HideStatusTextAfterDelay(2f));
        }

        OnDeviceConnected?.Invoke();

        // Start updating data
        if (!isUpdatingData)
        {
            StartCoroutine(UpdateDataRoutine());
        }

        // Optional: Pulse icon briefly on successful connection
        if (animateIconOnConnect && connectionIconImage != null)
        {
            StartCoroutine(PulseIconOnce());
        }
    }

    void DisconnectDevice()
    {
        UIDebug.Log(nameof(DeviceConnectionManager), "Device disconnected");
        isConnected = false;
        isUpdatingData = false;

        // Stop any icon animations
        if (iconPulseCoroutine != null)
        {
            StopCoroutine(iconPulseCoroutine);
            iconPulseCoroutine = null;
        }

        UpdateConnectionUI();

        if (continueButton != null)
        {
            continueButton.interactable = false;
        }

        if (connectionStatusText != null)
        {
            connectionStatusText.text = "Disconnected";
            connectionStatusText.color = new Color(1f, 0.5f, 0.3f); // Orange
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

    IEnumerator PulseIconWhileConnecting()
    {
        if (connectionIconImage == null) yield break;

        float timer = 0f;
        Vector3 originalScale = connectionIconImage.transform.localScale;

        while (!isConnected)
        {
            timer += Time.deltaTime * iconPulseSpeed;
            float scale = 1f + Mathf.Sin(timer * Mathf.PI * 2f) * 0.15f;
            connectionIconImage.transform.localScale = originalScale * scale;
            yield return null;
        }

        // Reset scale
        connectionIconImage.transform.localScale = originalScale;
    }

    IEnumerator PulseIconOnce()
    {
        if (connectionIconImage == null) yield break;

        Vector3 originalScale = connectionIconImage.transform.localScale;
        float duration = 0.3f;
        float elapsed = 0f;

        // Scale up
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(1f, 1.2f, elapsed / duration);
            connectionIconImage.transform.localScale = originalScale * scale;
            yield return null;
        }

        elapsed = 0f;

        // Scale back down
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(1.2f, 1f, elapsed / duration);
            connectionIconImage.transform.localScale = originalScale * scale;
            yield return null;
        }

        connectionIconImage.transform.localScale = originalScale;
    }

    void UpdateConnectionUI()
    {
        // Update button text
        if (connectButtonText != null)
        {
            connectButtonText.text = isConnected ? "Disconnect" : "Connect Device";
        }

        // Update button color
        if (connectButton != null)
        {
            ColorBlock colors = connectButton.colors;
            colors.normalColor = isConnected ? new Color(0.8f, 0.3f, 0.3f) : new Color(0.2f, 0.5f, 1f);
            connectButton.colors = colors;
        }

        // Update connection icon sprite
        if (connectionIconImage != null)
        {
            if (isConnected && connectedIconSprite != null)
            {
                connectionIconImage.sprite = connectedIconSprite;
                UIDebug.Log(nameof(DeviceConnectionManager), "Icon changed to CONNECTED sprite");
            }
            else if (!isConnected && disconnectedIconSprite != null)
            {
                connectionIconImage.sprite = disconnectedIconSprite;
                UIDebug.Log(nameof(DeviceConnectionManager), "Icon changed to DISCONNECTED sprite");
            }

            // Optional: Change icon color based on connection status
            connectionIconImage.color = isConnected ? new Color(0.4f, 1f, 0.6f) : Color.white;
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

    void OnDestroy()
    {
        // Clean up coroutines
        if (iconPulseCoroutine != null)
        {
            StopCoroutine(iconPulseCoroutine);
        }
    }
}