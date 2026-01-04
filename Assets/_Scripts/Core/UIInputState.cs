// Game.Core
namespace Game.Core
{
    public static class UIInputState
    {
        public static bool IsDragging { get; set; }
        public static void SetDragging(bool value) => IsDragging = value;
    }
}
