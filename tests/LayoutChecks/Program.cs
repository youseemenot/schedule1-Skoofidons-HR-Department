using System;
using SkoofidonsHRDepartment;

int passed = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception(name); passed++; }
var one = ClipboardLayoutPolicy.Calculate(500f, 140f, new[] { 48f }, new[] { 300f });
Check(!one.OuterScroll, "one list fits without outer scrolling");
Check(one.Sections[0].ViewportTop == 188f && one.Sections[0].Height == 304f, "one list consumes actual remaining panel height");
var two = ClipboardLayoutPolicy.Calculate(500f, 60f, new[] { 48f, 80f }, new[] { 300f, 190f });
Check(!two.OuterScroll, "Handler fits two readable viewports");
Check(two.Sections[0].Height == 146f && two.Sections[1].Height == 146f, "Handler shares residual budget equally");
Check(two.Sections[1].Top == 266f && two.Sections[1].ViewportTop == 346f, "routes follow station viewport and their own headings");
Check(two.Sections[1].ViewportTop + two.Sections[1].Height == 492f, "normal layout stays above panel bottom padding");
var tiny = ClipboardLayoutPolicy.Calculate(220f, 60f, new[] { 48f, 80f }, new[] { 300f, 190f });
Check(tiny.OuterScroll, "tiny panel uses one outer scrolling region");
Check(tiny.Sections[0].Height == 300f && tiny.Sections[1].Height == 190f, "outer scrolling exposes complete row content without inner scrolling");
Check(tiny.ContentHeight == 698f, "outer content includes static area, headings, rows, gap and padding");
foreach (float height in new[] { -1f, 0f, 1f, 220f, 400f, 1000f, float.NaN, float.PositiveInfinity })
{
    var layout = ClipboardLayoutPolicy.Calculate(height, 60f, new[] { 48f, 80f }, new[] { 300f, 190f });
    foreach (var section in layout.Sections)
    {
        Check(float.IsFinite(section.Height) && section.Height >= 60f, "nonnegative readable list geometry");
        Check(section.ViewportTop >= section.Top && section.ViewportTop + section.Height <= layout.ContentHeight, "every viewport bounded by its content");
    }
}
var exact = ClipboardLayoutPolicy.Calculate(328f, 60f, new[] { 48f, 80f }, new[] { 300f, 190f });
Check(!exact.OuterScroll && exact.Sections[0].Height == 60f, "exact readable minimum stays in normal mode");
var invalid = ClipboardLayoutPolicy.Calculate(200f, float.NaN, new[] { float.NaN }, new[] { float.PositiveInfinity });
Check(float.IsFinite(invalid.ContentHeight), "invalid dimensions cannot poison native geometry");
Console.WriteLine($"PASS {passed} adaptive layout checks");
