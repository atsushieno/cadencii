public static class Print_MetaText
{
    public static bool Edit(Cadencii.Media.Vsq.VsqFile vsq)
    {
        vsq.Track[1].printMetaText(@"c:\meta_text.txt", "Shift_JIS");
        return true;
    }
}