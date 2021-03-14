using UnityEngine;


namespace TXT.WEAVR.UI
{
    public static class WeavrUIHelper
    {
        private static Vector3[] s_canvasCorners = new Vector3[4];

        /// <summary>
        /// Rescales the specified canvas in world space to be seen with specified pixels on screen
        /// </summary>
        /// <param name="canvas">The canvas to get data from</param>
        /// <param name="parentToScale">The parent to apply scaling</param>
        /// <param name="camera">The camera to get the data from</param>
        /// <param name="pixelsLength">The length of pixels on screen for the width of the canvas</param>
        public static void RescaleWorldCanvasToPixels(Canvas canvas, Transform parentToScale, Camera camera, float pixelsLength)
        {
            var canvasTransform = canvas.transform as RectTransform;
            float width = GetLengthOfPixelsAt(canvasTransform.position, camera, pixelsLength);
            float canvasWidth = canvasTransform.rect.width * canvasTransform.lossyScale.x;
            parentToScale.localScale *= width / Mathf.Max(0.000001f, canvasWidth);
        }

        /// <summary>
        /// Get the world length at specified point in world starting from the amount of pixels seen on camera
        /// </summary>
        /// <param name="point">The point where to compute the length</param>
        /// <param name="cam">The camera used for computation</param>
        /// <param name="pixels">The amount of pixels to be seen on screen</param>
        /// <returns>The length of the pixels in world space at point <paramref name="point"/></returns>
        public static float GetLengthOfPixelsAt(Vector3 point, Camera cam, float pixels)
        {
            float distanceFromCamera = Vector3.Distance(point, cam.transform.position);
            //Vector3 pixel1 = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, distanceFromCamera);
            Vector3 pixel1 = new Vector3(cam.scaledPixelWidth * 0.5f, cam.scaledPixelHeight * 0.5f, distanceFromCamera);
            Vector3 pixel2 = pixel1;
            pixel2.x += pixels;

            Vector3 worldPixel1 = cam.ScreenToWorldPoint(pixel1);
            Vector3 worldPixel2 = cam.ScreenToWorldPoint(pixel2);

            return Vector3.Distance(worldPixel1, worldPixel2);
        }

        /// <summary>
        /// Get the world space size at specified point in world starting from the amount of pixels seen on camera
        /// </summary>
        /// <param name="point">The point where to compute the length</param>
        /// <param name="cam">The camera used for computation</param>
        /// <param name="pixels">The amount of pixels to be seen on screen</param>
        /// <returns>The size of the pixels in world space at point <paramref name="point"/></returns>
        public static Vector3 GetWidthOfPixelsAt(Vector3 point, Camera cam, Vector2 pixels)
        {
            float distanceFromCamera = Vector3.Distance(point, cam.transform.position);
            Vector3 pixel1 = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, distanceFromCamera);
            Vector3 pixel2 = pixel1;
            pixel2.x += pixels.x;
            pixel2.y += pixels.y;

            Vector3 worldPixel1 = cam.ScreenToWorldPoint(pixel1);
            Vector3 worldPixel2 = cam.ScreenToWorldPoint(pixel2);

            return worldPixel2 - worldPixel1;
        }
    }
}
