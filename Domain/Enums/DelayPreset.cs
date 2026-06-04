namespace Domain.Enums
{
    public enum DelayPreset
    {
        Safe = 1,
        Normal = 2,
        Fast = 3
    }

    public static class DelayPresetExtensions
    {
        public static (int From, int To) ToSeconds(this DelayPreset preset) => preset switch
        {
            DelayPreset.Safe   => (5, 10),
            DelayPreset.Normal => (3, 5),
            DelayPreset.Fast   => (1, 3),
            _                  => (3, 5)
        };
    }
}
