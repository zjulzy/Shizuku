namespace Shizuku.Tag
{
    public static class TagExtension
    {
        public static bool CheckIsParent(this uint tagParent,  uint tagChild)
        {
            return tagParent != tagChild;
        }
    }
}