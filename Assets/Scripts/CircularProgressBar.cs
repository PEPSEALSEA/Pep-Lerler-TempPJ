using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Circular progress indicators for joint angles
public class CircularProgressBar : MonoBehaviour
{
    [Header("Components")]
    public Image fillImage;
    public TextMeshProUGUI angleText;
    public TextMeshProUGUI labelText;

    [Header("Settings")]
    public float minAngle = 0f;
    public float maxAngle = 180f;
    public Color fillColor = new Color(0.2f, 0.5f, 1f, 1f);

    private float currentAngle = 0f;

    void Awake()
    {
        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Radial360;
            fillImage.fillOrigin = (int)Image.Origin360.Top;
            fillImage.color = fillColor;
        }
    }

    public void SetAngle(float angle, string label = "")
    {
        currentAngle = Mathf.Clamp(angle, minAngle, maxAngle);
        
        if (angleText != null)
        {
            angleText.text = $"{currentAngle:F0}°";
        }

        if (labelText != null && !string.IsNullOrEmpty(label))
        {
            labelText.text = label;
        }

        if (fillImage != null)
        {
            // Normalize angle to 0-1 range based on maxAngle
            fillImage.fillAmount = currentAngle / maxAngle;
        }
    }

    public void SetColor(Color color)
    {
        fillColor = color;
        if (fillImage != null)
        {
            fillImage.color = color;
        }
    }
}
