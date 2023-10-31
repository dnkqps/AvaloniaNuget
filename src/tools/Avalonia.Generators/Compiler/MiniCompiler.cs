﻿using System;
using System.Collections.Generic;
using System.Linq;
using XamlX;
using XamlX.Compiler;
using XamlX.Emit;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace Avalonia.Generators.Compiler;

internal sealed class MiniCompiler : XamlCompiler<object, IXamlEmitResult>
{
    public const string AvaloniaXmlnsDefinitionAttribute = "Avalonia.Metadata.XmlnsDefinitionAttribute";

    public static MiniCompiler CreateDefault(RoslynTypeSystem typeSystem, params string[] additionalTypes)
    {
        var mappings = new XamlLanguageTypeMappings(typeSystem);
        foreach (var additionalType in additionalTypes)
            mappings.XmlnsAttributes.Add(typeSystem.GetType(additionalType));

        var diagnosticsHandler = new XamlDiagnosticsHandler
        {
            // Elevate all errors to fatal, so generators build would stop right away. 
            HandleDiagnostic = diagnostic =>
                diagnostic.Severity == XamlDiagnosticSeverity.Error ?
                    XamlDiagnosticSeverity.Fatal :
                    diagnostic.Severity
        };
        
        var configuration = new TransformerConfiguration(
            typeSystem,
            typeSystem.Assemblies.First(),
            mappings,
            diagnosticsHandler: diagnosticsHandler);
        return new MiniCompiler(configuration);
    }
        
    private MiniCompiler(TransformerConfiguration configuration)
        : base(configuration, new XamlLanguageEmitMappings<object, IXamlEmitResult>(), false)
    {
        Transformers.Add(new NameDirectiveTransformer());
        Transformers.Add(new DataTemplateTransformer());
        Transformers.Add(new KnownDirectivesTransformer());
        Transformers.Add(new XamlIntrinsicsTransformer());
        Transformers.Add(new XArgumentsTransformer());
        Transformers.Add(new TypeReferenceResolver());
    }

    protected override XamlEmitContext<object, IXamlEmitResult> InitCodeGen(
        IFileSource file,
        Func<string, IXamlType,
        IXamlTypeBuilder<object>> createSubType,
        Func<string, IXamlType, IEnumerable<IXamlType>,
        IXamlTypeBuilder<object>> createDelegateType,
        object codeGen,
        XamlRuntimeContext<object, IXamlEmitResult> context,
        bool needContextLocal) =>
        throw new NotSupportedException();
}
