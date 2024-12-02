using UnityEngine;
using UnityEngine.UIElements;

// Static class to hold extension methods
public static class ExtensionMethods
{
    /// <summary>
    /// Resets the Transform's position, rotation, and scale to their default values.
    /// </summary>
    /// <param name="trans">The Transform to reset.</param>
    public static void ResetTransformation(this Transform trans)
    {
        trans.position = Vector3.zero;
        trans.localRotation = Quaternion.identity;
        trans.localScale = new Vector3(1, 1, 1);
    }

    /// <summary>
    /// Returns the world space position of the center of a VisualElement.
    /// </summary>
    /// <param name="visualElement">The VisualElement to calculate the center for.</param>
    /// <param name="camera">The Camera used for conversion (optional, defaults to Camera.main).</param>
    /// <param name="zDepth">The Z-depth to place the element in world space (default is 10).</param>
    /// <returns>The world space position of the center of the VisualElement.</returns>
    public static Vector3 GetWorldPosition(this VisualElement visualElement, Camera camera = null, float zDepth = 10f)
    {
        if (camera == null)
            camera = Camera.main;

        Vector3 worldPos = Vector3.zero;

        if (camera == null || visualElement == null)
            return worldPos;

        return visualElement.worldBound.center.ScreenPosToWorldPos(camera, zDepth);
    }

    /// <summary>
    /// Converts a screen position from UI Toolkit to world space coordinates.
    /// </summary>
    /// <param name="screenPos">The screen position to convert.</param>
    /// <param name="camera">The Camera used for conversion (optional, defaults to Camera.main).</param>
    /// <param name="zDepth">The Z-depth for the world space position (default is 10).</param>
    /// <returns>The world space position corresponding to the screen position.</returns>
    public static Vector3 ScreenPosToWorldPos(this Vector2 screenPos, Camera camera = null, float zDepth = 10f)
    {

        if (camera == null)
            camera = Camera.main;

        if (camera == null)
            return Vector2.zero;

        float xPos = screenPos.x;
        float yPos = screenPos.y;
        Vector3 worldPos = Vector3.zero;

        if (!float.IsNaN(screenPos.x) && !float.IsNaN(screenPos.y) && !float.IsInfinity(screenPos.x) && !float.IsInfinity(screenPos.y))
        {
            // convert to world space position using Camera class
            Vector3 screenCoord = new Vector3(xPos, yPos, zDepth);
            worldPos = camera.ScreenToWorldPoint(screenCoord);
        }
        return worldPos;
    }

    /// <summary>
    /// Converts a UI Toolkit ClickEvent position to screen coordinates in pixels.
    /// </summary>
    /// <param name="clickPosition">The ClickEvent position to convert.</param>
    /// <param name="rootVisualElement">The root VisualElement for the UI hierarchy.</param>
    /// <returns>The screen coordinates in pixels.</returns>
    public static Vector2 GetScreenCoordinate(this Vector2 clickPosition, VisualElement rootVisualElement)
    {
        // Adjust the clickPosition for the borders (for the SafeAreaBorder)
        float borderLeft = rootVisualElement.resolvedStyle.borderLeftWidth;
        float borderTop = rootVisualElement.resolvedStyle.borderTopWidth;
        clickPosition.x += borderLeft;
        clickPosition.y += borderTop;

        // Normalize the UI Toolkit position to account for Panel Match settings
        Vector2 normalizedPosition = clickPosition.NormalizeClickEventPosition(rootVisualElement);

        // Multiply by Screen dimensions to get screen coordinates in pixels
        float xValue = normalizedPosition.x * Screen.width;
        float yValue = normalizedPosition.y * Screen.height;
        return new Vector2(xValue, yValue);
    }

    /// <summary>
    /// Normalizes a ClickEvent position to a (0, 0) to (1, 1) range within the root VisualElement.
    /// </summary>
    /// <param name="clickPosition">The ClickEvent position to normalize.</param>
    /// <param name="rootVisualElement">The root VisualElement for normalization reference.</param>
    /// <returns>The normalized position in the range (0,0) to (1,1).</returns>
    public static Vector2 NormalizeClickEventPosition(this Vector2 clickPosition, VisualElement rootVisualElement)
    {
        // Get a Rect that represents the boundaries of the screen in UI Toolkit
        Rect rootWorldBound = rootVisualElement.worldBound;

        float normalizedX = clickPosition.x / rootWorldBound.xMax;

        // Flip the y value so y = 0 is at the bottom of the screen
        float normalizedY = 1 - clickPosition.y / rootWorldBound.yMax;

        return new Vector2(normalizedX, normalizedY);

    }

