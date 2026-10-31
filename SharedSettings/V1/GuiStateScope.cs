using System;
using UnityEngine;
namespace Skoofidons.SharedSettings.V1
{
    public sealed class GuiStateScope : IDisposable
    {
        private readonly Matrix4x4 matrix=GUI.matrix;
        private readonly Color color=GUI.color,background=GUI.backgroundColor,content=GUI.contentColor;
        private readonly bool enabled=GUI.enabled;
        private readonly GUISkin skin=GUI.skin;
        private readonly int depth=GUI.depth;
        public void Dispose()
        {
            GUI.matrix=matrix;GUI.color=color;GUI.backgroundColor=background;GUI.contentColor=content;
            GUI.enabled=enabled;GUI.skin=skin;GUI.depth=depth;
        }
    }
}
