using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlightHUD : MonoBehaviour
{
    // Start is called before the first frame update
    [Header("Components")]
    [SerializeField] private ManualFlightInput manualFlightInput = null;

    [Header("HUD Elements")]
    [SerializeField] private RectTransform boresight = null;
    [SerializeField] private RectTransform mousePos = null;

    private Camera playerCam = null;

    private void Awake()
    {
        if (manualFlightInput == null)
            Debug.LogError(name + ": Hud - Mouse Flight Controller not assigned!");

        playerCam = manualFlightInput.GetComponentInChildren<Camera>();

        if (playerCam == null)
            Debug.LogError(name + ": Hud - No camera found on assigned Mouse Flight Controller!");
    }

    private void FixedUpdate()
    {
        if (manualFlightInput == null || playerCam == null)
            return;

        UpdateGraphics(manualFlightInput);
    }

    private void UpdateGraphics(ManualFlightInput controller)
    {
        if (boresight != null)
        {
            boresight.position = playerCam.WorldToScreenPoint(controller.BoresightPos);
            boresight.gameObject.SetActive(boresight.position.z > 1f);
        }

        if (mousePos != null)
        {
            mousePos.position = playerCam.WorldToScreenPoint(controller.MouseAimPos);
            mousePos.gameObject.SetActive(mousePos.position.z > 1f);
        }
    }

    public void SetReferenceMouseFlight(ManualFlightInput controller)
    {
        manualFlightInput = controller;
    }
}

