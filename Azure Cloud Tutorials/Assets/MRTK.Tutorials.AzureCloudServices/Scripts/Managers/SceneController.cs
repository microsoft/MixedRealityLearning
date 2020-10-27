using System;
using System.Threading.Tasks;
using MRTK.Tutorials.AzureCloudServices.Scripts.Domain;
using MRTK.Tutorials.AzureCloudServices.Scripts.Utilities;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_WSA
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Windows.WebCam;
#endif

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Managers
{
    public class SceneController : MonoBehaviour
    {
        public bool IsCameraActive { private set; get; }
        public Project CurrentProject { get; private set; }
        public DataManager DataManager => dataManager;
        public ObjectDetectionManager ObjectDetectionManager => objectDetectionManager;
        public AnchorManager AnchorManager => anchorManager;
        
        [Header("Managers")]
        [SerializeField]
        private DataManager dataManager = default;
        [SerializeField]
        private ObjectDetectionManager objectDetectionManager = default;
        [SerializeField]
        private AnchorManager anchorManager = default;

        [Header("Misc Settings")]
        [SerializeField]
        private GameObject mainMenu = default;

        [Header("Events")]
        [SerializeField]
        private UnityEvent onCameraStarted = default;
        [SerializeField]
        private UnityEvent onCameraStopped = default;

#if UNITY_WSA
        private PhotoCapture photoCapture;
#else
        private WebCamTexture webCamTexture; 
#endif
        
        private void Start()
        {
            OpenMainMenu();
        }

        // Should be called from DataManager ready callback to ensure DB is ready.
        public async void Init()
        {
            if (CurrentProject == null)
            {
                CurrentProject = await dataManager.GetOrCreateProject();
            }
        }

        /// <summary>
        /// Start the camera to use for custom vision.
        /// </summary>
        public void StartCamera()
        {
            if (IsCameraActive)
            {
                return;
            }

            Debug.Log("Starting camera system.");
#if UNITY_WSA
            if (photoCapture == null)
            {
                PhotoCapture.CreateAsync(false, captureObject =>
                {
                    photoCapture = captureObject;
                    StartPhotoMode();
                });
            }
            else
            {
                StartPhotoMode();
            }
#else
            if (webCamTexture == null)
            {
                webCamTexture = new WebCamTexture();
                var webcamRenderer = gameObject.AddComponent<MeshRenderer>();
                webcamRenderer.material = new Material(Shader.Find("Standard"));
                webcamRenderer.material.mainTexture = webCamTexture;
                webCamTexture.Play();
            }
            else if (!webCamTexture.isPlaying)
            {
                webCamTexture.Play();
            }
#endif

            IsCameraActive = true;
        }

        /// <summary>
        /// Stop camera the camera.
        /// </summary>
        public void StopCamera()
        {
            if (!IsCameraActive)
            {
                return;
            }

            Debug.Log("Stopping camera system.");
#if UNITY_WSA
            photoCapture.StopPhotoModeAsync(result =>
            {
                if (result.success)
                {
                    IsCameraActive = false;
                    onCameraStopped?.Invoke();
                }
            });
#else
            webCamTexture.Stop();
#endif
            IsCameraActive = false;
        }

#if UNITY_WSA
        private void StartPhotoMode()
        {
            var cameraResolution = PhotoCapture.SupportedResolutions
                .OrderByDescending((res) => res.width * res.height)
                .First();
            
            var cameraParams = new CameraParameters()
            {
                hologramOpacity = 0f,
                cameraResolutionWidth = cameraResolution.width,
                cameraResolutionHeight = cameraResolution.height,
                pixelFormat = CapturePixelFormat.JPEG
            };

            photoCapture.StartPhotoModeAsync(cameraParams, startResult =>
            {
                Debug.Log($"Camera system start result = {startResult.resultType}.");
                IsCameraActive = startResult.success;
                onCameraStarted?.Invoke();
            });
        }
#endif

        /// <summary>
        /// Take a photo from the WebCam. Make sure the camera is active.
        /// </summary>
        /// <returns>Image data encoded as jpg.</returns>
        public Task<byte[]> TakePhoto()
        {
            if (!IsCameraActive)
            {
                throw new Exception("Can't take photo when camera is not ready.");
            }

            return Task.Run(() =>
            {
                var completionSource = new TaskCompletionSource<byte[]>();
            
                AppDispatcher.Instance().Enqueue(() =>
                {
                    Debug.Log("Starting photo capture.");
                    
#if UNITY_WSA
                    photoCapture.TakePhotoAsync((photoCaptureResult, frame) =>
                    {
                        Debug.Log("Photo capture done.");
            
                        var buffer = new List<byte>();
                        frame.CopyRawImageDataIntoBuffer(buffer);
                        completionSource.TrySetResult(buffer.ToArray());
                    });
#else
                    var tex = new Texture2D(webCamTexture.width, webCamTexture.height);
                    tex.SetPixels(webCamTexture.GetPixels());
                    tex.Apply();
                    var data = tex.EncodeToPNG();
                    completionSource.TrySetResult(data);
#endif
                });
                
                return completionSource.Task;
            });
        }
        
        /// <summary>
        /// Take a photo from the WebCam. Make sure the camera is active.
        /// </summary>
        /// <returns>Image data with a Texture for thumbnail.</returns>
        public Task<ImageThumbnail> TakePhotoWithThumbnail()
        {
            if (!IsCameraActive)
            {
                throw new Exception("Can't take photo when camera is not ready.");
            }

            return Task.Run(() =>
            {
                var completionSource = new TaskCompletionSource<ImageThumbnail>();

                AppDispatcher.Instance().Enqueue(() =>
                {
                    Debug.Log("Starting photo capture.");
                    
#if UNITY_WSA
                    photoCapture.TakePhotoAsync((photoCaptureResult, frame) =>
                    {
                        Debug.Log("Photo capture done.");

                        var buffer = new List<byte>();
                        frame.CopyRawImageDataIntoBuffer(buffer);
                        var texture = new Texture2D(2, 2);
                        var imageData = buffer.ToArray();
                        texture.LoadImage(imageData);
                        var imageThumbnail = new ImageThumbnail
                        {
                            ImageData = imageData,
                            Texture = texture
                        };
                        
                        completionSource.TrySetResult(imageThumbnail);
                    });
#else
                    var tex = new Texture2D(webCamTexture.width, webCamTexture.height);
                    tex.SetPixels(webCamTexture.GetPixels());
                    tex.Apply();
                    var data = tex.EncodeToPNG();
                    var imageThumbnail = new ImageThumbnail
                    {
                        ImageData = data,
                        Texture = tex
                    };
                    
                    completionSource.TrySetResult(imageThumbnail);
#endif
                });
                
                return completionSource.Task;
            });
        }

        public void OpenMainMenu()
        {
            mainMenu?.SetActive(true);
        }
    }
}
