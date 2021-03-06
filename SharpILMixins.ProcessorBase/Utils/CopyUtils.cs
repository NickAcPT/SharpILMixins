﻿using dnlib.DotNet;
using SharpILMixins.Processor.Workspace;

namespace SharpILMixins.Processor.Utils
{
    public static class CopyUtils
    {
        public static MethodDefUser CopyMethod(MethodDef original, MixinWorkspace workspace,
            TypeDef? declaringType = null, bool copyAttributes = true)
        {
            var redirectManager = workspace.RedirectManager;
            var copyMethod = new MethodDefUser(original.Name, original.MethodSig, original.ImplAttributes,
                original.Attributes)
            {
                Body = original.Body
            };

            foreach (var bodyVariable in original.Body.Variables)
            {
                bodyVariable.Type =
                    redirectManager.ProcessTypeRedirect(bodyVariable.Type, original.DeclaringType.DefinitionAssembly);
            }

            if (original.HasGenericParameters)
            {
                foreach (var genericParam in original.GenericParameters)
                    copyMethod.GenericParameters.Add(new GenericParamUser(genericParam.Number, genericParam.Flags,
                        genericParam.Name));
            }   

            copyMethod.ReturnType =
                redirectManager.ProcessTypeRedirect(copyMethod.ReturnType, declaringType?.DefinitionAssembly);
            foreach (var parameter in copyMethod.Parameters)
                parameter.Type = redirectManager.ProcessTypeRedirect(parameter.Type, declaringType?.DefinitionAssembly);

            copyMethod.ParamDefs.Clear();
            foreach (var originalParamDef in original.ParamDefs)
                copyMethod.ParamDefs.Add(new ParamDefUser(originalParamDef.Name, originalParamDef.Sequence,
                    originalParamDef.Attributes));

            foreach (var methodOverride in original.Overrides) copyMethod.Overrides.Add(methodOverride);

            if (declaringType != null) copyMethod.DeclaringType = declaringType;
            if (copyAttributes) copyMethod.Attributes = original.Attributes;
            return copyMethod;
        }
    }
}