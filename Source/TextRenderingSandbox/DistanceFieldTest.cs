
namespace TextRenderingSandbox
{
    class DistanceFieldTest
    {
        // preparation:
        //
        // int padding = 5; // TODO: add smoothing control, maybe range from 1 to 10 (0 = no smooth)
        // 
        // // TODO: add thickness control (127 is a sweet spot for accuracy)
        // // maybe range between -1 and 1 that is mapped to "127 + [-10 and 10]"
        // byte onedge = 127;
        // 
        // byte* sdfPixels = StbTrueType.GetCodepointSDF(
        //     fontInfo, scale, codepoint, padding, onedge, onedge / (float)padding,
        //     out int sdfWidth, out int sdfHeight, out var sdfOffset);


        // rendering:
        // 
        // float spread = 5;
        // float smoothing = 0.25f / (spread * scale);
        // effect.Parameters["Smoothing"].SetValue(smoothing);
    }
}
