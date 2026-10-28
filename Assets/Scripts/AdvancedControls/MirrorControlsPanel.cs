// Copyright 2026 The Open Brush Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections;
using UnityEngine;

namespace TiltBrush
{
    /// Mirror orientation, axis lock, pose save/recall/undo.
    /// Prefab registration (PanelType, panel map, launch button) is Unity-side work.
    public class MirrorControlsPanel : BasePanel
    {


        [Header("Mirror panel tints")]
        [Tooltip("Occupied slot that is not current, not dest/To, and axis-aligned.")]
        [SerializeField] Color m_ActiveTint = new Color(0f, 102f / 255f, 0f, 1f);
        [Header("Ring lock tints")]
        [Tooltip("On-color for the Ring orientation buttons.")]
        [SerializeField] Color m_RingOrientationTint = new Color(0.1f, 0.85f, 0.2f, 1f);
        [Tooltip("Panel color when the live slot is also a multi-select target.")]
        [SerializeField] Color m_LiveSelectedTint = new Color(0.53f, 0.81f, 0.92f, 1f);
        [Tooltip("Was live, now a marker. Orange is the source. After the draw, it returns to the standard slot color.")]
        [SerializeField] Color m_WasLiveTint = new Color(0.6f, 0f, 0.4f, 1f);
        [Tooltip("Shared on-color for RingTangentLock and RingPlaneLock.")]
        [SerializeField] Color m_RingLockTint = new Color(0f, 102f / 255f, 0f, 1f);
        [Tooltip("Empty slot, or a mode button that is off.")]
        [SerializeField] Color m_InactiveTint = new Color(115f / 255f, 115f / 255f, 115f / 255f, 1f);
        [Tooltip("Occupied slot whose saved rotation is not one of the four home orientations.")]
        [SerializeField] Color m_AngledSlotTint = new Color(247f / 255f, 2f / 255f, 174f / 255f, 1f);
        [Tooltip("Slot whose saved pose matches the live glass. World slot-guide is hidden while the glass is on.")]
        [SerializeField] Color m_CurrentSlotTint = new Color(5f / 255f, 144f / 255f, 250f / 255f, 1f);
        [Tooltip("Hyperspace To while Hyperspace is on. Jump dest while Hyperspace is off and dest is not the live pose.")]
        [SerializeField] Color m_MoveToMirrorToTint = new Color(1f, 200f / 255f, 0f, 1f);
        [Tooltip("ArchitectLineMode button. Separate from the target tint.")]
        [SerializeField] Color m_ArchitectLineModeTint = new Color(1f, 0f, 197f / 255f, 1f);
        [Tooltip("Architectural target button. Guide uses the widget color.")]
        [SerializeField] Color m_ArchitecturalTargetTint = new Color(1f, 0f, 197f / 255f, 1f);
        [Tooltip("ArchitecturalMultiTarget button and selected slots. Not the active glass.")]
        [SerializeField] Color m_ArchitecturalMultiTargetTint = new Color(1f, 0f, 197f / 255f, 1f);
        [Tooltip("ArchitecturalConnectAll button. One shot. Not the active glass.")]
        [SerializeField] Color m_ArchitecturalConnectAllTint = new Color(1f, 0f, 197f / 255f, 1f);
        [Tooltip("Reassign Slot mode button and the marked live slot. Lemon yellow.")]
        [SerializeField] Color m_ReassignSlotTint = new Color(1f, 230f / 255f, 0f, 1f);
        [Tooltip("Slot just drawn to. Stays after Architect Line Mode turns off. Turns blue when that slot becomes live.")]
        [SerializeField] Color m_ArchitecturalLastDrawnTint = new Color(255f / 255f, 139f / 255f, 56f / 255f, 1f);
        [Tooltip("Slot the last single draw came from. Click it to target it.")]
        [SerializeField] Color m_ArchitecturalPreviousSourceTint = new Color(0.0f, 0.8f, 0.8f, 1f);
        [Tooltip("Panel slot tint when the live glass matches the jump dest.")]
        [SerializeField] Color m_LiveDestTint = new Color(1f, 1f, 0f, 1f);
        [Tooltip("Ring color when the live glass matches the jump dest.")]
        [SerializeField] Color m_LiveTargetRingTint = new Color(1f, 1f, 0f, 1f);
        [Header("Live target ring")]
        [Tooltip("Ring shown only while the live glass matches the jump target.")]
        GameObject m_LiveTargetRing;
        LineRenderer m_LiveTargetRingLine;
        float m_AxisFlashUntil;
        string m_AxisFlashName;

        Color MirrorControlsMoveToMirrorToTint
        {
            get
            {
                return m_MoveToMirrorToTint;
            }
        }

        Color MirrorControlsCurrentSlotTint
        {
            get
            {
                return m_CurrentSlotTint;
            }
        }

        Color MirrorControlsAngledSlotTint
        {
            get
            {
                return m_AngledSlotTint;
            }
        }

        Color MirrorControlsActiveTint
        {
            get
            {
                return m_ActiveTint;
            }
        }

        Color MirrorControlsInactiveTint
        {
            get
            {
                return m_InactiveTint;
            }
        }

        Color MirrorControlsLastDrawnTint
        {
            get
            {
                return m_ArchitecturalLastDrawnTint;
            }
        }


        // DECLARATIONS
        bool m_MirrorSaveSlotClearMode;
        string m_LastLoggedAxisDescX;
        string m_LastLoggedAxisDescZ;
        Quaternion m_LastAxisLabelSceneRot;
        bool m_AxisLabelSceneRotValid;
        const float kAxisLabelSceneRotDegrees = 2.0f;


        override protected void OnEnablePanel()
        {
            base.OnEnablePanel();
            m_AxisLabelSceneRotValid = false;
            RefreshMirrorControlsPanelTints();
            RefreshAxisLockButtonDescriptions();
            RefreshOrientationButtonDescriptions();
            RefreshAllMirrorControlsTints();
        }

        override public void OnUpdatePanel(Vector3 vToPanel, Vector3 vHitPoint)
        {
            base.OnUpdatePanel(vToPanel, vHitPoint);
            RefreshMirrorControlsPanelTints();
            ShowSlot1IfMirrorModeOff();
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget != null)
            {
                widget.TickLocomotionDeselect();
                widget.TickMoveToMirrorValidity();
                widget.TickSlotGuideSceneScale();
            }
            RefreshAxisLockButtonDescriptions();
            RefreshOrientationButtonDescriptions();
            RefreshAllMirrorControlsTints();
        }

        static bool s_ControlsGlassShown;
        static bool s_PanelIsOpen;

        public static bool PanelIsOpen
        {
            get
            {
                return s_PanelIsOpen;
            }
        }

        static bool s_LoggedShowSlotNoManager;
        static bool s_LoggedShowSlotNoWidget;

