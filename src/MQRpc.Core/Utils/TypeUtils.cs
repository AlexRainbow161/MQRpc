using MQRpc.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MQRpc.Core.Utils
{
    public static class TypeUtils
    {
        private static readonly Dictionary<string, Type> _typeDefinitions = new Dictionary<string, Type>();
        private static readonly Dictionary<Type, Type> _serviceDefinitions = new Dictionary<Type, Type>();

        private static bool _firstRun = true;

        public static IEnumerable<KeyValuePair<string, Type>> TypeDefinitions => _typeDefinitions;
        public static IEnumerable<KeyValuePair<Type, Type>> ServiceDefinitions => _serviceDefinitions;
        public static void ScanAsseblyForHandlers(Type assemblyMarker)
        {
            if (!_firstRun) return;
            var handlerTypes = assemblyMarker.Assembly
                .GetTypes()
                .Where(type => type.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
                .ToList();

            foreach (var handlerType in handlerTypes)
            {
                var interfaceTypes = handlerType.GetInterfaces().Where(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)).ToList();
                foreach (var interfaceType in interfaceTypes)
                {
                    var genericArguments = interfaceType.GetGenericArguments();
                    var typeNamesPair = string.Concat(genericArguments[0].FullName, "^",
                        genericArguments[1].FullName);
                    _typeDefinitions.Add(typeNamesPair, interfaceType);
                    _serviceDefinitions.Add(interfaceType, handlerType);
                }
            }

            _firstRun = false;
        }

        public static Type? ResolveServiceType(string typeDefinition)
        {
            return !_typeDefinitions.TryGetValue(typeDefinition,
                out var serviceType) ? null : serviceType;
        }

        public static (Type, Type) GetGenericServiceArguments(Type type)
        {
            var arguments = type.GetGenericArguments();
            return (arguments[0], arguments[1]);
        }
    }
}