using UnityEngine;
using System.Collections;
using Nova;

public class DeviceConnectionManager : MonoBehaviour
{
    [Header("UI Components")]
    public Interactable connectButton;
    public TextBlock connectButtonText;
    public TextBlock connectionStatusText;
    public Interactable continueButton;

    [Header("Connection Icon")]
    public UIBlock2D connectionIconImage;
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
        if (connectButton?.UIBlock != null)
            connectButton.UIBlock.AddGestureHandler<Gesture.OnClick>(OnConnectButtonClicked);
        if (continueButton?.UIBlock != null)
        {
            continueButton.UIBlock.AddGestureHandler<Gesture.OnClick>(OnContinueButtonClicked);
            SetInteractable(continueButton, false);
        }
        UpdateConnectionUI();
    }

    void OnConnectButtonClicked(Gesture.OnClick evt)
    {
        if (!isConnected) StartCoroutine(ConnectDevice());
        else DisconnectDevice();
    }

    IEnumerator ConnectDevice()
    {
        if (connectionStatusText != null)
        {
            connectionStatusText.Text = "Connecting...";
            connectionStatusText.TMP.color = Color.yellow;
        }
        SetInteractable(connectButton, false);
        if (animateIconOnConnect && connectionIconImage != null)
            StartCoroutine(PulseIconWhileConnecting());

        yield return new WaitForSeconds(connectionDelay);

        isConnected = true;
        UpdateConnectionUI();
        SetInteractable(connectButton, true);
        SetInteractable(continueButton, true);

        if (connectionStatusText != null)
        {
            connectionStatusText.Text = "Connected!";
            connectionStatusText.TMP.color = new Color(0.4f, 1f, 0.6f);
            StartCoroutine(HideStatusTextAfterDelay(2f));
        }
        OnDeviceConnected?.Invoke();
        if (!isUpdatingData) StartCoroutine(UpdateDataRoutine());
        if (animateIconOnConnect && connectionIconImage != null)
            StartCoroutine(PulseIconOnce());
    }

    void DisconnectDevice()
    {
        isConnected = false;
        isUpdatingData = false;
        if (iconPulseCoroutine != null) StopCoroutine(iconPulseCoroutine);
        UpdateConnectionUI();
        SetInteractable(continueButton, false);
        if (connectionStatusText != null)
        {
            connectionStatusText.Text = "Disconnected";
            connectionStatusText.TMP.color = new Color(1f, 0.5f, 0.3f);
            StartCoroutine(HideStatusTextAfterDelay(2f));
        }
    }

    IEnumerator UpdateDataRoutine()
    {
        isUpdatingData = true;
        while (isConnected)
        {
            yield return new WaitForSeconds(dataUpdateInterval);
            OnDataUpdated?.Invoke();
        }
        isUpdatingData = false;
    }

    IEnumerator HideStatusTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (connectionStatusText != null && isConnected)
            connectionStatusText.Text = "";
    }

    IEnumerator PulseIconWhileConnecting()
    {
        if (connectionIconImage == null) yield break;
        Vector3 orig = connectionIconImage.transform.localScale;
        float t = 0;
        while (!isConnected)
        {
            t += Time.deltaTime * iconPulseSpeed;
            float s = 1f + Mathf.Sin(t * Mathf.PI * 2f) * 0.15f;
            connectionIconImage.transform.localScale = orig * s;
            yield return null;
        }
        connectionIconImage.transform.localScale = orig;
    }

    IEnumerator PulseIconOnce()
    {
        if (connectionIconImage == null) yield break;
        Vector3 orig = connectionIconImage.transform.localScale;
        float d = 0.3f, e = 0;
        while (e < d) { e += Time.deltaTime; connectionIconImage.transform.localScale = orig * Mathf.Lerp(1f, 1.2f, e / d); yield return null; }
        e = 0;
        while (e < d) { e += Time.deltaTime; connectionIconImage.transform.localScale = orig * Mathf.Lerp(1.2f, 1f, e / d); yield return null; }
        connectionIconImage.transform.localScale = orig;
    }

    void UpdateConnectionUI()
    {
        if (connectButtonText != null)
            connectButtonText.Text = isConnected ? "Disconnect" : "Connect Device";

        if (connectButton?.UIBlock != null)
        {
            connectButton.UIBlock.Color = isConnected ? new Color(0.8f, 0.3f, 0.3f) : new Color(0.2f, 0.5f, 1f);
        }

        if (connectionIconImage != null)
        {
            connectionIconImage.SetImage(isConnected ? connectedIconSprite : disconnectedIconSprite);
            connectionIconImage.Color = isConnected ? new Color(0.4f, 1f, 0.6f) : Color.white;
        }
    }

    public bool IsConnected() => isConnected;

    void OnContinueButtonClicked(Gesture.OnClick evt)
    {
        if (!isConnected)
        {
            DisplayTemporaryStatus("Please connect a device first.", Color.yellow);
            return;
        }
        OnContinueToDashboard?.Invoke();
    }

    void DisplayTemporaryStatus(string msg, Color c)
    {
        if (connectionStatusText == null) return;
        connectionStatusText.Text = msg;
        connectionStatusText.TMP.color = c;
        StartCoroutine(HideStatusTextAfterDelay(2f));
    }

    void SetInteractable(Interactable b, bool on) => b.enabled = on;

    void OnDestroy()
    {
        if (iconPulseCoroutine != null) StopCoroutine(iconPulseCoroutine);
    }
}