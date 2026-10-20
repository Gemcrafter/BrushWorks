using UnityEngine;

namespace TiltBrush
{
    public class LiveTargetRing : MonoBehaviour
    {
        [SerializeField] Color m_Color = new Color(0.7f, 0.3f, 1f, 1f);
        [SerializeField] float m_Radius = 0.6f;
        [SerializeField] float m_Width = 0.02f;
        [SerializeField] int m_Segments = 48;

        LineRenderer m_Line;

        void Awake()
        {
            m_Line = GetComponent<LineRenderer>();
            if (m_Line == null)
            {
                m_Line = gameObject.AddComponent<LineRenderer>();
            }
            m_Line.useWorldSpace = false;
            m_Line.loop = true;
            m_Line.startWidth = m_Width;
            m_Line.endWidth = m_Width;
            m_Line.material = new Material(Shader.Find("Sprites/Default"));
            m_Line.startColor = m_Color;
            m_Line.endColor = m_Color;
            Rebuild();
        }

        void OnValidate()
        {
            if (m_Line == null)
            {
                m_Line = GetComponent<LineRenderer>();
            }
            if (m_Line != null)
            {
                m_Line.startWidth = m_Width;
                m_Line.endWidth = m_Width;
                m_Line.startColor = m_Color;
                m_Line.endColor = m_Color;
                Rebuild();
            }
        }

        void Rebuild()
        {
            if (m_Line == null)
            {
                return;
            }
            m_Line.positionCount = m_Segments;
            for (int i = 0; i < m_Segments; ++i)
            {
                float a = (float)i / m_Segments * Mathf.PI * 2f;
                m_Line.SetPosition(i, new Vector3(Mathf.Cos(a) * m_Radius, 0f, Mathf.Sin(a) * m_Radius));
            }
        }
    }
}
