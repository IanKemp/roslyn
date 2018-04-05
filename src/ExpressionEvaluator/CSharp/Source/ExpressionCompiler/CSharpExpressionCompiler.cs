﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    [DkmReportNonFatalWatsonException(ExcludeExceptionType = typeof(NotImplementedException)), DkmContinueCorruptingException]
    internal sealed class CSharpExpressionCompiler : ExpressionCompiler
    {
        private static readonly DkmCompilerId s_compilerId = new DkmCompilerId(DkmVendorId.Microsoft, DkmLanguageId.CSharp);

        public CSharpExpressionCompiler(): base(new CSharpFrameDecoder(), new CSharpLanguageInstructionDecoder())
        {
        }

        internal override DiagnosticFormatter DiagnosticFormatter
        {
            get { return DebuggerDiagnosticFormatter.Instance; }
        }

        internal override DkmCompilerId CompilerId
        {
            get { return s_compilerId; }
        }

        internal delegate AppDomainMetadataContext<CSharpCompilation, EvaluationContext> GetMetadataContextDelegate<TAppDomain>(TAppDomain appDomain);
        internal delegate void SetMetadataContextDelegate<TAppDomain>(TAppDomain appDomain, AppDomainMetadataContext<CSharpCompilation, EvaluationContext> metadataContext);

        internal override EvaluationContextBase CreateTypeContext(
            DkmClrAppDomain appDomain,
            ImmutableArray<MetadataBlock> metadataBlocks,
            Guid moduleVersionId,
            int typeToken,
            bool useReferencedModulesOnly)
        {
            return CreateTypeContext(
                appDomain,
                ad => ad.GetMetadataContext<AppDomainMetadataContext<CSharpCompilation, EvaluationContext>>(),
                metadataBlocks,
                moduleVersionId,
                typeToken,
                GetMakeAssemblyReferencesKind(useReferencedModulesOnly));
        }

        internal static EvaluationContext CreateTypeContext<TAppDomain>(
            TAppDomain appDomain,
            GetMetadataContextDelegate<TAppDomain> getMetadataContext,
            ImmutableArray<MetadataBlock> metadataBlocks,
            Guid moduleVersionId,
            int typeToken,
            MakeAssemblyReferencesKind kind)
        {
            if (kind == MakeAssemblyReferencesKind.DirectReferencesOnly)
            {
                // Avoid using the cache for referenced assemblies only
                // since this should be the exceptional case.
                var compilation = metadataBlocks.ToCompilationReferencedModulesOnly(moduleVersionId);
                return EvaluationContext.CreateTypeContext(
                    compilation,
                    moduleVersionId,
                    typeToken);
            }

            var previous = getMetadataContext(appDomain);
            if (!previous.Matches(metadataBlocks))
            {
                previous = null;
            }
            if (previous != null && previous.ModuleVersionId != moduleVersionId)
            {
                previous = null;
            }

            var previousContext = previous?.AssemblyContext;
            var context = EvaluationContext.CreateTypeContext(
                previousContext,
                metadataBlocks,
                moduleVersionId,
                typeToken,
                kind);

            // New type context is not attached to the AppDomain since it is less
            // re-usable than the previous attached method context. (We could hold
            // on to it if we don't have a previous method context but it's unlikely
            // that we evaluated a type-level expression before a method-level.)
            Debug.Assert(context != previousContext?.EvaluationContext);

            return context;
        }

        internal override EvaluationContextBase CreateMethodContext(
            DkmClrAppDomain appDomain,
            ImmutableArray<MetadataBlock> metadataBlocks,
            Lazy<ImmutableArray<AssemblyReaders>> unusedLazyAssemblyReaders,
            object symReader,
            Guid moduleVersionId,
            int methodToken,
            int methodVersion,
            uint ilOffset,
            int localSignatureToken,
            bool useReferencedModulesOnly)
        {
            return CreateMethodContext(
                appDomain,
                ad => ad.GetMetadataContext<AppDomainMetadataContext<CSharpCompilation, EvaluationContext>>(),
                (ad, mc) => ad.SetMetadataContext<AppDomainMetadataContext<CSharpCompilation, EvaluationContext>>(mc),
                metadataBlocks,
                symReader,
                moduleVersionId,
                methodToken,
                methodVersion,
                ilOffset,
                localSignatureToken,
                GetMakeAssemblyReferencesKind(useReferencedModulesOnly));
        }

        internal static EvaluationContext CreateMethodContext<TAppDomain>(
            TAppDomain appDomain,
            GetMetadataContextDelegate<TAppDomain> getMetadataContext,
            SetMetadataContextDelegate<TAppDomain> setMetadataContext,
            ImmutableArray<MetadataBlock> metadataBlocks,
            object symReader,
            Guid moduleVersionId,
            int methodToken,
            int methodVersion,
            uint ilOffset,
            int localSignatureToken,
            MakeAssemblyReferencesKind kind)
        {
            if (kind == MakeAssemblyReferencesKind.DirectReferencesOnly)
            {
                // Avoid using the cache for referenced assemblies only
                // since this should be the exceptional case.
                var compilation = metadataBlocks.ToCompilationReferencedModulesOnly(moduleVersionId);
                return EvaluationContext.CreateMethodContext(
                    compilation,
                    symReader,
                    moduleVersionId,
                    methodToken,
                    methodVersion,
                    ilOffset,
                    localSignatureToken);
            }

            var previous = getMetadataContext(appDomain);
            if (!previous.Matches(metadataBlocks))
            {
                previous = null;
            }
            if (previous != null && previous.ModuleVersionId != moduleVersionId) // TODO: ModuleVersionId should be compared against default if AllAssemblies.
            {
                previous = null;
            }

            var previousContext = previous?.AssemblyContext;
            var context = EvaluationContext.CreateMethodContext(
                previousContext,
                metadataBlocks,
                symReader,
                moduleVersionId,
                methodToken,
                methodVersion,
                ilOffset,
                localSignatureToken,
                kind);

            if (context != previousContext?.EvaluationContext)
            {
                setMetadataContext(
                    appDomain,
                    new AppDomainMetadataContext<CSharpCompilation, EvaluationContext>(
                        metadataBlocks,
                        moduleVersionId,
                        new CSharpMetadataContext(context.Compilation, context)));
            }

            return context;
        }

        internal override void RemoveDataItem(DkmClrAppDomain appDomain)
        {
            appDomain.RemoveMetadataContext<AppDomainMetadataContext<CSharpCompilation, EvaluationContext>>();
        }

        internal override ImmutableArray<MetadataBlock> GetMetadataBlocks(DkmClrAppDomain appDomain, DkmClrRuntimeInstance runtimeInstance)
        {
            var previous = appDomain.GetMetadataContext<AppDomainMetadataContext<CSharpCompilation, EvaluationContext>>();
            return runtimeInstance.GetMetadataBlocks(appDomain, previous.MetadataBlocks);
        }
    }
}
