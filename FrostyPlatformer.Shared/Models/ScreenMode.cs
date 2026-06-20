namespace FrostyPlatformer.Models
{
    /// <summary>
    /// Fönsterläge för spelet. Persistas i <see cref="SettingsObj.ScreenMode"/> och
    /// appliceras av WindowManager vid uppstart och via settings-menyn/F11.
    /// </summary>
    public enum ScreenMode
    {
        /// <summary>Fast fönster med ram (1024×896). Standard under utveckling.</summary>
        Windowed,

        /// <summary>Ramlös helskärm i skärmens nativa upplösning — utan hardware mode switch.</summary>
        BorderlessFullscreen
    }
}
