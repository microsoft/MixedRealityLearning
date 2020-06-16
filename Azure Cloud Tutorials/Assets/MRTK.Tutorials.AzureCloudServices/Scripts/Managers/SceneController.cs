using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MRTK.Tutorials.AzureCloudServices.Scripts.Domain;
using MRTK.Tutorials.AzureCloudServices.Scripts.Utilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Windows.WebCam;

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
        private DataManager dataManager;
        [SerializeField]
        private ObjectDetectionManager objectDetectionManager;
        [SerializeField]
        private AnchorManager anchorManager;

        [Header("Misc Settings")]
        [SerializeField]
        private GameObject mainMenu;

        [Header("Events")]
        [SerializeField]
        private UnityEvent onCameraStarted;
        [SerializeField]
        private UnityEvent onCameraStopped;

        private PhotoCapture photoCapture;
        
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
        }

        /// <summary>
        /// Stop camera the camera.
        /// </summary>
        public void StopCamera()
        {
            if (!IsCameraActive || photoCapture == null)
            {
                return;
            }

            photoCapture.StopPhotoModeAsync(result =>
            {
                if (result.success)
                {
                    IsCameraActive = false;
                    onCameraStopped?.Invoke();
                }
            });
        }

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
        
        /// <summary>
        /// Take a photo from the WebCam. Make sure the camera is active.
        /// </summary>
        /// <returns>Image data encoded as jpg.</returns>
        public Task<byte[]> TakePhoto()
        {
            if (!IsCameraActive || photoCapture == null)
            {
                throw new Exception("Can't take photo when camera is not ready.");
            }

            return Task.Run(() =>
            {
                var completionSource = new TaskCompletionSource<byte[]>();

                AppDispatcher.Instance().Enqueue(() =>
                {
                    Debug.Log("Starting photo capture.");

                    photoCapture.TakePhotoAsync((photoCaptureResult, frame) =>
                    {
                        Debug.Log("Photo capture done.");

                        var buffer = new List<byte>();
                        frame.CopyRawImageDataIntoBuffer(buffer);
                        completionSource.TrySetResult(buffer.ToArray());
                    });
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
            if (!IsCameraActive || photoCapture == null)
            {
                throw new Exception("Can't take photo when camera is not ready.");
            }

            return Task.Run(() =>
            {
                var completionSource = new TaskCompletionSource<ImageThumbnail>();

                AppDispatcher.Instance().Enqueue(() =>
                {
                    Debug.Log("Starting photo capture.");

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
