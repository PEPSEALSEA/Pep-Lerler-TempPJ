using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

// Custom graph system that draws lines using Unity UI
public class LineGraphRenderer : MonoBehaviour
{
    [Header("Graph Settings")]
    public RectTransform graphContainer;
    public float lineWidth = 3f;
    public Color gridColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    public int gridLinesX = 10;
    public int gridLinesY = 10;

    private RectTransform rectTransform;
    private List<GraphLine> lines = new List<GraphLine>();
    private List<GameObject> gridObjects = new List<GameObject>();
    private List<GameObject> lineObjects = new List<GameObject>();

    [System.Serializable]
    public class GraphLine
    {
        public List<float> data;
        public Color color;
        public string label;
    }

    void Awake()
    {
        if (graphContainer == null)
        {
            graphContainer = GetComponent<RectTransform>();
        }
        rectTransform = graphContainer;
    }

    void Start()
    {
        DrawGrid();
    }

    public void ClearLines()
    {
        lines.Clear();
        foreach (GameObject obj in lineObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        lineObjects.Clear();
    }

    public void AddLine(List<float> data, Color color, string label)
    {
        if (data == null || data.Count == 0) return;

        GraphLine line = new GraphLine
        {
            data = new List<float>(data),
            color = color,
            label = label
        };
        lines.Add(line);
    }

    public void DrawGraph()
    {
        // Clear existing lines
        foreach (GameObject obj in lineObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        lineObjects.Clear();

        if (lines.Count == 0)
        {
            Debug.LogWarning("[GRAPH] No lines to draw!");
            return;
        }

        if (rectTransform == null || rectTransform.rect.width <= 0 || rectTransform.rect.height <= 0)
        {
            Debug.LogError("[GRAPH] GraphContainer has invalid size! Width: " + (rectTransform != null ? rectTransform.rect.width.ToString() : "null") + 
                          ", Height: " + (rectTransform != null ? rectTransform.rect.height.ToString() : "null"));
            return;
        }

        // Find min and max values across all lines
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        foreach (GraphLine line in lines)
        {
            if (line.data != null && line.data.Count > 0)
            {
                minValue = Mathf.Min(minValue, line.data.Min());
                maxValue = Mathf.Max(maxValue, line.data.Max());
            }
        }

        // Add some padding
        float valueRange = maxValue - minValue;
        if (valueRange < 0.1f) valueRange = 1f;
        minValue -= valueRange * 0.1f;
        maxValue += valueRange * 0.1f;

        // Draw each line
        int linesDrawn = 0;
        foreach (GraphLine line in lines)
        {
            if (line.data != null && line.data.Count > 0)
            {
                DrawLine(line, minValue, maxValue);
                linesDrawn++;
            }
        }

        Debug.Log($"[GRAPH] Drawn {linesDrawn} line(s) with {lineObjects.Count} segments");
    }

    void DrawLine(GraphLine line, float minValue, float maxValue)
    {
        if (line.data == null || line.data.Count < 1) 
        {
            Debug.LogWarning($"[GRAPH] Cannot draw line '{line.label}' - no data points");
            return;
        }
        
        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;
        
        if (width <= 0 || height <= 0)
        {
            Debug.LogError($"[GRAPH] Invalid graph size: {width}x{height}");
            return;
        }
        
        // If only 1 data point, create a small dot/line
        if (line.data.Count == 1)
        {
            float x = 0 - width / 2;
            float y = ((line.data[0] - minValue) / (maxValue - minValue)) * height - height / 2;
            
            GameObject dot = new GameObject("LineDot");
            dot.transform.SetParent(rectTransform, false);
            RectTransform dotRect = dot.AddComponent<RectTransform>();
            dotRect.anchoredPosition = new Vector2(x, y);
            dotRect.sizeDelta = new Vector2(lineWidth * 2, lineWidth * 2);
            
            Image dotImage = dot.AddComponent<Image>();
            dotImage.color = line.color;
            lineObjects.Add(dot);
            return;
        }
        
        float stepX = width / (line.data.Count - 1);
        int segmentsCreated = 0;

        // Create line segments using UI Images
        for (int i = 0; i < line.data.Count - 1; i++)
        {
            float x1 = i * stepX - width / 2;
            float y1 = ((line.data[i] - minValue) / (maxValue - minValue)) * height - height / 2;
            float x2 = (i + 1) * stepX - width / 2;
            float y2 = ((line.data[i + 1] - minValue) / (maxValue - minValue)) * height - height / 2;

            // Skip if points are too close (will cause issues)
            if (Mathf.Abs(x2 - x1) < 0.1f && Mathf.Abs(y2 - y1) < 0.1f)
                continue;

            // Create a line segment
            GameObject segment = new GameObject("LineSegment_" + i);
            segment.transform.SetParent(rectTransform, false);
            RectTransform segmentRect = segment.AddComponent<RectTransform>();
            
            // Calculate position and rotation
            Vector2 midPoint = new Vector2((x1 + x2) / 2, (y1 + y2) / 2);
            float distance = Vector2.Distance(new Vector2(x1, y1), new Vector2(x2, y2));
            
            // Skip if distance is too small
            if (distance < 0.1f)
            {
                Destroy(segment);
                continue;
            }
            
            float angle = Mathf.Atan2(y2 - y1, x2 - x1) * Mathf.Rad2Deg;

            segmentRect.anchoredPosition = midPoint;
            segmentRect.sizeDelta = new Vector2(distance, lineWidth);
            segmentRect.localRotation = Quaternion.Euler(0, 0, angle);

            Image segmentImage = segment.AddComponent<Image>();
            segmentImage.color = line.color;
            
            // Make sure the image is visible
            segmentImage.raycastTarget = false;

            lineObjects.Add(segment);
            segmentsCreated++;
        }
        
        if (segmentsCreated == 0)
        {
            Debug.LogWarning($"[GRAPH] No segments created for line '{line.label}'");
        }
    }

    void DrawGrid()
    {
        // Clear existing grid
        foreach (GameObject obj in gridObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        gridObjects.Clear();

        if (rectTransform == null) return;

        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;

        // Draw vertical grid lines
        for (int i = 0; i <= gridLinesX; i++)
        {
            GameObject line = new GameObject("GridLine_V_" + i);
            line.transform.SetParent(rectTransform, false);
            RectTransform lineRect = line.AddComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(i / (float)gridLinesX, 0);
            lineRect.anchorMax = new Vector2(i / (float)gridLinesX, 1);
            lineRect.sizeDelta = new Vector2(1, 0);
            lineRect.anchoredPosition = Vector2.zero;

            Image lineImage = line.AddComponent<Image>();
            lineImage.color = gridColor;
            gridObjects.Add(line);
        }

        // Draw horizontal grid lines
        for (int i = 0; i <= gridLinesY; i++)
        {
            GameObject line = new GameObject("GridLine_H_" + i);
            line.transform.SetParent(rectTransform, false);
            RectTransform lineRect = line.AddComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0, i / (float)gridLinesY);
            lineRect.anchorMax = new Vector2(1, i / (float)gridLinesY);
            lineRect.sizeDelta = new Vector2(0, 1);
            lineRect.anchoredPosition = Vector2.zero;

            Image lineImage = line.AddComponent<Image>();
            lineImage.color = gridColor;
            gridObjects.Add(line);
        }
    }
}
