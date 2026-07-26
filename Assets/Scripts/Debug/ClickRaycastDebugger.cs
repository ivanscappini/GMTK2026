using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Utility to help debug what the player is clicking on.
/// On left mouse button down this performs:
/// - a Physics.RaycastAll from the main camera through the pointer
/// - an EventSystem UI raycast to list UI elements under the pointer
/// Logs detailed information to the console so you can see what is blocking clicks.
/// </summary>
public class ClickRaycastDebugger : MonoBehaviour
{
    [Tooltip("Maximum distance for physics raycasts")]
    public float MaxDistance = 100f;

    [Tooltip("Small radius for a sphere cast to catch thin colliders (0 = disabled)")]
    public float SphereCastRadius = 0.0f;

    void Update()
    {
        // Use the new Input System mouse. If it's not available, warn and exit.
        if (Mouse.current == null)
        {
            Debug.LogWarning("[ClickRaycastDebugger] No Mouse.current available (Input System not present). Clicks cannot be detected.");
            return;
        }

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Vector2 pointerPos = Mouse.current.position.ReadValue();

        Debug.Log("[ClickRaycastDebugger] Pointer clicked at screen " + pointerPos);

        // log active cameras to help diagnose which camera is used for ScreenPointToRay
        var cams = Camera.allCameras;
        Debug.Log($"[ClickRaycastDebugger] Cameras in scene: {cams.Length}");
        foreach (var cinfo in cams)
        {
            Debug.Log($"  Camera: {cinfo.name} (main={(cinfo==Camera.main)} enabled={cinfo.enabled} targetTexture={(cinfo.targetTexture!=null)}) pos={cinfo.transform.position} forward={cinfo.transform.forward}");
        }

        // Prefer the Cinemachine brain's output camera when present (virtual cameras drive that camera).
        Camera cam = null;
        try
        {
            // attempt to locate CinemachineBrain via reflection to avoid a hard dependency
            var brainType = System.Type.GetType("Cinemachine.CinemachineBrain, Cinemachine") ?? System.Type.GetType("Cinemachine.CinemachineBrain");
            if (brainType != null)
            {
                var brain = UnityEngine.Object.FindObjectOfType(brainType);
                if (brain != null)
                {
                    var outProp = brainType.GetProperty("OutputCamera");
                    if (outProp != null)
                    {
                        cam = outProp.GetValue(brain, null) as Camera;
                    }
                }
            }
        }
        catch
        {
            // ignore any reflection failures and fall back to Camera.main
        }

        if (cam == null)
            cam = Camera.main;

        // fallback to any enabled camera
        if (cam == null)
        {
            var all = Camera.allCameras;
            if (all != null && all.Length > 0)
                cam = all[0];
        }

        if (cam == null)
        {
            Debug.LogWarning("[ClickRaycastDebugger] No camera available for raycast.");
            return;
        }

        Debug.Log($"[ClickRaycastDebugger] Using camera chosen: {cam.name} (tag={cam.tag}, pos={cam.transform.position})");

        Ray ray = cam.ScreenPointToRay(pointerPos);

        // draw debug ray in scene view to visualize what's under the pointer
        Debug.DrawRay(ray.origin, ray.direction * MaxDistance, Color.red, 2f);

        // log ray / camera geometry for diagnostics
        Vector3 nearPoint = cam.ScreenToWorldPoint(new Vector3(pointerPos.x, pointerPos.y, cam.nearClipPlane));
        Vector3 farPoint = cam.ScreenToWorldPoint(new Vector3(pointerPos.x, pointerPos.y, cam.farClipPlane));
        Debug.Log($"[ClickRaycastDebugger] Using Camera.main: {cam.name} pos={cam.transform.position} near={cam.nearClipPlane} far={cam.farClipPlane}");
        Debug.Log($"[ClickRaycastDebugger] Ray origin={ray.origin} dir={ray.direction} worldNearPoint={nearPoint} worldFarPoint={farPoint}");

        // Physics hits
        if (SphereCastRadius > 0f)
        {
            RaycastHit[] hits = Physics.SphereCastAll(ray, SphereCastRadius, MaxDistance);
            if (hits.Length == 0)
            {
                Debug.Log("[ClickRaycastDebugger] Physics.SphereCastAll found no hits (radius=" + SphereCastRadius + ").");
            }
            else
            {
                Debug.Log($"[ClickRaycastDebugger] Physics.SphereCastAll hits ({hits.Length}):");
                foreach (var h in hits)
                {
                    Debug.Log($"  Hit: {h.collider.gameObject.name} (layer={LayerMask.LayerToName(h.collider.gameObject.layer)} tag={h.collider.gameObject.tag}) distance={h.distance:F2}");
                }
            }
        }
        else
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, MaxDistance);
            if (hits.Length == 0)
            {
                Debug.Log("[ClickRaycastDebugger] Physics.RaycastAll found no hits.");
            }
            else
            {
                Debug.Log($"[ClickRaycastDebugger] Physics.RaycastAll hits ({hits.Length}):");
                foreach (var h in hits)
                {
                    Debug.Log($"  Hit: {h.collider.gameObject.name} (layer={LayerMask.LayerToName(h.collider.gameObject.layer)} tag={h.collider.gameObject.tag}) distance={h.distance:F2}");
                }
            }
        }

        // UI hits via EventSystem
        if (EventSystem.current == null)
        {
            Debug.LogWarning("[ClickRaycastDebugger] EventSystem.current is null - UI raycasts cannot be tested.");
            return;
        }

        // log EventSystem modules and selected object for diagnostics
        var esGO = EventSystem.current.gameObject;
        var comps = esGO.GetComponents<Component>();
        Debug.Log($"[ClickRaycastDebugger] EventSystem on GameObject '{esGO.name}' has {comps.Length} components:");
        foreach (var comp in comps)
        {
            Debug.Log($"  Component: {comp.GetType().FullName} enabled={(comp is Behaviour b ? b.enabled.ToString() : "n/a")}");
        }

        Debug.Log($"[ClickRaycastDebugger] EventSystem.current.currentSelectedGameObject = {EventSystem.current.currentSelectedGameObject}");
        Debug.Log($"[ClickRaycastDebugger] Cursor: lockState={Cursor.lockState} visible={Cursor.visible}");

        PointerEventData ped = new PointerEventData(EventSystem.current)
        {
            position = pointerPos
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);

        if (results.Count == 0)
        {
            Debug.Log("[ClickRaycastDebugger] EventSystem.RaycastAll found no UI results.");
        }
        else
        {
            Debug.Log($"[ClickRaycastDebugger] EventSystem.RaycastAll results ({results.Count}):");
            foreach (var r in results)
            {
                Debug.Log($"  UI Hit: {r.gameObject.name} (module={r.module}, distance={r.distance:F2}, index={r.index})");
            }
        }

        // Additionally run GraphicRaycaster.Raycast per Canvas to capture which Canvas and GraphicRaycaster
        var canvases = GameObject.FindObjectsOfType<Canvas>();
        foreach (var c in canvases)
        {
            var gr = c.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (gr == null) continue;

            PointerEventData localPed = new PointerEventData(EventSystem.current) { position = pointerPos };
            List<RaycastResult> canvasResults = new List<RaycastResult>();
            gr.Raycast(localPed, canvasResults);

            if (canvasResults.Count > 0)
            {
                Debug.Log($"[ClickRaycastDebugger] GraphicRaycaster on Canvas '{c.name}' found {canvasResults.Count} hits:");
                foreach (var cr in canvasResults)
                {
                    var go = cr.gameObject;
                    var cg = go.GetComponent<UnityEngine.CanvasGroup>();
                    bool blocks = cg == null ? true : cg.blocksRaycasts;
                    Debug.Log($"  CanvasHit: {go.name} (active={go.activeInHierarchy}, interactable={(go.GetComponent<UnityEngine.UI.Selectable>() != null ? go.GetComponent<UnityEngine.UI.Selectable>().interactable.ToString() : "n/a")}, canvasGroup.blocksRaycasts={blocks})");
                }
            }
            else
            {
                Debug.Log($"[ClickRaycastDebugger] GraphicRaycaster on Canvas '{c.name}' found no hits.");
            }
        }
    }
}
