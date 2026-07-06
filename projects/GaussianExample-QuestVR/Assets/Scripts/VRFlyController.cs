using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

// VR fly navigation for Quest controllers — the stereo analog of FlyCamera.cs.
//
// Attach to the XR rig ROOT (the parent of the tracked camera), never to the
// camera itself: the HMD's tracked pose would fight any direct camera movement.
//
// Controls:
//   Left stick        fly forward/back + strafe, relative to gaze direction
//   Right stick X     smooth turn (or snap turn if snapTurnDegrees > 0)
//   Right stick Y     move up/down (world Y)
//   Left grip (hold)  sprint
//   Right A button    reset rig to start pose
public class VRFlyController : MonoBehaviour
{
    [Tooltip("The tracked HMD camera (child of this rig). Used as movement reference frame.")]
    public Transform head;

    [Tooltip("Base fly speed in meters per second")]
    public float moveSpeed = 1.5f;
    [Tooltip("Speed multiplier while holding the left grip")]
    public float sprintMultiplier = 3.0f;
    [Tooltip("Vertical speed (right stick Y) in meters per second")]
    public float verticalSpeed = 1.0f;
    [Tooltip("Smooth turn speed in degrees per second (right stick X)")]
    public float turnSpeed = 60.0f;
    [Tooltip("If > 0, use snap turning by this many degrees instead of smooth turning")]
    public float snapTurnDegrees = 0.0f;
    [Tooltip("Stick deadzone")]
    public float deadzone = 0.15f;

    InputDevice m_LeftHand;
    InputDevice m_RightHand;
    Vector3 m_ResetPosition;
    Quaternion m_ResetRotation;
    bool m_SnapTurnArmed = true;
    bool m_ResetButtonDown;

    void Start()
    {
        m_ResetPosition = transform.position;
        m_ResetRotation = transform.rotation;
        if (head == null && Camera.main != null)
            head = Camera.main.transform;
    }

    void Update()
    {
        if (!m_LeftHand.isValid)
            m_LeftHand = GetDevice(XRNode.LeftHand);
        if (!m_RightHand.isValid)
            m_RightHand = GetDevice(XRNode.RightHand);
        if (head == null)
            return;

        // reset pose on right controller A button
        if (m_RightHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool aPressed))
        {
            if (aPressed && !m_ResetButtonDown)
            {
                transform.SetPositionAndRotation(m_ResetPosition, m_ResetRotation);
                m_ResetButtonDown = true;
                return;
            }
            if (!aPressed)
                m_ResetButtonDown = false;
        }

        float dt = Time.deltaTime;

        // left stick: fly relative to gaze
        if (m_LeftHand.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 move) &&
            move.magnitude > deadzone)
        {
            float speed = moveSpeed;
            if (m_LeftHand.TryGetFeatureValue(CommonUsages.gripButton, out bool grip) && grip)
                speed *= sprintMultiplier;

            Vector3 delta = (head.forward * move.y + head.right * move.x) * (speed * dt);
            transform.position += delta;
        }

        if (m_RightHand.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 turnAxis))
        {
            // right stick Y: vertical movement
            if (Mathf.Abs(turnAxis.y) > deadzone)
                transform.position += Vector3.up * (turnAxis.y * verticalSpeed * dt);

            // right stick X: turn around the head position so the view pivots in place
            if (snapTurnDegrees > 0.0f)
            {
                if (Mathf.Abs(turnAxis.x) > 0.7f && m_SnapTurnArmed)
                {
                    transform.RotateAround(head.position, Vector3.up,
                        Mathf.Sign(turnAxis.x) * snapTurnDegrees);
                    m_SnapTurnArmed = false;
                }
                else if (Mathf.Abs(turnAxis.x) < 0.3f)
                {
                    m_SnapTurnArmed = true;
                }
            }
            else if (Mathf.Abs(turnAxis.x) > deadzone)
            {
                transform.RotateAround(head.position, Vector3.up, turnAxis.x * turnSpeed * dt);
            }
        }
    }

    static InputDevice GetDevice(XRNode node)
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(node, devices);
        return devices.Count > 0 ? devices[0] : default;
    }
}
