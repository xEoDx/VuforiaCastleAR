using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class CastleController : MonoBehaviour {



    #region PRIVATE_MEMBERS

    [SerializeField]
    private UserDefinedTargetHandler udtEventHandler;
   
    /*[SerializeField]
    private GameObject msgGetCloser;*/
    [SerializeField]
    private GameObject msgModeDT;
    [SerializeField]
    private GameObject msgModeUDT;

    private enum TrackingMode
    {
        DEVICE_ORIENTATION,
        CONSTRAINED_TO_CAMERA,
        UDT_BASED,
    }

    // initial mode
    private TrackingMode mTrackingMode = TrackingMode.DEVICE_ORIENTATION;

    private Vector3 mPosOffsetAtTargetCreation;

    private const float mInitialDistance = 2.5f;

    private bool mBuildingUDT = false;

    private Camera cam;

    private GameObject penguinModel;
    private GameObject penguinShadow;
    private bool startCheckingFrameQualityForNewUDT;

    #endregion //PRIVATE_MEMBERS

    public void StartUDTTracker()
    {
        if (!startCheckingFrameQualityForNewUDT)
            startCheckingFrameQualityForNewUDT = true;
    }

    #region MONOBEHAVIOUR_METHODS

    void Awake()
    {
        //msgGetCloser.SetActive(false);

        msgModeDT.SetActive(true);
        msgModeUDT.SetActive(false);
        startCheckingFrameQualityForNewUDT = false;
    }

    void Start()
    {
        mTrackingMode = TrackingMode.DEVICE_ORIENTATION;

        VuforiaARController.Instance.RegisterVuforiaStartedCallback(OnVuforiaStarted);

        udtEventHandler.ShowQualityIndicator(false); // don't show at start

        penguinModel = GameObject.Find("PenguinModel");
        penguinShadow = GameObject.Find("Penguin_Shadow");
    }

    void Update()
    {
        if (startCheckingFrameQualityForNewUDT)
        {
            ChangeMode();
        }
    }
    /*
    void Update()
    {
        if (CheckTapOnObject())
        {
            ChangeMode();
        }

        if ((Screen.orientation == ScreenOrientation.Portrait) && (Screen.width < Screen.height))
        {
            msgModeDT.GetComponent<RectTransform>().anchoredPosition = new Vector2(0.0f, 150.0f);
            msgModeUDT.GetComponent<RectTransform>().anchoredPosition = new Vector2(0.0f, 150.0f);
        }
        else
        {
            msgModeDT.GetComponent<RectTransform>().anchoredPosition = new Vector2(0.0f, 30.0f);
            msgModeUDT.GetComponent<RectTransform>().anchoredPosition = new Vector2(0.0f, 30.0f);
        }
    }*/

    void LateUpdate()
    {

        switch (mTrackingMode)
        {
            case TrackingMode.CONSTRAINED_TO_CAMERA:
                {
                    // In this phase, the Penguin is constrained to remain
                    // in the camera view, so it follows the user motion
                    Vector3 constrainedPos = cam.transform.position + cam.transform.forward * mInitialDistance;
                    this.transform.position = constrainedPos;

                    // Update object rotation so that it always look towards the camera
                    // and its "up vector" is always aligned with the gravity direction.
                    // NOTE: since we are using DeviceTracker, the World up vector is guaranteed 
                    // to be aligned (approximately) with the real world gravity direction 
                    //RotateToLookAtCamera();

                    // Check if we were waiting for a UDT creation,
                    // and switch mode if UDT was created
                    if (mBuildingUDT && udtEventHandler && udtEventHandler.TargetCreated)
                    {

                        ImageTargetBehaviour trackedTarget = GetActiveTarget();

                        if (trackedTarget != null)
                        {
                            mBuildingUDT = false;

                            // Switch mode to UDT based tracking
                            mTrackingMode = TrackingMode.UDT_BASED;

                            // Update header text
                            //DisplayMessage(msgGetCloser);
                            DisplayModeLabel(msgModeUDT);

                            // Hide quality indicator
                            udtEventHandler.ShowQualityIndicator(false);

                            // Show the penguin
                            ShowPenguin(true);                            

                            // Save a snapshot of the current position offset
                            // between the object and the target center
                            mPosOffsetAtTargetCreation = this.transform.position - trackedTarget.transform.position;
                        }
                    }
                }
                break;
            case TrackingMode.UDT_BASED:
                {
                    // Update the object world position according to the UDT target position
                    ImageTargetBehaviour trackedTarget = GetActiveTarget();
                    if (trackedTarget != null)
                    {
                        this.transform.position = trackedTarget.transform.position + mPosOffsetAtTargetCreation;
                    }

                    // Update object rotation so that it always look towards the camera
                    // and its "up vector" is always aligned with the gravity direction.
                    // NOTE: since we are using DeviceTracker, the World up vector is guaranteed 
                    // to be aligned (approximately) with the real world gravity direction 
                    //RotateToLookAtCamera();
                }
                break;
        }
    }

    #endregion //MONOBEHAVIOUR_METHODS



    #region PUBLIC_METHODS

    public void ResetState()
    {
        mTrackingMode = TrackingMode.DEVICE_ORIENTATION;
        mBuildingUDT = false;

        
        DisplayModeLabel(msgModeDT);

        // Hide the quality indicator
        udtEventHandler.ShowQualityIndicator(false);

        // Show the penguin
        ShowPenguin(true);

       
    }

    #endregion //PUBLIC_METHODS


    #region PRIVATE_METHODS

    // Callback called when Vuforia has started
    private void OnVuforiaStarted()
    {
        cam = Vuforia.DigitalEyewearARController.Instance.PrimaryCamera ?? Camera.main;

        StartCoroutine(ResetAfter(0.5f));
    }

    private IEnumerator ResetAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        ResetState();
    }

    private void ChangeMode()
    {
        
        if (mTrackingMode == TrackingMode.DEVICE_ORIENTATION)
        {
            SwitchToCameraMode();
        }
        else if (mTrackingMode == TrackingMode.CONSTRAINED_TO_CAMERA)
        {
            SwitchToUDTMode();
        }
    }

    private void SwitchToCameraMode()
    {
        if (mTrackingMode == TrackingMode.DEVICE_ORIENTATION)
        {
            mTrackingMode = TrackingMode.CONSTRAINED_TO_CAMERA;

            DisplayModeLabel(msgModeUDT);

            // Show the quality indicator
            udtEventHandler.ShowQualityIndicator(true);

            // Hide the penguin
            ShowPenguin(false);
        }
    }

    private void SwitchToUDTMode()
    {
        if (mTrackingMode == TrackingMode.CONSTRAINED_TO_CAMERA)
        {
            // check if UDT frame quality is medium or high
            if (udtEventHandler.IsFrameQualityHigh() || udtEventHandler.IsFrameQualityMedium())
            {
                // Build a new UDT
                // Note that this may take more than one frame
                CreateUDT();

            }
            else
            {
                DisplayModeLabel(msgModeUDT);
            }
        }
    }

    private void CreateUDT()
    {
        float fovRad = cam.fieldOfView * Mathf.Deg2Rad;
        float halfSizeY = mInitialDistance * Mathf.Tan(0.5f * fovRad);
        float targetWidth = 2.0f * halfSizeY; // portrait
        if (Screen.width > Screen.height)
        { // landscape
            float screenAspect = Screen.width / (float)Screen.height;
            float halfSizeX = screenAspect * halfSizeY;
            targetWidth = 2.0f * halfSizeX;
        }

        mBuildingUDT = true;
        udtEventHandler.BuildNewTarget(targetWidth);
        startCheckingFrameQualityForNewUDT = false;
    }

    /*private void RotateToLookAtCamera()
    {
        Vector3 objPos = this.transform.position;
        Vector3 objGroundPos = new Vector3(objPos.x, 0, objPos.z); // y = 0
        Vector3 camGroundPos = new Vector3(cam.transform.position.x, 0, cam.transform.position.z);
        Vector3 objectToCam = camGroundPos - objGroundPos;
        objectToCam.Normalize();
        this.transform.rotation *= Quaternion.FromToRotation(this.transform.forward, objectToCam);
    }*/

    /*private void DisplayMessage(GameObject messageObj)
    {
        //msgGetCloser.SetActive((msgGetCloser == messageObj));
    }*/

    private void DisplayModeLabel(GameObject modeObj)
    {
        msgModeDT.SetActive((msgModeDT == modeObj));
        msgModeUDT.SetActive((msgModeUDT == modeObj));
    }

    private ImageTargetBehaviour GetActiveTarget()
    {
        StateManager stateManager = TrackerManager.Instance.GetStateManager();
        foreach (var tb in stateManager.GetActiveTrackableBehaviours())
        {
            if (tb is ImageTargetBehaviour)
            {
                // found target
                return (ImageTargetBehaviour)tb;
            }
        }
        return null;
    }

    private void ShowPenguin(bool isVisible)
    {
        if (penguinModel != null && penguinShadow != null)
        {
            penguinModel.GetComponent<Renderer>().enabled = isVisible;
            penguinShadow.GetComponent<Renderer>().enabled = isVisible;
        }
    }

    #endregion //PRIVATE_METHODS
}
