using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class CastleController : MonoBehaviour {

    [SerializeField]
    private UserDefinedTargetHandler udtEventHandler;
    
    [SerializeField]
    private GameObject msgFindThePenguin;
    [SerializeField]
    private GameObject msgTapThePenguin;
    [SerializeField]
    private GameObject msgTapTheCircle;
    
    [SerializeField]
    private GameObject msgModeDT;
    [SerializeField]
    private GameObject msgModeUDT;

    private Camera cam;

    private enum TrackingMode
    {
        DEVICE_ORIENTATION,
        CONSTRAINED_TO_CAMERA,
        UDT_BASED,
    }

    // initial mode
    private TrackingMode mTrackingMode = TrackingMode.DEVICE_ORIENTATION;
    private GameObject castleObject;
    private float mInitialDistance = 2.5f;

    // Use this for initialization
    void Start () {
        mTrackingMode = TrackingMode.DEVICE_ORIENTATION;

        VuforiaARController.Instance.RegisterVuforiaStartedCallback(OnVuforiaStarted);
    }
	
	// Update is called once per frame
	void Update () {
		
	}


    // Callback called when Vuforia has started
    private void OnVuforiaStarted()
    {
        cam = Vuforia.DigitalEyewearARController.Instance.PrimaryCamera ?? Camera.main;
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

           

            // Show the quality indicator
            udtEventHandler.ShowQualityIndicator(true);

            // Hide the penguin
            ShowCastle(false);
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
                //DisplayMessage(msgTryAgain);
                //DisplayModeLabel(msgModeUDT);
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

        //mBuildingUDT = true;
        udtEventHandler.BuildNewTarget(targetWidth);
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

    private void ShowCastle(bool isVisible)
    {
        if (castleObject != null )
        {
            castleObject.GetComponent<Renderer>().enabled = isVisible;
        }
    }
}
