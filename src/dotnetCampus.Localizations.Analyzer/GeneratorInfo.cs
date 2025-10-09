using System;
using System.Linq;
using dotnetCampus.Localizations.Utils.IO;

namespace dotnetCampus.Localizations;

internal static class GeneratorInfo
{
    public static readonly string RootNamespace = typeof(GeneratorInfo).Namespace!;

    public static EmbeddedSourceFile GetEmbeddedTemplateFile<TReferenceType>()
    {
        var typeName = typeof(TReferenceType).Name;
        var templateNamespace = typeof(TReferenceType).Namespace!;
        var templatesFolder = templateNamespace.AsSpan().Slice(GeneratorInfo.RootNamespace.Length + 1).ToString();
        var embeddedFile = EmbeddedSourceFiles.Enumerate(templatesFolder)
            .Single(x => x.TypeName == typeName);
        return embeddedFile;
    }
}