        void ShowSlot1IfMirrorModeOff()
        {
            if (s_ControlsGlassShown)
            {
                return;
            }
            if (PointerManager.m_Instance == null)
            {
                if (!s_LoggedShowSlotNoManager)
                {
                    s_LoggedShowSlotNoManager = true;
                    Debug.LogWarning("[MirrorControlsPanel.ShowSlot1IfMirrorModeOff] skip no PointerManager");
                }
                return;
            }
            // Mode on is the normal case while the panel is open. Do not log it.
            if (PointerManager.m_Instance.CurrentSymmetryMode != PointerManager.SymmetryMode.None)
            {
                return;
            }
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null)
            {
                if (!s_LoggedShowSlotNoWidget)
                {
                    s_LoggedShowSlotNoWidget = true;
                    Debug.LogWarning("[MirrorControlsPanel.ShowSlot1IfMirrorModeOff] skip no widget");
                }
                return;
            }
            s_PanelIsOpen = true;
            s_ControlsGlassShown = true;
            Debug.Log("[MirrorControlsPanel.ShowSlot1IfMirrorModeOff] show slot 1 mirror mode off");
            widget.ShowGlassForControlsPanel();
        }


        void LogButtonPress(string button, string method)
        {
            Debug.LogError("[MirrorControlsPanel." + method + "] button=" + button);
        }

        public void NotifyDismissedByUser()
        {
            SymmetryWidget mirror = FindSymmetryWidget();
            if (mirror != null && mirror.MoveToMirrorModeActive)
            {
                Debug.LogError(
                    "[MirrorControlsPanel.NotifyDismissedByUser] close or toss -> exit Hyperspace");
                mirror.ExitMoveToMirrorMode();
            }
            s_PanelIsOpen = false;
            s_ControlsGlassShown = false;
            if (mirror != null)
            {
                mirror.HideGlassForControlsPanel();
                mirror.HideArchitecturalMarkersPublic();
                if (m_LiveTargetRing != null)
                {
                    m_LiveTargetRing.SetActive(false);
                }
            }
        }

        public static void CloseAllOpenInstances()
        {
            MirrorControlsPanel[] panels = FindObjectsByType<MirrorControlsPanel>();
            for (int i = 0; i < panels.Length; ++i)
            {
                if (panels[i] != null)
                {
                    panels[i].ClosePanel();
                }
            }

        }

        public void ClosePanel()
        {
            m_Fixed = false;
            NotifyDismissedByUser();

            PanelWidget widget = GetComponent<PanelWidget>();
            if (widget != null)
            {
                widget.Show(false);
                return;
            }

            gameObject.SetActive(false);
        }

        static SymmetryWidget FindSymmetryWidget()
        {
            if (PointerManager.m_Instance == null)
            {
                return null;
            }
            return PointerManager.m_Instance.SymmetryWidget;
        }

        static readonly Color kMirrorControlsPanelActiveTint =
            new Color(0x00 / 255f, 0x66 / 255f, 0x00 / 255f, 1f);
        static readonly Color kMirrorControlsPanelInactiveTint =
            new Color(0.45f, 0.45f, 0.45f, 1f);

        static void ForceMirrorControlsPanelButtonTint(BaseButton button, Color tint)
        {
            if (button == null)
            {
                return;
            }
            Renderer renderer = button.GetComponent<Renderer>();
            if (renderer == null || renderer.material == null)
            {
                return;
            }
            renderer.material.SetColor("_Color", tint);
            if (renderer.material.HasProperty("_SecondaryColor"))
            {
                renderer.material.SetColor("_SecondaryColor", tint);
            }
        }



        public static void RefreshOpenOrientationIcons()
        {
            MirrorControlsPanel[] panels = FindObjectsByType<MirrorControlsPanel>();
            for (int i = 0; i < panels.Length; ++i)
            {
                if (panels[i] != null)
                {
                    panels[i].RefreshMirrorControlsPanelTints();
                }
            }
        }

        void RefreshMirrorControlsPanelTints()
        {
            SymmetryWidget widget = FindSymmetryWidget();
            SymmetryWidget.PreferredOrientation orient =
                widget != null
                    ? widget.CurrentPreferredOrientation
                    : SymmetryWidget.PreferredOrientation.HorizontalForward;
            bool orientSaved = widget != null && widget.LiveOrientationMatchesSavedSlot();
            SymmetryWidget.MirrorSlideDefault axis =
                widget != null ? widget.CurrentMirrorSlideDefault : SymmetryWidget.MirrorSlideDefault.None;

            BaseButton[] toggles = GetComponentsInChildren<BaseButton>(true);
            string activeRing = "Ring" + RingOrientation().ToString();
            for (int i = 0; i < toggles.Length; ++i)
            {
                if (toggles[i] == null)
                {
                    continue;
                }

                string name = toggles[i].gameObject.name;
                bool active = false;
                bool isOrientation = false;
                bool isRingOrientation = false;
                bool isMirrorModeControl = true;

                switch (name)
                {
                    case "SetMirrorHorizontalSideways":
                        active = orient == SymmetryWidget.PreferredOrientation.HorizontalSideways;
                        isOrientation = true;
                        break;
                    case "SetMirrorHorizontalForward":
                        active = orient == SymmetryWidget.PreferredOrientation.HorizontalForward;
                        isOrientation = true;
                        break;
                    case "SetMirrorVerticalForward":
                        active = orient == SymmetryWidget.PreferredOrientation.VerticalForward;
                        isOrientation = true;
                        break;
                    case "SetMirrorVerticalSideways":
                        active = orient == SymmetryWidget.PreferredOrientation.VerticalSideways;
                        isOrientation = true;
                        break;
                    case "Left-Right-X":
                        active = axis == SymmetryWidget.MirrorSlideDefault.X;
                        break;
                    case "Up-Down-Y":
                        active = axis == SymmetryWidget.MirrorSlideDefault.Y;
                        break;
                    case "Forward-Back-Z":
                        active = axis == SymmetryWidget.MirrorSlideDefault.Z;
                        break;
                    case "LockMirrorMovement":
                        active = axis == SymmetryWidget.MirrorSlideDefault.All;
                        break;
                    case "MirrorSnapMode":
                        active = widget != null && widget.MirrorSnapModeActive;
                        break;
                    case "FreeformSnap":
                        active = widget != null && widget.FreeformSnapArmed;
                        break;
                    case "RingTangentLock":
                        active = PointerManager.m_Instance != null
                            && PointerManager.m_Instance.m_RingTangentLock;
                        break;
                    case "RingPlaneLock":
                        active = PointerManager.m_Instance != null
                            && PointerManager.m_Instance.m_RingPlaneLock;
                        break;
                    case "RingDefault":
                    case "RingHorizontalForward":
                    case "RingVerticalForward":
                    case "RingHorizontalSideways":
                    case "RingVerticalSideways":
                        active = name.StartsWith(activeRing);
                        isRingOrientation = true;
                        break;
                    default:
                        isMirrorModeControl = false;
                        break;
                }

                if (!isMirrorModeControl)
                {
                    continue;
                }

                Color tint = kMirrorControlsPanelInactiveTint;
                if (isRingOrientation)
                {
                    tint = active ? m_RingOrientationTint : kMirrorControlsPanelInactiveTint;
                }
                else if (active && (name == "RingTangentLock" || name == "RingPlaneLock"))
                {
                    tint = m_RingLockTint;
                }
                else if (active && isOrientation && !orientSaved)
                {
                    tint = Color.red;
                }
                else if (active)
                {
                    tint = kMirrorControlsPanelActiveTint;
                }
                ForceMirrorControlsPanelButtonTint(toggles[i], tint);
            }
        }


        public void SaveMirrorPose()
        {
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null)
            {
                Debug.LogError("MIRROR_POSE: Save — SymmetryWidget is null");
                return;
            }
            widget.SaveMirrorPose();
            Debug.LogError("MIRROR_POSE: Save — OK");
        }

        public void RecallMirrorPose()
        {
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null)
            {
                Debug.LogError("MIRROR_POSE: Recall — SymmetryWidget is null");
                return;
            }
            if (!widget.HasSavedMirrorPose)
            {
                Debug.LogError("MIRROR_POSE: Recall — nothing saved");
                return;
            }
            widget.RecallMirrorPose();
            Debug.LogError("MIRROR_POSE: Recall — OK");
        }


        public void UndoMirrorMove()
        {
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null)
            {
                Debug.LogError("MIRROR_UNDO: SymmetryWidget null");
                return;
            }
            if (widget.HasReassignUndo)
            {
                widget.UndoReassignSlot();
                RefreshAllMirrorControlsTints();
                Debug.LogError("MIRROR_UNDO: reassign restored");
                return;
            }
            if (!widget.HasMirrorMoveUndo)
            {
                Debug.LogError("MIRROR_UNDO: nothing to undo");
                return;
            }
            widget.UndoLastMirrorMove();
            Debug.LogError("MIRROR_UNDO: applied");
        }

        public void SetMirrorHorizontalSideways()
        {
            LogButtonPress("SetMirrorHorizontalSideways", "SetMirrorHorizontalSideways");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null)
            {
                Debug.LogError("MIRROR_ORIENT: HorizontalSideways — SymmetryWidget null");
                return;
            }
            widget.SetOrientationHorizontalSideways();
            RefreshMirrorControlsPanelTints();
        }

        public void SetMirrorHorizontalForward()
        {
            LogButtonPress("SetMirrorHorizontalForward", "SetMirrorHorizontalForward");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null)
            {
                Debug.LogError("MIRROR_ORIENT: HorizontalForward — SymmetryWidget null");
                return;
            }
            widget.SetOrientationHorizontalForward();
            RefreshMirrorControlsPanelTints();
        }

        public void SetMirrorVerticalForward()
        {
            LogButtonPress("SetMirrorVerticalForward", "SetMirrorVerticalForward");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null)
            {
                Debug.LogError("MIRROR_ORIENT: VerticalForward — SymmetryWidget null");
                return;
            }
            widget.SetOrientationVerticalForward();
            RefreshMirrorControlsPanelTints();
        }

        public void SetMirrorVerticalSideways()
        {
            LogButtonPress("SetMirrorVerticalSideways", "SetMirrorVerticalSideways");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null)
            {
                Debug.LogError("MIRROR_ORIENT: VerticalSideways — SymmetryWidget null");
                return;
            }
            widget.SetOrientationVerticalSideways();
            RefreshMirrorControlsPanelTints();
        }

        public void ToggleMirrorAxisLockX()
        {
            LogButtonPress("Left-Right-X", "ToggleMirrorAxisLockX");
            Debug.LogError("[MirrorControlsPanel.ToggleMirrorAxisLockX] button=Left-Right-X");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null)
            {
                Debug.LogError("MIRROR_AXIS: panel ToggleX — SymmetryWidget null");
                return;
            }
            widget.ToggleMirrorSlideDefaultX();
            Debug.LogError("MIRROR_AXIS: panel ToggleX done, widget=" + widget.CurrentMirrorSlideDefault);
            RefreshMirrorControlsPanelTints();
        }

        public void ToggleMirrorAxisLockY()
        {
            Debug.LogError("[MirrorControlsPanel.ToggleMirrorAxisLockY] button=Y-axis-object-not-in-lookup");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null)
            {
                Debug.LogError("MIRROR_AXIS: panel ToggleY — SymmetryWidget null");
                return;
            }
            widget.ToggleMirrorSlideDefaultY();
            Debug.LogError("MIRROR_AXIS: panel ToggleY done, widget=" + widget.CurrentMirrorSlideDefault);
            RefreshMirrorControlsPanelTints();
        }

        public void ToggleMirrorAxisLockZ()
        {
            LogButtonPress("Forward-Back-Z", "ToggleMirrorAxisLockZ");
            Debug.LogError("[MirrorControlsPanel.ToggleMirrorAxisLockZ] button=Forward-Back-Z");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null)
            {
                Debug.LogError("MIRROR_AXIS: panel ToggleZ — SymmetryWidget null");
                return;
            }
            widget.ToggleMirrorSlideDefaultZ();
            Debug.LogError("MIRROR_AXIS: panel ToggleZ done, widget=" + widget.CurrentMirrorSlideDefault);
            RefreshMirrorControlsPanelTints();
        }

        public void ToggleMirrorAxisLockAll()
        {
            Debug.LogError("[MirrorControlsPanel.ToggleMirrorAxisLockAll] button=all-axis-object-not-in-lookup");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null)
            {
                Debug.LogError("MIRROR_AXIS: panel ToggleAll — SymmetryWidget null");
                return;
            }
            widget.ToggleMirrorSlideDefaultAll();
            Debug.LogError("MIRROR_AXIS: panel ToggleAll done, widget=" + widget.CurrentMirrorSlideDefault);
            RefreshMirrorControlsPanelTints();
        }

        public void Toggle_FreeformSnap()
        {
            LogButtonPress("FreeformSnap", "Toggle_FreeformSnap");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget != null)
            {
                widget.Toggle_FreeformSnap();
            }
        }

        public void ToggleMirrorSnapMode()
        {
            LogButtonPress("MirrorSnapMode", "ToggleMirrorSnapMode");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget != null)
            {
                widget.ToggleMirrorSnapMode();
            }
            RefreshMirrorControlsPanelTints();
        }

        /// Hover text for scene X/Z only: Left-Right vs Forward-Back from a
        /// snapshot of the user vs scene. Do not retitle on headset look.
        /// Retitle when scene rotation changes (world turn). Buttons still
        /// lock scene X and Z; Y stays Up-Down.
        void RefreshAxisLockButtonDescriptions()
        {
            if (!ShouldRefreshAxisLabelsForSceneRotation())
            {
                return;
            }

            bool xIsSideways = GetSceneXIsSidewaysForUser();
            string descX = xIsSideways ? "Lateral" : "Forward-Back";
            string descZ = xIsSideways ? "Forward-Back" : "Lateral";

            ActionToggleButton[] toggles = GetComponentsInChildren<ActionToggleButton>(true);
            for (int i = 0; i < toggles.Length; ++i)
            {
                if (toggles[i] == null)
                {
                    continue;
                }
                string name = toggles[i].gameObject.name;
                if (name == "Left-Right-X")
                {
                    toggles[i].SetDescriptionText(descX);
                }
                else if (name == "Forward-Back-Z")
                {
                    toggles[i].SetDescriptionText(descZ);
                }
            }

            if (descX != m_LastLoggedAxisDescX || descZ != m_LastLoggedAxisDescZ)
            {
                m_LastLoggedAxisDescX = descX;
                m_LastLoggedAxisDescZ = descZ;
                SymmetryWidget widget = FindSymmetryWidget();
                string lockName = widget != null ? widget.CurrentMirrorSlideDefault.ToString() : "none";
                Debug.LogError(
                    "[MirrorControlsPanel] AXIS_LABEL xIsSideways=" + xIsSideways +
                    " X=" + descX + " Z=" + descZ + " lock=" + lockName);
            }
        }




        bool ShouldRefreshAxisLabelsForSceneRotation()
        {
            if (App.Scene == null)
            {
                return !m_AxisLabelSceneRotValid;
            }
            Quaternion sceneRot = App.Scene.Pose.rotation;
            if (!m_AxisLabelSceneRotValid)
            {
                m_LastAxisLabelSceneRot = sceneRot;
                m_AxisLabelSceneRotValid = true;
                return true;
            }
            if (Quaternion.Angle(sceneRot, m_LastAxisLabelSceneRot) < kAxisLabelSceneRotDegrees)
            {
                return false;
            }
            m_LastAxisLabelSceneRot = sceneRot;
            return true;
        }

        /// True when scene X is the user's lateral axis (scene Z more aligned with head forward).
        /// False when scene X is more forward/back for the user (labels should swap).
        static bool GetSceneXIsSidewaysForUser()
        {
            if (ViewpointScript.Head == null || App.Scene == null)
            {
                return true;
            }

            Vector3 headFlat = ViewpointScript.Head.forward;
            headFlat.y = 0.0f;
            if (headFlat.sqrMagnitude < 1e-6f)
            {
                return true;
            }
            headFlat.Normalize();

            // Scene axes expressed in global space, then flattened.
            Quaternion sceneToGlobal = App.Scene.Pose.rotation;
            Vector3 sceneX_GS = sceneToGlobal * Vector3.right;
            Vector3 sceneZ_GS = sceneToGlobal * Vector3.forward;
            sceneX_GS.y = 0.0f;
            sceneZ_GS.y = 0.0f;

            if (sceneX_GS.sqrMagnitude < 1e-6f || sceneZ_GS.sqrMagnitude < 1e-6f)
            {
                return true;
            }
            sceneX_GS.Normalize();
            sceneZ_GS.Normalize();

            float alignX = Mathf.Abs(Vector3.Dot(headFlat, sceneX_GS));
            float alignZ = Mathf.Abs(Vector3.Dot(headFlat, sceneZ_GS));
            return alignZ >= alignX;
        }



        void RefreshOrientationButtonDescriptions()
        {
            float vf = SymmetryWidget.GetOrientationForwardScore(
                SymmetryWidget.PreferredOrientation.VerticalForward);
            float vs = SymmetryWidget.GetOrientationForwardScore(
                SymmetryWidget.PreferredOrientation.VerticalSideways);
            float hf = SymmetryWidget.GetOrientationForwardScore(
                SymmetryWidget.PreferredOrientation.HorizontalForward);
            float hs = SymmetryWidget.GetOrientationForwardScore(
                SymmetryWidget.PreferredOrientation.HorizontalSideways);

            // Pairwise: higher score = Forward label on that button; the other gets Sideways.
            bool vfIsForward = vf >= vs;
            bool hfIsForward = hf >= hs;

            ActionToggleButton[] toggles = GetComponentsInChildren<ActionToggleButton>(true);
            for (int i = 0; i < toggles.Length; ++i)
            {
                if (toggles[i] == null)
                {
                    continue;
                }
                string name = toggles[i].gameObject.name;
                if (name == "SetMirrorVerticalForward")
                {
                    toggles[i].SetDescriptionText(
                        vfIsForward ? "Mirror Vertical Forward" : "Mirror Vertical Sideways");
                }
                else if (name == "SetMirrorVerticalSideways")
                {
                    toggles[i].SetDescriptionText(
                        vfIsForward ? "Mirror Vertical Sideways" : "Mirror Vertical Forward");
                }
                else if (name == "SetMirrorHorizontalForward")
                {
                    toggles[i].SetDescriptionText(
                        hfIsForward ? "Mirror Horizontal Forward" : "Mirror Horizontal Sideways");
                }
                else if (name == "SetMirrorHorizontalSideways")
                {
                    toggles[i].SetDescriptionText(
                        hfIsForward ? "Mirror Horizontal Sideways" : "Mirror Horizontal Forward");
                }
            }
        }


        // Constrain Spin and enforce 90 degree snap
        public void ToggleConstrainOrientation()
        {
            LogButtonPress("ConstrainOrientation", "ToggleConstrainOrientation");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null)
            {
                Debug.LogError(
                    "[MirrorControlsPanel.ToggleConstrainOrientation] button=ConstrainOrientation widget null");
                return;
            }
            widget.ToggleLockOrientation();
            RefreshMirrorControlsPanelTints();
            RefreshAllMirrorControlsTints();
        }

        public void ToggleLockSpin()
        {
            LogButtonPress("LockSpin", "ToggleLockSpin");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null)
            {
                Debug.LogError(
                    "[MirrorControlsPanel.ToggleLockSpin] button=LockSpin widget null");
                return;
            }
            widget.ToggleLockSpin();
            RefreshMirrorControlsPanelTints();
            RefreshAllMirrorControlsTints();
        }


        // ------- MIRROR SAVE SLOT SECTION BEGINS ----------
        public void MirrorSaveSlot01()
        {
            LogButtonPress("MirrorSaveSlot01", "MirrorSaveSlot01");
            HandleMirrorSaveSlot(2);
        }
        public void MirrorSaveSlot02()
        {
            LogButtonPress("MirrorSaveSlot02", "MirrorSaveSlot02");
            HandleMirrorSaveSlot(3);
        }
        public void MirrorSaveSlot03()
        {
            LogButtonPress("MirrorSaveSlot03", "MirrorSaveSlot03");
            HandleMirrorSaveSlot(4);
        }
        public void MirrorSaveSlot04()
        {
            LogButtonPress("MirrorSaveSlot04", "MirrorSaveSlot04");
            HandleMirrorSaveSlot(5);
        }
        public void MirrorSaveSlot05()
        {
            LogButtonPress("MirrorSaveSlot05", "MirrorSaveSlot05");
            HandleMirrorSaveSlot(6);
        }
        public void MirrorSaveSlot06()
        {
            LogButtonPress("MirrorSaveSlot06", "MirrorSaveSlot06");
            HandleMirrorSaveSlot(7);
        }
        public void MirrorSaveSlot07()
        {
            LogButtonPress("MirrorSaveSlot07", "MirrorSaveSlot07");
            HandleMirrorSaveSlot(8);
        }
        public void MirrorSaveSlot08()
        {
            LogButtonPress("MirrorSaveSlot08", "MirrorSaveSlot08");
            HandleMirrorSaveSlot(9);
        }
        public void MirrorSaveSlot09()
        {
            LogButtonPress("MirrorSaveSlot09", "MirrorSaveSlot09");
            HandleMirrorSaveSlot(10);
        }
        public void MirrorSaveSlot10()
        {
            LogButtonPress("MirrorSaveSlot10", "MirrorSaveSlot10");
            HandleMirrorSaveSlot(11);
        }
        public void MirrorSaveSlot11()
        {
            LogButtonPress("MirrorSaveSlot11", "MirrorSaveSlot11");
            HandleMirrorSaveSlot(12);
        }
        public void MirrorSaveSlot12()
        {
            LogButtonPress("MirrorSaveSlot12", "MirrorSaveSlot12");
            HandleMirrorSaveSlot(13);
        }
        public void MirrorSaveSlot13()
        {
            LogButtonPress("MirrorSaveSlot13", "MirrorSaveSlot13");
            HandleMirrorSaveSlot(14);
        }
        public void MirrorSaveSlot14()
        {
            LogButtonPress("MirrorSaveSlot14", "MirrorSaveSlot14");
            HandleMirrorSaveSlot(15);
        }
        public void MirrorSaveSlot15()
        {
            LogButtonPress("MirrorSaveSlot15", "MirrorSaveSlot15");
            HandleMirrorSaveSlot(16);
        }


        // SAVE SLOT RELATED FUNCTIONS

        public void ToggleMirrorSaveSlotClearMode()
        {
            LogButtonPress("ClearSavedMirror", "ToggleMirrorSaveSlotClearMode");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget != null && widget.MoveToMirrorModeActive)
            {
                return;
            }

            m_MirrorSaveSlotClearMode = !m_MirrorSaveSlotClearMode;
            Debug.LogError(
                "[MirrorControlsPanel.ToggleMirrorSaveSlotClearMode] button=ClearSavedMirror clear=" + m_MirrorSaveSlotClearMode);
            RefreshAllMirrorControlsTints();
        }

        void HandleMirrorSaveSlot(int slotNumber1Based)
        {
            int index = slotNumber1Based - 1;
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null)
            {
                Debug.LogError("MIRROR_SLOT: Handle — SymmetryWidget null");
                return;
            }
            if (index < 0 || index >= SymmetryWidget.kMirrorSaveSlotCount)
            {
                Debug.LogError("MIRROR_SLOT: Handle - bad index " + index);
                return;
            }

            if (widget.ReassignSlotModeActive)
            {
                if (widget.IsTrueCenterSlot(index))
                {
                    RefreshAllMirrorControlsTints();
                    return;
                }
                if (widget.IsMirrorSaveSlotMatchingCurrent(index))
                {
                    widget.ExitReassignSlotMode();
                    RefreshAllMirrorControlsTints();
                    return;
                }
                widget.ReassignLiveSlotTo(index);
                RefreshAllMirrorControlsTints();
                return;
            }

            if (widget.ArchitectLineModeActive && widget.ArchitecturalConnectArmed
                && widget.HasMirrorSaveSlot(index))
            {
                widget.ToggleArchitecturalConnectSlot(index);
                RefreshAllMirrorControlsTints();
                return;
            }

            if (widget.IsTrueCenterSlot(index) && !widget.MoveToMirrorModeActive
                && !widget.ArchitectLineModeActive)
            {
                if (m_MirrorSaveSlotClearMode)
                {
                    Debug.LogError("MIRROR_SLOT: Handle array=0 panel=1 TrueCenter - clear ignored");
                    RefreshAllMirrorControlsTints();
                    return;
                }
                widget.EnsureTrueCenterSlot();
                widget.SetTeleportDest(index);
                RefreshAllMirrorControlsTints();
                return;
            }

            if (widget.ArchitectLineModeActive && widget.ArchitecturalMultiTargetOn)
            {
                if (!widget.ArchitecturalSourceLocked && widget.HasMirrorSaveSlot(index))
                {
                    if (widget.IsArchitecturalLastDrawn(index))
                    {
                        widget.ClearArchitecturalPreviousSource();
                        widget.RecallMirrorSaveSlot(index);
                        widget.NoteArchitecturalPreviousSource(index);
                        widget.LockArchitecturalSource();
                        Debug.LogError("[MirrorControlsPanel.HandleMirrorSaveSlot] multi first orange source " + SymmetryWidget.SlotLogLabel(index));
                    }
                    else if (widget.IsMirrorSaveSlotMatchingCurrent(index))
                    {
                        widget.NoteArchitecturalPreviousSource(index);
                        widget.ClearArchitecturalLastDrawn();
                        widget.LockArchitecturalSource();
                        Debug.LogError("[MirrorControlsPanel.HandleMirrorSaveSlot] multi first blue source " + SymmetryWidget.SlotLogLabel(index));
                    }
                    else
                    {
                        int orange = -1;
                        for (int i = 0; i < SymmetryWidget.kMirrorSaveSlotCount; ++i)
                        {
                            if (widget.IsArchitecturalLastDrawn(i)) { orange = i; break; }
                        }
                        if (orange >= 0)
                        {
                            int wasLive = widget.TeleportDestIndex;
                            if (wasLive >= 0) widget.NoteWasLive(wasLive);
                            widget.ClearArchitecturalPreviousSource();
                            widget.RecallMirrorSaveSlot(orange);
                            widget.NoteArchitecturalPreviousSource(orange);
                            widget.LockArchitecturalSource();
                            Debug.LogError("[MirrorControlsPanel.HandleMirrorSaveSlot] multi first other, orange source " + SymmetryWidget.SlotLogLabel(orange));
                        }
                        else
                        {
                            widget.NoteArchitecturalPreviousSource(widget.TeleportDestIndex);
                            widget.LockArchitecturalSource();
                            Debug.LogError("[MirrorControlsPanel.HandleMirrorSaveSlot] multi first other, blue source " + SymmetryWidget.SlotLogLabel(widget.TeleportDestIndex));
                        }
                    }
                }
                if (widget.HasMirrorSaveSlot(index) && !widget.IsTrueCenterSlot(index))
                {
                    widget.ToggleArchitecturalMultiTargetSlot(index);
                }
                RefreshAllMirrorControlsTints();
                return;
            }

            if (widget.IsArchitecturalLastDrawn(index) && !widget.ArchitecturalSourceLocked)
            {
                widget.ClearArchitecturalPreviousSource();
                widget.RecallMirrorSaveSlot(index);
                widget.NoteArchitecturalPreviousSource(index);
                widget.LockArchitecturalSource();
                if (widget.ArchitectLineModeActive
                    && !widget.ArchitecturalMultiTargetOn
                    && !widget.ArchitecturalConnectArmed)
                {
                    widget.BeginArchitecturalTarget();
                }
                Debug.LogError("[MirrorControlsPanel.HandleMirrorSaveSlot] orange to live teal " + SymmetryWidget.SlotLogLabel(index));
                RefreshAllMirrorControlsTints();
                return;
            }
            if (widget.ArchitectLineModeActive && !widget.ArchitecturalSourceLocked
                && !widget.IsArchitecturalLastDrawn(index)
                && !widget.IsMirrorSaveSlotMatchingCurrent(index)
                && widget.HasMirrorSaveSlot(index))
            {
                int orange = -1;
                for (int i = 0; i < SymmetryWidget.kMirrorSaveSlotCount; ++i)
                {
                    if (widget.IsArchitecturalLastDrawn(i)) { orange = i; break; }
                }
                if (orange >= 0)
                {
                    int wasLive = widget.TeleportDestIndex;
                    if (wasLive >= 0)
                    {
                        widget.NoteWasLive(wasLive);
                    }
                    widget.ClearArchitecturalPreviousSource();
                    widget.RecallMirrorSaveSlot(orange);
                    widget.NoteArchitecturalPreviousSource(orange);
                    widget.LockArchitecturalSource();
                    Debug.LogError("[MirrorControlsPanel.HandleMirrorSaveSlot] regular click, orange to source " + SymmetryWidget.SlotLogLabel(orange));
                }
            }
            if (m_MirrorSaveSlotClearMode)
            {
                if (widget.HasMirrorSaveSlot(index) && !widget.IsTrueCenterSlot(index))
                {
                    widget.ClearMirrorSaveSlot(index);
                    m_MirrorSaveSlotClearMode = false;
                    Debug.LogError(
                        "[MirrorControlsPanel.HandleMirrorSaveSlot] clear " +
                        SymmetryWidget.SlotLogLabel(index) + " mode off");
                }
                else
                {
                    Debug.LogError(
                        "[MirrorControlsPanel.HandleMirrorSaveSlot] clear skip " +
                        SymmetryWidget.SlotLogLabel(index) +
                        " occupied=" + widget.HasMirrorSaveSlot(index));
                }
                RefreshAllMirrorControlsTints();
                return;
            }
            if (widget.IsMirrorSaveSlotMatchingCurrent(index))
            {
                if (widget.ArchitectLineModeActive)
                {
                    if (widget.IsArchitecturalPreviousSource(index) && widget.ArchitecturalSourceLocked)
                    {
                        widget.ClearArchitecturalPreviousSource();
                        Debug.LogError("[MirrorControlsPanel.HandleMirrorSaveSlot] live cleared source " + SymmetryWidget.SlotLogLabel(index));
                    }
                    else
                    {
                        widget.NoteArchitecturalPreviousSource(index);
                        widget.ClearArchitecturalLastDrawn();
                        widget.LockArchitecturalSource();
                        Debug.LogError("[MirrorControlsPanel.HandleMirrorSaveSlot] live confirmed source " + SymmetryWidget.SlotLogLabel(index));
                    }
                    if (widget.ArchitecturalMultiTargetOn)
                    {
                        widget.ToggleArchitecturalMultiTargetSlot(index);
                    }
                }
                else
                {
                    widget.ClearArchitecturalLastDrawn();
                }
                RefreshAllMirrorControlsTints();
                return;
            }

            if (widget.ArchitectLineModeActive)
            {
                if (widget.ArchitecturalMultiTargetOn)
                {
                    widget.ToggleArchitecturalMultiTargetSlot(index);
                }
                if (widget.ArchitecturalPickingTarget)
                {
                    widget.TryAssignArchitecturalTarget(index);
                }
                RefreshAllMirrorControlsTints();
                return;
            }

            // Hyperspace on: slots only assign To while picking; no save/recall/clear.
            if (widget.MoveToMirrorModeActive)
            {
                if (widget.MoveToMirrorPickingTo)
                {
                    widget.TryAssignMoveToMirrorTo(index);
                    RefreshAllMirrorControlsTints();
                }
                return;
            }

            if (widget.HasMirrorSaveSlot(index))
            {
                widget.SetTeleportDest(index);
            }
            else
            {
                widget.SaveMirrorSaveSlot(index);
            }
            RefreshAllMirrorControlsTints();
        }


        public void MirrorTrueCenter()
        {
            LogButtonPress("TrueCenter", "MirrorTrueCenter");
            HandleMirrorSaveSlot(1);
        }



        public void QuickRestore()
        {
            LogButtonPress("QuickRestore", "QuickRestore");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget != null && widget.MoveToMirrorModeActive)
            {
                return;
            }

            if (m_MirrorSaveSlotClearMode)
            {
                Debug.LogError("MIRROR_SLOT: QuickRestore — ignored (clear mode)");
                return;
            }
            if (widget == null)
            {
                Debug.LogError("MIRROR_SLOT: QuickRestore — SymmetryWidget null");
                return;
            }
            widget.ApplyMirrorQuickReturn();
            RefreshAllMirrorControlsTints();
        }


        public static void ResetModesForNewSketch()
        {
            MirrorControlsPanel[] panels = FindObjectsByType<MirrorControlsPanel>();
            for (int i = 0; i < panels.Length; ++i)
            {
                if (panels[i] == null) continue;
                panels[i].m_MirrorSaveSlotClearMode = false;
            }
            RefreshAllMirrorControlsTints();
        }

        public static void RefreshAllMirrorControlsTints()
        {
            MirrorControlsPanel[] panels = FindObjectsByType<MirrorControlsPanel>(FindObjectsInactive.Include);
            for (int i = 0; i < panels.Length; ++i)
            {
                if (panels[i] != null)
                {
                    panels[i].RefreshMirrorSaveSlotTints();
                }
            }
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget != null)
            {
                widget.RefreshVisibleSlotGuides();
                if (panels.Length > 0 && panels[0] != null)
                {
                    panels[0].UpdateLiveTargetRing(widget);
                }
            }
        }

        void UpdateLiveTargetRing(SymmetryWidget widget)
        {
            bool hasTarget = widget != null
                && (widget.ArchitectLineModeActive || widget.HasArchitecturalLastDrawn())
                && widget.PeekTeleportDestValid();
            bool matches = hasTarget && widget.IsMirrorSaveSlotMatchingCurrent(widget.TeleportDestIndex);
            if (!matches)
            {
                if (m_LiveTargetRing != null)
                {
                    m_LiveTargetRing.SetActive(false);
                }
                return;
            }
            if (m_LiveTargetRing == null)
            {
                m_LiveTargetRing = new GameObject("LiveTargetRing");
                m_LiveTargetRing.transform.SetParent(widget.transform, false);
                m_LiveTargetRingLine = m_LiveTargetRing.AddComponent<LineRenderer>();
                m_LiveTargetRingLine.useWorldSpace = false;
                m_LiveTargetRingLine.loop = true;
                m_LiveTargetRingLine.startWidth = 0.03f;
                m_LiveTargetRingLine.endWidth = 0.03f;
                m_LiveTargetRingLine.material = new Material(Shader.Find("Sprites/Default"));
                m_LiveTargetRingLine.startColor = m_LiveTargetRingTint;
                m_LiveTargetRingLine.endColor = m_LiveTargetRingTint;
                const int segments = 48;
                m_LiveTargetRingLine.positionCount = segments;
                float radius = 0.7f;
                for (int i = 0; i < segments; i++)
                {
                    float angle = i * Mathf.PI * 2f / segments;
                    m_LiveTargetRingLine.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0.01f));
                }
                Debug.LogError("[MirrorControlsPanel.UpdateLiveTargetRing] created ring");
            }
            if (m_LiveTargetRing != null)
            {
                m_LiveTargetRing.SetActive(true);
                Debug.LogError("[MirrorControlsPanel.UpdateLiveTargetRing] show ring dest=" +
                    SymmetryWidget.SlotLogLabel(widget.TeleportDestIndex));
            }
        }

        void RefreshMirrorSaveSlotTints()
        {
            SymmetryWidget widget = FindSymmetryWidget();
            ActionToggleButton[] toggles = GetComponentsInChildren<ActionToggleButton>(true);
            for (int i = 0; i < toggles.Length; ++i)
            {
                if (toggles[i] == null)
                {
                    continue;
                }
                string name = toggles[i].gameObject.name;
                bool active = false;
                bool isSlotControl = true;
                int index = -1;
                bool angledOccupied = false;
                bool matchesCurrent = false;
                bool isMoveToTo = false;
                bool isTeleportDest = false;
                bool isArchitecturalTarget = false;
                bool isLastDrawn = false;
                bool isPreviousSource = false;
                bool isLiveDest = false;
                bool isMultiTarget = false;
                bool inConnectPool = false;
                bool isWasLive = false;

                // --- Hyperspace phase buttons (gold / purple) ---
                if (name == "ArchitectLineMode")
                {
                    active = widget != null && widget.ArchitectLineModeActive;
                    ForceMirrorControlsPanelButtonTint(
                        toggles[i],
                        active ? m_ArchitectLineModeTint : MirrorControlsInactiveTint);
                    continue;
                }
                if (name == "ArchitecturalTarget")
                {
                    active = widget != null && widget.ArchitecturalPickingTarget;
                    ForceMirrorControlsPanelButtonTint(
                        toggles[i],
                        active ? m_ArchitecturalTargetTint : MirrorControlsInactiveTint);
                    continue;
                }
                if (name == "ArchitecturalDraw")
                {
                    active = widget != null && (widget.ArchitecturalTargetArmed() || widget.ArchitecturalMultiTargetOn || widget.ArchitecturalConnectArmed);
                    ForceMirrorControlsPanelButtonTint(
                        toggles[i],
                        active ? m_ArchitecturalTargetTint : MirrorControlsInactiveTint);
                    continue;
                }
                if (name == "ArchitecturalMultiTarget")
                {
                    active = widget != null && widget.ArchitecturalMultiTargetOn;
                    ForceMirrorControlsPanelButtonTint(
                        toggles[i],
                        active ? m_ArchitecturalMultiTargetTint : MirrorControlsInactiveTint);
                    continue;
                }
                if (name == "ArchitecturalConnectAll")
                {
                    active = widget != null && widget.ArchitecturalConnectArmed;
                    ForceMirrorControlsPanelButtonTint(
                        toggles[i],
                        active ? m_ArchitecturalConnectAllTint : MirrorControlsInactiveTint);
                    continue;
                }
                if (name == "ReassignSlot")
                {
                    active = widget != null && widget.ReassignSlotModeActive;
                    ForceMirrorControlsPanelButtonTint(
                        toggles[i],
                        active ? m_ReassignSlotTint : MirrorControlsInactiveTint);
                    continue;
                }
                if (name == "ArchitecturalConnectDraw")
                {
                    active = widget != null && widget.ArchitecturalConnectArmed;
                    ForceMirrorControlsPanelButtonTint(
                        toggles[i],
                        active ? m_ArchitecturalConnectAllTint : MirrorControlsInactiveTint);
                    continue;
                }
                if (name == "ArchitecturalSequenceMode")
                {
                    active = widget != null && widget.ArchitecturalSequenceMode;
                    ForceMirrorControlsPanelButtonTint(
                        toggles[i],
                        active ? widget.ArchitecturalSequenceTint : MirrorControlsInactiveTint);
                    continue;
                }
                if (name == m_AxisFlashName && Time.time < m_AxisFlashUntil)
                {
                    ForceMirrorControlsPanelButtonTint(toggles[i], Color.yellow);
                    continue;
                }
                if (name == "HyperspaceMode")
                {
                    active = widget != null && widget.MoveToMirrorModeActive;
                    ForceMirrorControlsPanelButtonTint(
                        toggles[i],
                        active ? MirrorControlsMoveToMirrorToTint : MirrorControlsInactiveTint);
                    continue;
                }
                if (name == "SetJumpTarget")
                {
                    active = widget != null && widget.MoveToMirrorPickingTo;
                    ForceMirrorControlsPanelButtonTint(
                        toggles[i],
                        active ? MirrorControlsMoveToMirrorToTint : MirrorControlsInactiveTint);
                    continue;
                }
                if (name == "SendSelection")
                {
                    active = widget != null && widget.IsMoveToMirrorArmed();
                    ForceMirrorControlsPanelButtonTint(
                        toggles[i],
                        active ? MirrorControlsMoveToMirrorToTint : MirrorControlsInactiveTint);
                    continue;
                }
                if (name == "CloneMirrorGroup")
                {
                    active = widget != null && widget.IsMoveToMirrorArmed();
                    ForceMirrorControlsPanelButtonTint(
                        toggles[i],
                        active ? MirrorControlsMoveToMirrorToTint : MirrorControlsInactiveTint);
                    continue;
                }
                if (name == "TeleportToActive")
                {
                    // Dest purple / live yellow / confirmed blue / empty grey.
                    // Grey while architecture mode is on.
                    Color teleportTint = MirrorControlsInactiveTint;
                    if (widget != null && widget.ArchitectLineModeActive)
                    {
                        // contained
                    }
                    else if (widget != null && widget.PeekTeleportDestValid())
                    {
                        if (widget.IsMirrorSaveSlotMatchingCurrent(widget.TeleportDestIndex))
                        {
                            teleportTint = m_LiveDestTint;
                        }
                        else
                        {
                            teleportTint = widget.SlotGuideSelectedDestTint;
                        }
                    }
                    else if (widget != null && widget.LiveMirrorIsActive())
                    {
                        teleportTint = MirrorControlsCurrentSlotTint;
                    }
                    ForceMirrorControlsPanelButtonTint(toggles[i], teleportTint);
                    continue;
                }
                if (name == "MakeTargetSlotActive")
                {
                    active = widget != null
                        && widget.PeekTeleportDestValid()
                        && !widget.IsMirrorSaveSlotMatchingCurrent(widget.TeleportDestIndex);
                    ForceMirrorControlsPanelButtonTint(
                        toggles[i],
                        active ? widget.SlotGuideSelectedDestTint : MirrorControlsInactiveTint);
                    continue;
                }
                if (name == "SlotGuides")
                {
                    active = widget != null && widget.SlotGuidesToggledOn;
                    ForceMirrorControlsPanelButtonTint(
                        toggles[i],
                        active ? MirrorControlsActiveTint : MirrorControlsInactiveTint);
                    continue;
                }
                if (name == "PlaneLock")
                {
                    active = widget != null && widget.PlaneLockActive;
                    ForceMirrorControlsPanelButtonTint(
                        toggles[i],
                        active ? MirrorControlsMoveToMirrorToTint : MirrorControlsInactiveTint);
                    continue;
                }
                if (name == "TunnelLock")
                {
                    active = widget != null && widget.TunnelLockActive;
                    ForceMirrorControlsPanelButtonTint(
                        toggles[i],
                        active ? MirrorControlsMoveToMirrorToTint : MirrorControlsInactiveTint);
                    continue;
                }
                if (name == "LateralLock")
                {
                    active = widget != null && widget.LateralLockActive;
                    ForceMirrorControlsPanelButtonTint(
                        toggles[i],
                        active ? MirrorControlsMoveToMirrorToTint : MirrorControlsInactiveTint);
                    continue;
                }

                // --- Existing mirror controls ---
                if (name == "ClearSavedMirror")
                {
                    active = m_MirrorSaveSlotClearMode;
                }
                else if (name == "QuickRestore")
                {
                    active = widget != null && widget.HasMirrorQuickReturn;
                }
                else if (name == "ConstrainOrientation")
                {
                    active = widget != null && widget.LockOrientation;
                }
                else if (name == "LockSpin")
                {
                    active = widget != null && widget.LockSpin;
                }
                else if (name == "TrueCenter")
                {
                    bool atCenter = false;
                    if (widget != null)
                    {
                        TrTransform cur = App.Scene.AsScene[widget.transform];
                        TrTransform home = widget.GetMirrorTrueCenterPose_SS();
                        float eps = 0.01f * App.METERS_TO_UNITS;
                        atCenter =
                            (cur.translation - home.translation).sqrMagnitude <= eps * eps;
                    }
                    ForceMirrorControlsPanelButtonTint(
                        toggles[i],
                        atCenter
                            ? MirrorControlsInactiveTint
                            : Color.white);
                    continue;
                }
                else if (name.StartsWith("MirrorSaveSlot") && name.Length >= 16)
                {
                    int slotNumber;
                    if (int.TryParse(name.Substring(14), out slotNumber))
                    {
                        index = slotNumber;
                        active = widget != null && widget.HasMirrorSaveSlot(index);
                        if (active && widget != null)
                        {
                            matchesCurrent = widget.IsMirrorSaveSlotMatchingCurrent(index);
                            angledOccupied = !widget.IsMirrorSaveSlotAxisAligned(index);
                            isMultiTarget = widget.IsArchitecturalMultiTarget(index);
                            inConnectPool = widget.IsArchitecturalConnectPool(index);
                            isArchitecturalTarget =
                                widget.ArchitectLineModeActive
                                && ((widget.ArchitecturalTargetArmed()
                                    && widget.ArchitecturalTargetIndex == index)
                                    || isMultiTarget
                                    || inConnectPool);
                            isLastDrawn = widget.IsArchitecturalLastDrawn(index) && !matchesCurrent;
                            isPreviousSource = widget.IsArchitecturalPreviousSource(index) && !isLastDrawn;
                            isWasLive = widget.IsWasLive(index);
                            isMoveToTo =
                                widget.MoveToMirrorModeActive
                                && widget.PeekMoveToMirrorToValid()
                                && widget.MoveToMirrorToIndex == index
                                && !matchesCurrent;
                            // Hyperspace To is the jump/send target. Dest gold
                            // on a different slot was a second "active" target.
                            isTeleportDest =
                                !widget.MoveToMirrorModeActive
                                && widget.PeekTeleportDestValid()
                                && widget.TeleportDestIndex == index
                                && !matchesCurrent;
                            isLiveDest = matchesCurrent
                                && widget.PeekTeleportDestValid()
                                && widget.TeleportDestIndex == index;
                        }
                    }
                    else
                    {
                        isSlotControl = false;
                    }
                }
                else
                {
                    isSlotControl = false;
                }
                if (!isSlotControl)
                {
                    continue;
                }
                Color tint;
                if (!active)
                {
                    tint = MirrorControlsInactiveTint;
                }
                else if (widget != null && widget.IsMirrorSnapCrossing(index))
                {
                    tint = widget.MirrorSnapFlashTint;
                }
                else if (isLiveDest)
                {
                    tint = m_LiveDestTint;
                }
                else if (isArchitecturalTarget)
                {
                    tint = isMultiTarget && matchesCurrent
                        ? m_LiveSelectedTint
                        : (isMultiTarget
                            ? m_ArchitecturalMultiTargetTint
                            : (inConnectPool
                                ? m_ArchitecturalConnectAllTint
                                : m_ArchitecturalTargetTint));
                }
                else if (isMoveToTo)
                {
                    tint = MirrorControlsMoveToMirrorToTint;
                }
                else if (isTeleportDest)
                {
                    tint = widget.SlotGuideSelectedDestTint;
                }
                else if (isPreviousSource && matchesCurrent)
                {
                    tint = m_LiveSelectedTint;
                }
                else if (isPreviousSource)
                {
                    tint = m_ArchitecturalPreviousSourceTint;
                }
                else if (widget != null && widget.ReassignSlotModeActive && matchesCurrent)
                {
                    tint = m_ReassignSlotTint;
                }
                else if (isWasLive)
                {
                    tint = m_WasLiveTint;
                }
                else if (matchesCurrent)
                {
                    tint = MirrorControlsCurrentSlotTint;
                }
                else if (isLastDrawn)
                {
                    tint = MirrorControlsLastDrawnTint;
                }
                else if (angledOccupied)
                {
                    tint = MirrorControlsAngledSlotTint;
                }
                else
                {
                    tint = MirrorControlsActiveTint;
                }
                ForceMirrorControlsPanelButtonTint(toggles[i], tint);
            }
        }





        // ------- MIRROR SAVE SLOT SECTION COMPLETE ----------

        // METHODS FOR MIRROR TRANSLATION WITH OBJECTS AND BRUSH STROKES
        void OnDisable()
        {
            // Reset All Panels / hide only puts the pad away.
            // Hyperspace state stays on SymmetryWidget so From/To survive.
        }

        public void ToggleMoveToMirrorMode()
        {
            LogButtonPress("HyperspaceMode", "ToggleMoveToMirrorMode");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null)
            {
                return;
            }
            if (widget.MoveToMirrorModeActive)
            {
                widget.ExitMoveToMirrorMode();
            }
            else
            {
                m_MirrorSaveSlotClearMode = false;
                widget.EnterMoveToMirrorMode();
            }
            RefreshAllMirrorControlsTints();
        }

        public void BeginMoveToMirrorSetTo()
        {
            LogButtonPress("SetJumpTarget", "BeginMoveToMirrorSetTo");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null)
            {
                return;
            }
            if (!widget.MoveToMirrorModeActive)
            {
                return;
            }
            if (m_MirrorSaveSlotClearMode)
            {
                m_MirrorSaveSlotClearMode = false;
            }
            widget.BeginMoveToMirrorSetTo();
            RefreshAllMirrorControlsTints();
        }

        public void ApplyMoveToMirror()
        {
            LogButtonPress("SendSelection", "ApplyMoveToMirror");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null || !widget.IsMoveToMirrorArmed())
            {
                Debug.LogError(
                    "[MirrorControlsPanel.ApplyMoveToMirror] button=SendSelection skip unarmed");
                return;
            }
            SketchMemoryScript.m_Instance.PerformAndRecordCommand(
                new ApplyMirrorDestinationCommand());
            RefreshAllMirrorControlsTints();
        }


        public void JumpToMoveToMirrorDestination()
        {
            LogButtonPress("MakeTargetSlotActive", "JumpToMoveToMirrorDestination");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null || !widget.PeekTeleportDestValid())
            {
                Debug.LogError(
                    "[MirrorControlsPanel.JumpToMoveToMirrorDestination] button=MakeTargetSlotActive skip no dest slot");
                return;
            }

            widget.ExitMoveToMirrorMode(true);
            NavigateToMirrorCommand cmd = new NavigateToMirrorCommand();
            if (!cmd.IsValid)
            {
                Debug.LogError(
                    "[MirrorControlsPanel.JumpToMoveToMirrorDestination] button=MakeTargetSlotActive skip already there or invalid");
                RefreshAllMirrorControlsTints();
                return;
            }
            Debug.LogError(
                "[MirrorControlsPanel.JumpToMoveToMirrorDestination] button=MakeTargetSlotActive dest=" +
                widget.TeleportDestIndex);
            SketchMemoryScript.m_Instance.PerformAndRecordCommand(cmd);
            RefreshAllMirrorControlsTints();
        }

        //  SLOT GUIDES
        public void ToggleSlotGuides()
        {
            LogButtonPress("SlotGuides", "ToggleSlotGuides");
            s_PanelIsOpen = true;
            ShowSlot1IfMirrorModeOff();
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null)
            {
                return;
            }
            widget.ToggleSlotGuides();
            RefreshAllMirrorControlsTints();
        }

        public void TogglePlaneLock()
        {
            LogButtonPress("PlaneLock", "TogglePlaneLock");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null)
            {
                return;
            }
            widget.TogglePlaneLock();
            RefreshAllMirrorControlsTints();
        }

        public void ToggleRingTangentLock()
        {
            LogButtonPress("RingTangentLock", "ToggleRingTangentLock");
            if (PointerManager.m_Instance == null)
            {
                return;
            }

            PointerManager.m_Instance.ToggleRingTangentLock();
            RefreshMirrorControlsPanelTints();
        }

        PointerManager.RingOrientation RingOrientation()
        {
            if (PointerManager.m_Instance == null)
            {
                return PointerManager.RingOrientation.Default;
            }
            return PointerManager.m_Instance.m_RingOrientation;
        }

        void SetRing(PointerManager.RingOrientation orientation, string button)
        {
            LogButtonPress(button, button);
            if (PointerManager.m_Instance == null)
            {
                return;
            }
            PointerManager.m_Instance.SetRingOrientation(orientation);
            var buttons = GetComponentsInChildren<BaseButton>(true);
            string names = "";
            for (int i = 0; i < buttons.Length; ++i)
            {
                if (buttons[i] != null && buttons[i].gameObject.name.Contains("Ring"))
                {
                    names += buttons[i].gameObject.name + " ";
                }
            }
            Debug.LogError("[MirrorControlsPanel.SetRing] want=" + button + " found=" + names);
            RefreshAllMirrorControlsTints();
        }

        public void SetRingDefault()
        {
            SetRing(PointerManager.RingOrientation.Default, "RingDefault");
        }

        public void SetRingHorizontalForward()
        {
            SetRing(PointerManager.RingOrientation.HorizontalForward, "RingHorizontalForward");
        }

        public void SetRingVerticalForward()
        {
            SetRing(PointerManager.RingOrientation.VerticalForward, "RingVerticalForward");
        }

        public void SetRingHorizontalSideways()
        {
            SetRing(PointerManager.RingOrientation.HorizontalSideways, "RingHorizontalSideways");
        }

        public void SetRingVerticalSideways()
        {
            SetRing(PointerManager.RingOrientation.VerticalSideways, "RingVerticalSideways");
        }

        public void ToggleRingPlaneLock()
        {
            LogButtonPress("RingPlaneLock", "ToggleRingPlaneLock");
            if (PointerManager.m_Instance == null)
            {
                return;
            }

            PointerManager.m_Instance.ToggleRingPlaneLock();
            RefreshMirrorControlsPanelTints();
        }

        const float kSlotHeldAxisTolerance = 0.01f;

        public void DrawMirrorSlotSpan()
        {
            LogButtonPress("MirrorSlotSpan", "DrawMirrorSlotSpan");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null || PointerManager.m_Instance == null)
            {
                FlashSlotSpanMiss();
                return;
            }

            bool sideways = widget.CurrentPreferredOrientation == SymmetryWidget.PreferredOrientation.HorizontalSideways
                || widget.CurrentPreferredOrientation == SymmetryWidget.PreferredOrientation.VerticalSideways;
            Vector3 axis = sideways ? widget.transform.right : widget.transform.forward;
            bool found = widget.TryFindSlotAhead(
                widget.transform.position, axis, kSlotHeldAxisTolerance,
                out int slot, out Vector3 slotRoom, out float ahead, out float offAxis);
            if (!found)
            {
                Debug.LogError(
                    "[MirrorControlsPanel.DrawMirrorSlotSpan] button=MirrorSlotSpan miss axis=" + axis +
                    " facing=" + widget.CurrentPreferredOrientation);
                FlashSlotSpanMiss();
                return;
            }

            float room = Vector3.Distance(widget.transform.position, slotRoom);
            Debug.LogError(
                "[MirrorControlsPanel.DrawMirrorSlotSpan] button=MirrorSlotSpan " + SymmetryWidget.SlotLogLabel(slot) +
                " room=" + room + " ahead=" + ahead + " off=" + offAxis);
            if (!PointerManager.m_Instance.DrawSingleSpan(widget.transform.position, slotRoom))
            {
                FlashSlotSpanMiss();
            }
        }

        void FlashSlotSpanMiss()
        {
            ActionToggleButton button = FindNamedToggle("MirrorSlotSpan");
            if (button == null)
            {
                return;
            }

            ForceMirrorControlsPanelButtonTint(button, Color.red);
            StartCoroutine(ClearSlotSpanFlash(button));
        }

        IEnumerator ClearSlotSpanFlash(ActionToggleButton button)
        {
            yield return new WaitForSeconds(1.2f);
            if (button != null)
            {
                ForceMirrorControlsPanelButtonTint(button, kMirrorControlsPanelInactiveTint);
            }
        }

        ActionToggleButton FindNamedToggle(string name)
        {
            ActionToggleButton[] toggles = GetComponentsInChildren<ActionToggleButton>(true);
            for (int i = 0; i < toggles.Length; ++i)
            {
                if (toggles[i] != null && toggles[i].gameObject.name == name)
                {
                    return toggles[i];
                }
            }

            return null;
        }


        public void ToggleArchitectLineMode()
        {
            LogButtonPress("ArchitectLineMode", "ToggleArchitectLineMode");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null)
            {
                return;
            }
            if (widget.ReassignSlotModeActive)
            {
                widget.ExitReassignSlotMode();
            }
            if (widget.ArchitectLineModeActive)
            {
                widget.ExitArchitectLineMode();
                RefreshAllMirrorControlsTints();
            }
            else
            {
                m_MirrorSaveSlotClearMode = false;
                widget.EnterArchitectLineMode();
            }
            RefreshAllMirrorControlsTints();
        }

        public void ToggleReassignSlot()
        {
            LogButtonPress("ReassignSlot", "ToggleReassignSlot");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null)
            {
                return;
            }
            if (widget.MoveToMirrorModeActive)
            {
                return;
            }
            if (widget.ArchitectLineModeActive)
            {
                widget.ExitArchitectLineMode();
                RefreshAllMirrorControlsTints();
                return;
            }
            if (widget.ReassignSlotModeActive)
            {
                widget.ExitReassignSlotMode();
            }
            else
            {
                widget.EnterReassignSlotMode();
            }
            RefreshAllMirrorControlsTints();
        }

        public void BeginArchitecturalTarget()
        {
            LogButtonPress("ArchitecturalTarget", "BeginArchitecturalTarget");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null || !widget.ArchitectLineModeActive)
            {
                return;
            }
            widget.BeginArchitecturalTarget();
            RefreshAllMirrorControlsTints();
        }

        public void ToggleArchitecturalSequenceMode()
        {
            LogButtonPress("ArchitecturalSequenceMode", "ToggleArchitecturalSequenceMode");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null)
            {
                return;
            }
            widget.SetArchitecturalSequenceMode(!widget.ArchitecturalSequenceMode);
            RefreshAllMirrorControlsTints();
        }

        public void ToggleArchitecturalMultiTarget()
        {
            LogButtonPress("ArchitecturalMultiTarget", "ToggleArchitecturalMultiTarget");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null || !widget.ArchitectLineModeActive)
            {
                return;
            }
            widget.SetArchitecturalMultiTarget(!widget.ArchitecturalMultiTargetOn);
            RefreshAllMirrorControlsTints();
        }

        public void ArchitecturalConnectAll()
        {
            LogButtonPress("ArchitecturalConnectAll", "ArchitecturalConnectAll");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null || !widget.ArchitectLineModeActive)
            {
                return;
            }
            if (widget.ArchitecturalConnectArmed)
            {
                widget.ClearArchitecturalConnect();
                RefreshAllMirrorControlsTints();
                return;
            }
            int[] slots = widget.ArchitecturalMultiTargetOn
                ? widget.CopyArchitecturalTargets()
                : widget.CopyOccupiedSlots();
            widget.ArmArchitecturalConnect(slots);
            RefreshAllMirrorControlsTints();
        }

        public void ArchitecturalConnectDraw()
        {
            LogButtonPress("ArchitecturalConnectDraw", "ArchitecturalConnectDraw");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null || PointerManager.m_Instance == null || !widget.ArchitecturalConnectArmed)
            {
                Debug.LogError("[MirrorControlsPanel.ArchitecturalConnectDraw] miss unarmed");
                FlashArchitecturalDrawMiss();
                return;
            }
            int[] slots = widget.CopyArchitecturalConnectPool();
            if (widget.ArchitecturalSequenceMode) DrawArchitecturalSequence(widget, slots);
            else DrawArchitecturalPairs(widget, slots);
            widget.ClearArchitecturalConnect();
            widget.ClearArchitecturalMultiTarget();
            RefreshAllMirrorControlsTints();
        }

        void DrawArchitecturalSequence(SymmetryWidget widget, int[] slots)
        {
            if (widget == null || PointerManager.m_Instance == null || slots == null || slots.Length < 2)
            {
                Debug.LogError(
                    "[MirrorControlsPanel.ArchitecturalSequence] miss count=" +
                    (slots == null ? 0 : slots.Length));
                FlashArchitecturalDrawMiss();
                return;
            }
            string source = widget.ArchitecturalConnectArmed
                ? "connect"
                : (widget.ArchitecturalMultiTargetOn ? "multi" : "occupied");
            Debug.LogError(
                "[MirrorControlsPanel.ArchitecturalSequence] source=" + source +
                " count=" + slots.Length);
            for (int i = 0; i < slots.Length - 1; ++i)
            {
                Vector3 from;
                Vector3 to;
                if (!widget.TryGetSlotRoom(slots[i], out from) || !widget.TryGetSlotRoom(slots[i + 1], out to))
                {
                    continue;
                }
                Debug.LogError(
                    "[MirrorControlsPanel.ArchitecturalSequence] " +
                    SymmetryWidget.SlotLogLabel(slots[i]) + " -> " +
                    SymmetryWidget.SlotLogLabel(slots[i + 1]));
                DrawSlotPair(widget, slots[i], slots[i + 1], from, to);
            }
        }

        void DrawArchitecturalPairs(SymmetryWidget widget, int[] slots)
        {
            for (int s = 0; s < slots.Length; ++s)
            {
                Vector3 from;
                if (!widget.TryGetSlotRoom(slots[s], out from)) continue;
                TrTransform pose;
                widget.TryGetMirrorSaveSlotPose(slots[s], out pose);
                Debug.LogError(
                    "[MirrorControlsPanel.ArchitecturalConnectAll] source " +
                    SymmetryWidget.SlotLogLabel(slots[s]) +
                    " euler=" + pose.rotation.eulerAngles);
                for (int d = s + 1; d < slots.Length; ++d)
                {
                    Vector3 to;
                    if (!widget.TryGetSlotRoom(slots[d], out to)) continue;
                    DrawSlotPair(widget, slots[s], slots[d], from, to);
                }
            }
        }

        void DrawSlotPair(SymmetryWidget widget, int fromSlot, int toSlot, Vector3 fromCenter, Vector3 toCenter)
        {
            TrTransform sourcePose;
            TrTransform targetPose;
            Vector3[] fromRing;
            Vector3[] toRing;
            if (widget.TryGetMirrorSaveSlotPose(fromSlot, out sourcePose)
                && widget.TryGetMirrorSaveSlotPose(toSlot, out targetPose)
                && widget.TryGetPointSymmetrySpans(sourcePose, targetPose, out fromRing, out toRing))
            {
                int drawn = 0;
                for (int i = 0; i < fromRing.Length; ++i)
                {
                    if (PointerManager.m_Instance.DrawSingleSpan(fromRing[i], toRing[i]))
                    {
                        drawn++;
                    }
                }
                Debug.LogError(
                    "[MirrorControlsPanel.DrawSlotPair] ring " +
                    SymmetryWidget.SlotLogLabel(fromSlot) + " -> " +
                    SymmetryWidget.SlotLogLabel(toSlot) +
                    " spokes=" + fromRing.Length + " drawn=" + drawn);
                return;
            }
            Debug.LogError(
                "[MirrorControlsPanel.DrawSlotPair] center " +
                SymmetryWidget.SlotLogLabel(fromSlot) + " -> " +
                SymmetryWidget.SlotLogLabel(toSlot) +
                " mode=" + (PointerManager.m_Instance != null
                    ? PointerManager.m_Instance.CurrentSymmetryMode.ToString()
                    : "none"));
            PointerManager.m_Instance.DrawSingleSpan(fromCenter, toCenter);
        }

        public void ArchitecturalStackLast()
        {
            LogButtonPress("ArchitecturalStackLast", "ArchitecturalStackLast");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null || SketchMemoryScript.m_Instance == null)
            {
                return;
            }
            var sources = new System.Collections.Generic.List<Stroke>();
            foreach (Stroke stroke in widget.ArchitecturalSourceStrokes)
            {
                if (stroke != null && stroke.IsGeometryEnabled) sources.Add(stroke);
            }
            if (sources.Count == 0)
            {
                Debug.LogError("[MirrorControlsPanel.ArchitecturalStackLast] miss no strokes");
                FlashArchitecturalDrawMiss();
                return;
            }
            Vector3 axis = Vector3.up;
            string flash = "Up-Down-Y";
            switch (widget.CurrentMirrorSlideDefault)
            {
                case SymmetryWidget.MirrorSlideDefault.X:
                    axis = Vector3.right;
                    flash = "Left-Right-X";
                    break;
                case SymmetryWidget.MirrorSlideDefault.Z:
                    axis = Vector3.forward;
                    flash = "Forward-Back-Z";
                    break;
            }
            axis = App.Scene.Pose.rotation * axis;
            m_AxisFlashName = flash;
            m_AxisFlashUntil = Time.time + 0.33f;
            float gap = 0.15f * App.METERS_TO_UNITS;
            if (PointerManager.m_Instance != null && PointerManager.m_Instance.MainPointer != null)
            {
                gap = Mathf.Max(gap, PointerManager.m_Instance.MainPointer.BrushSizeAbsolute);
            }
            CanvasScript canvas = sources[0].Canvas;
            Vector3 axisCs = canvas.transform.InverseTransformDirection(axis);
            float sourceFar = FurthestOnAxisCs(sources, axisCs);
            float sourceNear = NearestOnAxisCs(sources, axisCs);
            float extent = sourceFar - sourceNear;
            float baseOffset = extent > 0.01f ? extent : gap;
            float copyFar = FurthestOnAxisCs(widget.ArchitecturalCopies, axisCs);
            float origin = sourceFar;
            if (!float.IsNegativeInfinity(copyFar)) origin = Mathf.Max(sourceFar, copyFar);
            float extra = Mathf.Max(0f, origin - sourceFar);
            Vector3 offsetCs = axisCs.normalized * (baseOffset + extra);
            var command = new ArchitecturalStackCommand(sources, TrTransform.T(offsetCs));
            SketchMemoryScript.m_Instance.PerformAndRecordCommand(command);
            foreach (Stroke stroke in command.Copies) widget.ArchitecturalCopies.Add(stroke);
            Debug.LogError(
                "[MirrorControlsPanel.ArchitecturalStackLast] axis=" + flash +
                " copies=" + command.Copies.Count +
                " extent=" + extent +
                " base=" + baseOffset +
                " extra=" + extra +
                " canvasScale=" + canvas.Pose.scale);
            RefreshAllMirrorControlsTints();
        }

        static float FurthestOnAxisCs(System.Collections.Generic.IList<Stroke> strokes, Vector3 axisCs)
        {
            float far = float.NegativeInfinity;
            for (int i = 0; i < strokes.Count; ++i)
            {
                Stroke stroke = strokes[i];
                if (stroke == null || !stroke.IsGeometryEnabled || stroke.m_ControlPoints == null) continue;
                for (int p = 0; p < stroke.m_ControlPoints.Length; ++p)
                {
                    far = Mathf.Max(far, Vector3.Dot(stroke.m_ControlPoints[p].m_Pos, axisCs));
                }
            }
            return far;
        }

        static float NearestOnAxisCs(System.Collections.Generic.IList<Stroke> strokes, Vector3 axisCs)
        {
            float near = float.PositiveInfinity;
            for (int i = 0; i < strokes.Count; ++i)
            {
                Stroke stroke = strokes[i];
                if (stroke == null || !stroke.IsGeometryEnabled || stroke.m_ControlPoints == null) continue;
                for (int p = 0; p < stroke.m_ControlPoints.Length; ++p)
                {
                    near = Mathf.Min(near, Vector3.Dot(stroke.m_ControlPoints[p].m_Pos, axisCs));
                }
            }
            return near;
        }

        static float FurthestOnAxis(System.Collections.Generic.IList<Stroke> strokes, Vector3 axis)
        {
            float far = float.NegativeInfinity;
            for (int i = 0; i < strokes.Count; ++i)
            {
                Stroke stroke = strokes[i];
                if (stroke == null || !stroke.IsGeometryEnabled || stroke.m_ControlPoints == null || stroke.Canvas == null) continue;
                TrTransform pose = stroke.Canvas.Pose;
                for (int p = 0; p < stroke.m_ControlPoints.Length; ++p)
                {
                    Vector3 room = pose.translation + pose.rotation * (stroke.m_ControlPoints[p].m_Pos * pose.scale);
                    far = Mathf.Max(far, Vector3.Dot(room, axis));
                }
            }
            return far;
        }

        static float NearestOnAxis(System.Collections.Generic.IList<Stroke> strokes, Vector3 axis)
        {
            float near = float.PositiveInfinity;
            for (int i = 0; i < strokes.Count; ++i)
            {
                Stroke stroke = strokes[i];
                if (stroke == null || !stroke.IsGeometryEnabled || stroke.m_ControlPoints == null || stroke.Canvas == null) continue;
                TrTransform pose = stroke.Canvas.Pose;
                for (int p = 0; p < stroke.m_ControlPoints.Length; ++p)
                {
                    Vector3 room = pose.translation + pose.rotation * (stroke.m_ControlPoints[p].m_Pos * pose.scale);
                    near = Mathf.Min(near, Vector3.Dot(room, axis));
                }
            }
            return near;
        }

        public void ArchitecturalDraw()
        {
            LogButtonPress("ArchitecturalDraw", "ArchitecturalDraw");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget != null)
            {
                int liveSlot = -1;
                for (int i = 0; i < SymmetryWidget.kMirrorSaveSlotCount; ++i)
                {
                    if (widget.IsMirrorSaveSlotMatchingCurrent(i))
                    {
                        liveSlot = i;
                        break;
                    }
                }
                TrTransform live = App.Scene.AsScene[widget.transform];
                TrTransform trueCenter = widget.GetMirrorTrueCenterPose_SS();
                TrTransform slot1;
                bool haveSlot1 = widget.TryGetMirrorSaveSlotPose(1, out slot1);
                Debug.LogError(
                    "[MirrorControlsPanel.ArchitecturalDraw] liveSlot=" +
                    SymmetryWidget.SlotLogLabel(liveSlot) +
                    " livePos=" + live.translation +
                    " trueCenterPos=" + trueCenter.translation +
                    " slot1Pos=" + (haveSlot1 ? slot1.translation.ToString() : "empty") +
                    " slot1Match=" + (haveSlot1 && widget.IsMirrorSaveSlotMatchingCurrent(1)));
            }
            if (widget != null) widget.BeginArchitecturalStrokeCapture();
            if (widget != null && widget.ArchitecturalConnectArmed)
            {
                int[] pool = widget.CopyArchitecturalConnectPool();
                Debug.LogError("[MirrorControlsPanel.ArchitecturalDraw] connect count=" + pool.Length);
                if (widget.ArchitecturalSequenceMode)
                {
                    DrawArchitecturalSequence(widget, pool);
                    if (pool.Length > 0) { widget.NoteArchitecturalLastDrawn(pool[pool.Length - 1]); widget.ClearWasLive(); }
                    widget.UnlockArchitecturalSource();
                }
                else DrawArchitecturalPairs(widget, pool);
                GroupLastArchitecturalStrokes(widget);
                widget.ClearArchitecturalConnect();
                widget.ClearArchitecturalMultiTarget();
                RefreshAllMirrorControlsTints();
                return;
            }
            if (widget != null && widget.ArchitecturalMultiTargetOn)
            {
                int[] slots = widget.CopyArchitecturalTargets();
                if (widget.ArchitecturalSequenceMode)
                {
                    DrawArchitecturalSequence(widget, slots);
                    if (slots.Length > 0) { widget.NoteArchitecturalLastDrawn(slots[slots.Length - 1]); widget.ClearWasLive(); }
                    widget.UnlockArchitecturalSource();
                    GroupLastArchitecturalStrokes(widget);
                    widget.SetArchitecturalMultiTarget(false);
                    RefreshAllMirrorControlsTints();
                    return;
                }
                TrTransform livePose = App.Scene.AsScene[widget.transform];
                for (int i = 0; i < slots.Length; ++i)
                {
                    if (widget.IsMirrorSaveSlotMatchingCurrent(slots[i])) continue;
                    Vector3 to;
                    if (!widget.TryGetSlotRoom(slots[i], out to)) continue;
                    Debug.LogError(
                        "[MirrorControlsPanel.ArchitecturalDraw] multi " +
                        SymmetryWidget.SlotLogLabel(slots[i]));
                    TrTransform targetPose;
                    Vector3[] fromRing;
                    Vector3[] toRing;
                    if (widget.TryGetMirrorSaveSlotPose(slots[i], out targetPose)
                        && widget.TryGetPointSymmetrySpans(livePose, targetPose, out fromRing, out toRing))
                    {
                        int drawn = 0;
                        for (int s = 0; s < fromRing.Length; ++s)
                        {
                            if (PointerManager.m_Instance.DrawSingleSpan(fromRing[s], toRing[s])) drawn++;
                        }
                        Debug.LogError(
                            "[MirrorControlsPanel.ArchitecturalDraw] multi ring " +
                            SymmetryWidget.SlotLogLabel(slots[i]) +
                            " spokes=" + fromRing.Length + " drawn=" + drawn);
                        continue;
                    }
                    Vector3 fromRoom = (App.Scene.Pose * livePose).translation;
                    PointerManager.m_Instance.DrawSingleSpan(fromRoom, to);
                    Debug.LogError(
                        "[MirrorControlsPanel.ArchitecturalDraw] multi center " +
                        SymmetryWidget.SlotLogLabel(slots[i]));
                }
                GroupLastArchitecturalStrokes(widget);
                widget.SetArchitecturalMultiTarget(false);
                RefreshAllMirrorControlsTints();
                return;
            }
            if (widget == null || PointerManager.m_Instance == null || !widget.ArchitecturalTargetArmed())
            {
                Debug.LogError("[MirrorControlsPanel.ArchitecturalDraw] button=ArchitecturalDraw miss unarmed");
                FlashArchitecturalDrawMiss();
                return;
            }
            Vector3 slotRoom;
            if (!widget.TryGetArchitecturalTargetRoom(out slotRoom))
            {
                Debug.LogError("[MirrorControlsPanel.ArchitecturalDraw] button=ArchitecturalDraw miss no pose");
                FlashArchitecturalDrawMiss();
                return;
            }
            TrTransform pose = widget.GetArchitecturalTargetPose_SS();
            Debug.LogError(
                "[MirrorControlsPanel.ArchitecturalDraw] button=ArchitecturalDraw " +
                SymmetryWidget.SlotLogLabel(widget.ArchitecturalTargetIndex) +
                " widget=" + widget.GetEntityId() +
                " live=" + (PointerManager.m_Instance != null && PointerManager.m_Instance.SymmetryWidget != null
                    ? PointerManager.m_Instance.SymmetryWidget.GetEntityId().ToString()
                    : "null") +
                " euler=" + pose.rotation.eulerAngles);
            int source = -1;
            for (int i = 0; i < SymmetryWidget.kMirrorSaveSlotCount; ++i)
            {
                if (widget.IsMirrorSaveSlotMatchingCurrent(i))
                {
                    source = i;
                    break;
                }
            }
            bool drew = DrawPointSymmetrySpans(widget, pose, slotRoom);
            if (drew)
            {
                GroupLastArchitecturalStrokes(widget);
                widget.NoteArchitecturalLastDrawn(widget.ArchitecturalTargetIndex); widget.ClearWasLive();
                widget.UnlockArchitecturalSource();
            }
            widget.ClearArchitecturalTarget();
            RefreshAllMirrorControlsTints();
            if (!drew)
            {
                FlashArchitecturalDrawMiss();
            }
        }

        bool DrawPointSymmetrySpans(SymmetryWidget widget, TrTransform targetScene, Vector3 targetRoom)
        {
            PointerManager pointers = PointerManager.m_Instance;
            if (pointers.CurrentSymmetryMode != PointerManager.SymmetryMode.MultiMirror || pointers.m_CustomSymmetryType != PointerManager.CustomSymmetryType.Point)
            {
                return pointers.DrawSingleSpan(widget.transform.position, targetRoom);
            }
            var matrices = pointers.CustomMirrorMatrices;
            if (matrices == null || matrices.Count == 0)
            {
                return false;
            }
            widget.RefreshArchitecturalMarkers();
            int drawn = 0;
            int skipped = 0;
            float sceneScale = App.Scene != null ? App.Scene.Pose.scale : 1.0f;
            int count = matrices.Count;
            var used = new System.Collections.Generic.List<Vector3>();
            for (int i = 0; i < count; ++i)
            {
                Vector3 from;
                Vector3 to;
                if (!widget.TryGetArchitecturalMarkerSpan(i, out from, out to))
                {
                    Debug.LogError("[MirrorControlsPanel.DrawPointSymmetrySpans] no target spoke=" + i);
                    continue;
                }
                if (SpanAlreadyDrawn(used, from, to, sceneScale))
                {
                    skipped++;
                    continue;
                }
                Debug.LogError(
                    "[MirrorControlsPanel.DrawPointSymmetrySpans] spoke=" + i
                    + " marker=true sceneScale=" + sceneScale
                    + " from=" + from
                    + " to=" + to);
                if (pointers.DrawSingleSpan(from, to))
                {
                    drawn++;
                    used.Add(from);
                    used.Add(to);
                }
            }
            Debug.LogError(
                "[MirrorControlsPanel.DrawPointSymmetrySpans] family=" + pointers.m_PointSymmetryFamily
                + " order=" + pointers.m_PointSymmetryOrder
                + " count=" + count
                + " drawn=" + drawn
                + " skipped=" + skipped
                + " marker=true sceneScale=" + sceneScale);
            return drawn > 0;
        }

        static bool SpanAlreadyDrawn(System.Collections.Generic.List<Vector3> used, Vector3 from, Vector3 to, float sceneScale)
        {
            float scale = Mathf.Max(sceneScale, 1e-4f);
            const float eps = 0.01f;
            float eps2 = eps * eps;
            Vector3 fromScene = from / scale;
            Vector3 toScene = to / scale;
            for (int i = 0; i + 1 < used.Count; i += 2)
            {
                Vector3 usedFrom = used[i] / scale;
                Vector3 usedTo = used[i + 1] / scale;
                bool same = (usedFrom - fromScene).sqrMagnitude < eps2 && (usedTo - toScene).sqrMagnitude < eps2;
                bool flip = (usedFrom - toScene).sqrMagnitude < eps2 && (usedTo - fromScene).sqrMagnitude < eps2;
                if (same || flip) return true;
            }
            return false;
        }

        void GroupLastArchitecturalStrokes(SymmetryWidget widget)
        {
            if (widget == null || App.GroupManager == null) return;
            var strokes = widget.ArchitecturalSourceStrokes;
            int count = 0;
            for (int i = 0; i < strokes.Count; ++i)
            {
                if (strokes[i] != null) count++;
            }
            if (count < 2) return;
            SketchGroupTag group = App.GroupManager.NewUnusedGroup();
            for (int i = 0; i < strokes.Count; ++i)
            {
                if (strokes[i] != null) strokes[i].Group = group;
            }
            Debug.LogError("[MirrorControlsPanel.GroupLastArchitecturalStrokes] count=" + count);
        }

        void FlashArchitecturalDrawMiss()
        {
            ActionToggleButton button = FindNamedToggle("ArchitecturalDraw");
            if (button == null)
            {
                return;
            }
            ForceMirrorControlsPanelButtonTint(button, Color.red);
            StartCoroutine(ClearSlotSpanFlash(button));
        }

        public void ToggleTunnelLock()
        {
            LogButtonPress("TunnelLock", "ToggleTunnelLock");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null)
            {
                return;
            }
            widget.ToggleTunnelLock();
            RefreshAllMirrorControlsTints();
        }

        public void ToggleLateralLock()
        {
            LogButtonPress("LateralLock", "ToggleLateralLock");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null)
            {
                return;
            }
            widget.ToggleLateralLock();
            RefreshAllMirrorControlsTints();
        }

        public void SummonLiveMirror()
        {
            LogButtonPress("SummonLiveMirror", "SummonLiveMirror");
            if (SketchControlsScript.m_Instance == null)
            {
                return;
            }

            SketchControlsScript.m_Instance.IssueGlobalCommand(
                SketchControlsScript.GlobalCommands.SummonMirror);
        }

        public void TeleportUserToActiveMirror()
        {
            LogButtonPress("TeleportToActive", "TeleportUserToActiveMirror");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null || widget.ArchitectLineModeActive
                || !widget.PeekTeleportDestValid())
            {
                Debug.LogError(
                    "[MirrorControlsPanel.TeleportUserToActiveMirror] button=TeleportToActive skip no dest slot");
                return;
            }
            widget.ExitMoveToMirrorMode(true);
            TeleportToMirrorCommand cmd = new TeleportToMirrorCommand();
            if (!cmd.IsValid)
            {
                Debug.LogError(
                    "[MirrorControlsPanel.TeleportUserToActiveMirror] button=TeleportToActive skip command invalid");
                RefreshAllMirrorControlsTints();
                return;
            }
            Debug.LogError(
                "[MirrorControlsPanel.TeleportUserToActiveMirror] button=TeleportToActive dest=" +
                widget.TeleportDestIndex);
            SketchMemoryScript.m_Instance.PerformAndRecordCommand(cmd);
            RefreshAllMirrorControlsTints();
        }

        public void CloneMoveToMirror()
        {
            LogButtonPress("CloneMirrorGroup", "CloneMoveToMirror");
            SymmetryWidget widget = FindSymmetryWidget();
            if (widget == null || !widget.IsMoveToMirrorArmed())
            {
                Debug.LogError(
                    "[MirrorControlsPanel.CloneMoveToMirror] button=CloneMirrorGroup skip unarmed");
                return;
            }
            CloneMirrorGroupCommand cloneCmd = new CloneMirrorGroupCommand();
            if (!cloneCmd.IsValid)
            {
                Debug.LogError(
                    "[MirrorControlsPanel.CloneMoveToMirror] CLONE skip invalid");
                return;
            }
            SketchMemoryScript.m_Instance.PerformAndRecordCommand(cloneCmd);
            widget.ClearMoveToMirrorTo();
            RefreshAllMirrorControlsTints();
        }


    } // Functions Complete
    public class ArchitecturalStackCommand : BaseCommand
    {
        readonly Stroke[] m_Copies;
        readonly TrTransform m_Offset;
        public System.Collections.Generic.IList<Stroke> Copies { get { return m_Copies; } }

        public ArchitecturalStackCommand(System.Collections.Generic.IList<Stroke> sources, TrTransform offsetCs, BaseCommand parent = null)
            : base(parent)
        {
            m_Offset = offsetCs;
            var copies = new System.Collections.Generic.List<Stroke>();
            CanvasScript active = App.Scene != null ? App.Scene.ActiveCanvas : null;
            for (int i = 0; i < sources.Count; ++i)
            {
                Stroke source = sources[i];
                if (source == null || active == null) continue;
                copies.Add(SketchMemoryScript.m_Instance.DuplicateStroke(source, active, offsetCs, absoluteScale: true));
            }
            m_Copies = copies.ToArray();
        }

        public override bool NeedsSave { get { return true; } }

        protected override void OnRedo()
        {
            int batched = 0;
            int canvasMatch = 0;
            CanvasScript active = App.Scene != null ? App.Scene.ActiveCanvas : null;
            for (int i = 0; i < m_Copies.Length; ++i)
            {
                Stroke stroke = m_Copies[i];
                stroke.Group = App.GroupManager.NewUnusedGroup();
                bool hasSubset = stroke.m_Type == Stroke.Type.BatchedBrushStroke && stroke.m_BatchSubset != null;
                bool matches = hasSubset && stroke.Canvas == active;
                if (hasSubset)
                {
                    batched++;
                    stroke.m_BatchSubset.m_ParentBatch.EnableSubset(stroke.m_BatchSubset);
                }
                else if (stroke.m_Object != null)
                {
                    BaseBrushScript brush = stroke.m_Object.GetComponent<BaseBrushScript>();
                    if (brush != null) brush.HideBrush(false);
                }
                if (matches) canvasMatch++;
                Debug.LogError(
                    "[ArchitecturalStackCommand.OnRedo] index=" + i +
                    " group=" + stroke.Group +
                    " canvas=" + (stroke.Canvas != null ? stroke.Canvas.name : "null"));
                if (!hasSubset || !matches)
                {
                    Debug.LogError(
                        "[ArchitecturalStackCommand.OnRedo] miss index=" + i +
                        " type=" + stroke.m_Type +
                        " hasSubset=" + hasSubset +
                        " canvas=" + (stroke.Canvas != null ? stroke.Canvas.name : "null") +
                        " active=" + (active != null ? active.name : "null") +
                        " group=" + stroke.Group);
                }
            }
            Debug.LogError(
                "[ArchitecturalStackCommand.OnRedo] copies=" + m_Copies.Length +
                " batched=" + batched +
                " canvasMatch=" + canvasMatch +
                " offset=" + m_Offset.translation);
        }

        protected override void OnUndo()
        {
            for (int i = 0; i < m_Copies.Length; ++i)
            {
                Stroke stroke = m_Copies[i];
                stroke.Group = SketchGroupTag.None;
                if (stroke.m_Type == Stroke.Type.BatchedBrushStroke && stroke.m_BatchSubset != null)
                {
                    stroke.m_BatchSubset.m_ParentBatch.DisableSubset(stroke.m_BatchSubset);
                }
                else if (stroke.m_Object != null)
                {
                    BaseBrushScript brush = stroke.m_Object.GetComponent<BaseBrushScript>();
                    if (brush != null) brush.HideBrush(true);
                }
            }
        }
    }

} //Namespace TiltBrush

