using System;
using System.Collections.Generic;
using System.Linq;
using System.Composition;


public static class ExportFactoryExtensions
{
    public static IEnumerable<Lazy<T, TMetadata>> ToLazy<T, TMetadata>(this IEnumerable<ExportFactory<T, TMetadata>> factories)
    {
        return factories.Select(factory =>
            new Lazy<T, TMetadata>(
                () => factory.CreateExport().Value,
                factory.Metadata
            ));
    }
}
