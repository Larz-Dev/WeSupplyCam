using System;
using System.IO;
using System.Reflection;
using PdfSharp.Fonts;

public class CustomFontResolver : IFontResolver
{
    public byte[] GetFont(string faceName)
    {
        string fontFile = "Resources.Fonts.ARIAL.TTF"; // Nombre exacto de tu fuente

        var assembly = Assembly.GetExecutingAssembly();
        using (Stream stream = assembly.GetManifestResourceStream(fontFile))
        {
            if (stream == null)
                throw new InvalidOperationException($"No se encontró la fuente '{fontFile}'");

            byte[] fontData = new byte[stream.Length];
            stream.Read(fontData, 0, fontData.Length);
            return fontData;
        }
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        return new FontResolverInfo("ArialCustom");
    }
}
