namespace SkoofidonsHRDepartment
{
    public readonly struct ClipboardSection
    {
        public readonly float Top, ViewportTop, Height;
        public ClipboardSection(float top, float viewportTop, float height) { Top = top; ViewportTop = viewportTop; Height = height; }
    }
    public sealed class ClipboardLayout
    {
        public bool OuterScroll;
        public float ContentHeight;
        public ClipboardSection[] Sections;
    }
    public static class ClipboardLayoutPolicy
    {
        public static ClipboardLayout Calculate(float panelHeight, float startTop, float[] headings, float[] contentHeights)
        {
            const float minimum = 60f, gap = 12f, bottom = 8f;
            panelHeight = FiniteSize(panelHeight);
            startTop = FiniteSize(startTop);
            int count = headings.Length;
            float reserved = startTop + bottom + System.Math.Max(0, count - 1) * gap;
            foreach (float heading in headings) reserved += FiniteSize(heading);
            bool outer = panelHeight < reserved + count * minimum;
            float height = count == 0 ? 0f : System.Math.Max(minimum, (panelHeight - reserved) / count);
            var sections = new ClipboardSection[count];
            float cursor = startTop;
            for (int i = 0; i < count; i++)
            {
                float listHeight = outer ? System.Math.Max(minimum, FiniteSize(contentHeights[i])) : height;
                float viewportTop = cursor + FiniteSize(headings[i]);
                sections[i] = new ClipboardSection(cursor, viewportTop, listHeight);
                cursor = viewportTop + listHeight + (i + 1 < count ? gap : 0f);
            }
            return new ClipboardLayout { OuterScroll = outer, ContentHeight = System.Math.Max(panelHeight, cursor + bottom), Sections = sections };
        }

        private static float FiniteSize(float size) => float.IsFinite(size) ? System.Math.Clamp(size, 0f, 1000000f) : 0f;
    }
}
