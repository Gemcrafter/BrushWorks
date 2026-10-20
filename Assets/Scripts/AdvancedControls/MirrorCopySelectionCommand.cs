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

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TiltBrush
{
    /// Copies the current selection across the live mirror.
    /// SinglePlane: one reflection. MultiMirror: every non-original slot, widget-centered.
    /// No identity clone, no ClipboardManager duplicate offset.
    public class MirrorCopySelectionCommand : BaseCommand
    {
        private List<Stroke> m_SourceStrokes;
        private List<Stroke> m_MirroredStrokes;
        private CanvasScript m_CurrentCanvas;

        public int CreatedCount => m_MirroredStrokes == null ? 0 : m_MirroredStrokes.Count;

        public MirrorCopySelectionCommand(BaseCommand parent = null) : base(parent)
        {
            m_CurrentCanvas = App.ActiveCanvas;
            m_SourceStrokes = SelectionManager.m_Instance.SelectedStrokes.ToList();
            m_MirroredStrokes = new List<Stroke>();
            Debug.LogError(
                "[MirrorCopySelectionCommand] enter mode=" +
                (PointerManager.m_Instance != null
                    ? PointerManager.m_Instance.CurrentSymmetryMode.ToString() : "none") +
                " sources=" + m_SourceStrokes.Count);

            if (m_SourceStrokes.Count == 0)
            {
                Debug.LogError(
                    "[MirrorCopySelectionCommand] skip noStrokes mode=" +
                    (PointerManager.m_Instance != null
                        ? PointerManager.m_Instance.CurrentSymmetryMode.ToString() : "none"));
                return;
            }

            if (PointerManager.m_Instance == null ||
                PointerManager.m_Instance.SymmetryWidget == null)
            {
                Debug.LogError("MirrorCopySelectionCommand: SymmetryWidget is null");
                return;
            }

            // Match DuplicateSelectionCommand canvas-space conversion.
            TrTransform xfGSfromCS = App.Scene.SelectionCanvas.Pose;
            TrTransform xfCSfromGS = m_CurrentCanvas.Pose.inverse;
            PointerManager pm = PointerManager.m_Instance;

            if (pm.CurrentSymmetryMode == PointerManager.SymmetryMode.MultiMirror)
            {
                // Same center and matrix rule as PointerManager.GetSymmetryTransformFor.
                // Index 0 is the original. Copies use the live widget, not the controller.
                if (pm.CustomMirrorMatrices == null || pm.CustomMirrorMatrices.Count < 2)
                {
                    Debug.LogError(
                        "MirrorCopySelectionCommand: MultiMirror matrices=" +
                        (pm.CustomMirrorMatrices == null ? -1 : pm.CustomMirrorMatrices.Count));
                    return;
                }

                TrTransform xfCenter = TrTransform.FromTransform(pm.SymmetryWidget.transform);
                int copies = 0;
                for (int child = 1; child < pm.CustomMirrorMatrices.Count; ++child)
                {
                    (TrTransform, TrTransform) trAndFix =
                        pm.TrFromMatrixWithFixedReflections(pm.CustomMirrorMatrices[child]);
                    TrTransform tr = xfCenter * trAndFix.Item1 * xfCenter.inverse;
                    TrTransform xfCopyCS = xfCSfromGS * tr * xfGSfromCS * trAndFix.Item2;
                    foreach (var stroke in m_SourceStrokes)
                    {
                        m_MirroredStrokes.Add(
                            SketchMemoryScript.m_Instance.DuplicateStroke(
                                stroke, m_CurrentCanvas, xfCopyCS, absoluteScale: true));
                    }
                    copies++;
                }

                Debug.LogError(
                    "[MirrorCopySelectionCommand] mode=MultiMirror sources=" +
                    m_SourceStrokes.Count + " slots=" + copies +
                    " strokes=" + m_MirroredStrokes.Count);
            }
            else
            {
                // Global-space reflection only (same source as SinglePlane's non-identity entry).
                TrTransform xfReflectGS = pm.SymmetryWidget.ReflectionPlane.ToTrTransform();
                TrTransform xfReflectCS = xfCSfromGS * xfReflectGS * xfGSfromCS;

                foreach (var stroke in m_SourceStrokes)
                {
                    m_MirroredStrokes.Add(
                        SketchMemoryScript.m_Instance.DuplicateStroke(
                            stroke, m_CurrentCanvas, xfReflectCS, absoluteScale: true));
                }

                Debug.LogError(
                    "[MirrorCopySelectionCommand] mode=" + pm.CurrentSymmetryMode +
                    " sources=" + m_SourceStrokes.Count +
                    " strokes=" + m_MirroredStrokes.Count);
            }

            GroupManager.MoveStrokesToNewGroups(m_MirroredStrokes, null);
        }

        public override bool NeedsSave
        {
            get
            {
                return true;
            }
        }

        protected override void OnRedo()
        {
            foreach (var stroke in m_MirroredStrokes)
            {
                switch (stroke.m_Type)
                {
                    case Stroke.Type.BrushStroke:
                        {
                            BaseBrushScript brushScript = stroke.m_Object.GetComponent<BaseBrushScript>();
                            if (brushScript)
                            {
                                brushScript.HideBrush(false);
                            }
                        }
                        break;
                    case Stroke.Type.BatchedBrushStroke:
                        {
                            stroke.m_BatchSubset.m_ParentBatch.EnableSubset(stroke.m_BatchSubset);
                        }
                        break;
                    default:
                        Debug.LogError("Unexpected: redo NotCreated mirror-copy stroke");
                        break;
                }
                TiltMeterScript.m_Instance.AdjustMeter(stroke, up: true);
            }
        }

        protected override void OnUndo()
        {
            foreach (var stroke in m_MirroredStrokes)
            {
                switch (stroke.m_Type)
                {
                    case Stroke.Type.BrushStroke:
                        {
                            BaseBrushScript brushScript = stroke.m_Object.GetComponent<BaseBrushScript>();
                            if (brushScript)
                            {
                                brushScript.HideBrush(true);
                            }
                        }
                        break;
                    case Stroke.Type.BatchedBrushStroke:
                        {
                            stroke.m_BatchSubset.m_ParentBatch.DisableSubset(stroke.m_BatchSubset);
                        }
                        break;
                    default:
                        Debug.LogError("Unexpected: undo NotCreated mirror-copy stroke");
                        break;
                }
                TiltMeterScript.m_Instance.AdjustMeter(stroke, up: false);
            }
        }
    }
}