    /// <summary>
    /// Aligns a VisualElement to a specified world position.
    /// </summary>
    /// <param name="element">The VisualElement to move.</param>
    /// <param name="worldPosition">The target world position.</param>
    /// <param name="worldSize">The size of the world object being aligned to.</param>
    public static void MoveToWorldPosition(this VisualElement element, Vector3 worldPosition, Vector2 worldSize)
    {
        Rect rect = RuntimePanelUtils.CameraTransformWorldToPanelRect(element.panel, worldPosition, worldSize, Camera.main);
        element.transform.position = rect.position;
    }

    // Keeps a VisualElement within the camera viewport
    public static void ClampToScreenBounds(this VisualElement element, Camera camera = null)
    {
        camera ??= Camera.main;
        if (camera == null || element == null)
            return;

        // Calculate bounding rectangle for the entire hierarchy
        Rect boundingRect = new Rect(element.worldBound.position, element.worldBound.size);

        //// Extend the boundaries for any child elements
        foreach (VisualElement child in element.Children())
        {
            Rect childRect = child.worldBound;
            boundingRect.xMin = Mathf.Min(boundingRect.xMin, childRect.xMin);
            boundingRect.xMax = Mathf.Max(boundingRect.xMax, childRect.xMax);
            boundingRect.yMin = Mathf.Min(boundingRect.yMin, childRect.yMin);
            boundingRect.yMax = Mathf.Max(boundingRect.yMax, childRect.yMax);
        }

        Vector3 viewportPosition = camera.WorldToViewportPoint(boundingRect.center);

        // Clamp to screen space, considering the bounding rectangle dimensions
        viewportPosition.x = Mathf.Clamp(viewportPosition.x, boundingRect.width / 2 / Screen.width, 1 - boundingRect.width / 2 / Screen.width);
        viewportPosition.y = Mathf.Clamp(viewportPosition.y, boundingRect.height / 2 / Screen.height, 1 - boundingRect.height / 2 / Screen.height);

        // Convert back to world position and set
        Vector3 newWorldPosition = camera.ViewportToWorldPoint(viewportPosition);
       
        Vector3 offset = newWorldPosition - new Vector3(boundingRect.center.x, boundingRect.center.y, newWorldPosition.z);

        element.transform.position += offset;
    }

    /// <summary>
    /// Sets the height of the VisualElement based on its width to maintain the given aspect ratio.
    /// </summary>
    /// <param name="element">The VisualElement to adjust.</param>
    /// <param name="aspectRatio">The desired width-to-height ratio (e.g., 1.0 for a square).</param>
    public static void SetAspectRatio(this VisualElement element, float aspectRatio)
    {
        // Register callback for when the element's geometry changes (e.g., resizing)
        element.RegisterCallback<GeometryChangedEvent>(evt =>
        {
            // Set the height to be the width divided by the aspect ratio
            element.style.height = element.resolvedStyle.width / aspectRatio;
        });
    }
    
    public static void AlignToGameObject(this VisualElement element, GameObject gameObject, Camera camera, VisualElement rootElement)
    {
        if (gameObject == null || camera == null || rootElement == null)
            return;

        // Get the GameObject's world position and convert it to screen coordinates
        Vector3 worldPos = gameObject.transform.position;
        Vector3 screenPos = camera.WorldToScreenPoint(worldPos);

        // Convert the screen position into the local position of the root element
        Vector2 localPos = ScreenToLocal(rootElement, screenPos);

        // Center the element
        element.style.left = localPos.x - (element.resolvedStyle.width / 2); 
        element.style.top = localPos.y - (element.resolvedStyle.height / 2); 
    }
    
    /// <summary>
    /// Converts screen coordinates to local coordinates within a root VisualElement.
    /// </summary>
    /// <param name="rootElement">The root VisualElement target.</param>
    /// <param name="screenPos">The screen position to convert.</param>
    /// <returns>The local position inside the root element.</returns>
    private static Vector2 ScreenToLocal(VisualElement rootElement, Vector3 screenPos)
    {
        // Invert the Y coordinate; screen coordinates have origin at bottom-left and UI is top-left
        screenPos.y = Screen.height - screenPos.y;

        // Convert screen position to the local position within the root element
        return rootElement.WorldToLocal(screenPos);
    }
    
    /// <summary>
    /// Recursively clears the dataSource of the VisualElement and all its children.
    /// </summary>
    /// <param name="element">The root VisualElement to clear data sources from.</param>
    public static void ClearDataSourcesRecursive(this VisualElement element)
    {
        if (element == null)
        {
            UnityEngine.Debug.LogWarning("[ExtensionMethods] ClearDataSourcesRecursive: Passed VisualElement is null.");
            return;
        }

        // Clear the dataSource of the current element
        if (element.dataSource != null)
        {
            element.dataSource = null;
            UnityEngine.Debug.Log($"[ExtensionMethods] Cleared dataSource for VisualElement '{element.name}'.");
        }

        // Recursively clear dataSources for all child elements
        foreach (var child in element.Children())
        {
            child.ClearDataSourcesRecursive();
        }
    }
}

