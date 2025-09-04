using Microsoft.Extensions.Logging;
using PdfSharp.Fonts;

namespace WeSupplyCam
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

         
            string fontPath = FileFontResolver.CopyFontToLocalPath().Result;
            GlobalFontSettings.FontResolver = new FileFontResolver(fontPath);

#if DEBUG
            builder.Logging.AddDebug();
#endif
            return builder.Build();
        }
    }

    public class FileFontResolver : IFontResolver
    {
        private readonly string _fontPath;

        public FileFontResolver(string fontPath)
        {
            _fontPath = fontPath;
        }

        public string DefaultFontName => "ArialCustom";

        public byte[] GetFont(string faceName)
        {
            if (File.Exists(_fontPath))
                return File.ReadAllBytes(_fontPath);

            throw new FileNotFoundException($"Fuente no encontrada: {_fontPath}");
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            if (familyName.Equals("Arial", StringComparison.CurrentCultureIgnoreCase))
                return new FontResolverInfo("ArialCustom");

            return null;
        }

        public static async Task<string> CopyFontToLocalPath()
        {
            string localFontPath = Path.Combine(FileSystem.AppDataDirectory, "ARIAL.TTF");

            if (!File.Exists(localFontPath))
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("ARIAL.TTF");
                using var fileStream = File.Create(localFontPath);
                await stream.CopyToAsync(fileStream);
            }

            return localFontPath;
        }
    }
}
