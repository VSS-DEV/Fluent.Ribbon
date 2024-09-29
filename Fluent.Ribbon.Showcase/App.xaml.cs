namespace FluentTest;

using System.Windows;
using ControlzEx.Theming;

public partial class App
{
    public App()
    {
        //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("ru");

        //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("fa");
        //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("ru");
        //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("de");
        //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("hu");
        //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("cs");
        //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("fr");
        //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("pl");
        //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("ja");
        //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("nl");
        //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("pt");
        //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("pt-br");
        //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("es");
        //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("zh");
        //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("sv");
        //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("sk");
        //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("uk");
        //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("ro");
        //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("it");
        //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("ar");
        //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("da");
        //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("az");

        System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
    }

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
        ThemeManager.Current.SyncTheme();

        ThemeManager.Current.ThemeChanged += HandleThemeChanged;

#pragma warning disable CS0618 // Type or member is obsolete
        AppModeHelper.SyncAppMode();
#pragma warning restore CS0618 // Type or member is obsolete

        base.OnStartup(e);

        return;

        void HandleThemeChanged(object? sender, ThemeChangedEventArgs themeChangedEventArgs)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            AppModeHelper.SyncAppMode();
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}