﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using dnlib.DotNet;
using Dynamitey;
using ImpromptuInterface;

namespace SharpILMixins.Processor.Utils
{
    public static class CustomAttributeUtils
    {
        public static T? GetCustomAttribute<T>(this IHasCustomAttribute provider) where T : class
        {
            var attribute =
                provider.CustomAttributes.FirstOrDefault(attr =>
                {
                    var definition = attr.AttributeType.ResolveTypeDef();
                    return attr.AttributeType.FullName == typeof(T).FullName || definition?.BaseType != null && definition.BaseType.FullName == typeof(T).FullName;
                });

            if (attribute == null) return null;

            var type = typeof(T).Assembly.GetType(attribute.AttributeType.FullName);
            var constructorInfos = type?.GetConstructors();
            if (constructorInfos == null) return null;
            foreach (var constructor in constructorInfos)
            {
                try
                {
                    var values = FixValues(attribute.ConstructorArguments, constructor).ToArray();

                    return constructor?.Invoke(values) as T;
                }
                catch (Exception e)
                {
                    // ignored
                }
            }

            return null;
        }

        private static IEnumerable<object?> FixValues(IList<CAArgument> constructorArguments,
            MethodBase constructor)
        {
            for (var i = 0; i < constructorArguments.Count; i++)
            {
                var argument = constructorArguments[i];
                var obj = argument.Value;

                if (obj is TypeRef type)
                    yield return type.FullName;
                else if (obj is CorLibTypeSig corLibType)
                    yield return corLibType.FullName;
                else
                {

                    yield return Cast(obj, constructor.GetParameters()[i].ParameterType);
                }
            }
        }
        public static object? Cast(object data, Type type)
        {
            var dataParam = Expression.Parameter(typeof(object), "data");
            var block = Expression.Block(Expression.Convert(Expression.Convert(dataParam, data.GetType()), type));

            var compile = Expression.Lambda(block, dataParam).Compile();
            var ret = compile.DynamicInvoke(data);
            return ret;
        }

    }

}