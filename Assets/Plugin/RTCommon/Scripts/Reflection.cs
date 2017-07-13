﻿using System;
using System.Reflection;
using System.Linq;
using UnityEngine;
using Battlehub.RTSaveLoad;

namespace Battlehub
{
    public class UnityEditorOnly : System.Attribute
    {
    }


    public static class Reflection
    {
        public static bool IsScript(this Type type)
        {
            return type.IsSubclassOf(typeof(MonoBehaviour));
        }

        public static PropertyInfo[] GetSerializableProperties(this Type type)
        {
            return type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).
                Where(p => IsSerializable(p)).ToArray();
        }


        public static FieldInfo[] GetSerializableFields(this Type type)
        {
            return type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).
                Where(f => IsSerializable(f)).ToArray();
        }

        private static bool IsSerializable(this FieldInfo field)
        {
            return field.IsPublic || field.IsDefined(typeof(SerializeField), false) && !field.IsDefined(typeof(UnityEditorOnly), false);
        }

        private static bool IsSerializable(this PropertyInfo property)
        {
            return property.CanWrite && property.GetSetMethod(/*nonPublic*/ true).IsPublic &&
                   property.CanRead && property.GetGetMethod(true).IsPublic 
                || property.IsDefined(typeof(SerializeField), false) && !property.IsDefined(typeof(UnityEditorOnly), false);
        }

        public static Type[] GetAllFromCurrentAssembly()
        {
#if !UNITY_WINRT || UNITY_EDITOR
            var types = typeof(Reflection).Assembly.GetTypes();
#else
            var types = typeof(Reflection).GetTypeInfo().Assembly.GetTypes();
#endif
            return types.ToArray();
        }

        public static Type[] GetAssignableFromTypes(Type type)
        {
#if !UNITY_WINRT || UNITY_EDITOR
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && p.IsClass);
#else
            var types = type.GetTypeInfo().Assembly.GetTypes().
                Where(p => type.IsAssignableFrom(p) && p.GetTypeInfo().IsClass);
#endif
            return types.ToArray();
        }


#if UNITY_WINRT && !UNITY_EDITOR
        public static Type BaseType(this Type type)
        {
            return type.GetTypeInfo().BaseType;
        }

        public static bool IsValueType(this Type type)
        {
              return type.GetTypeInfo().IsValueType;
        }

        public static bool IsPrimitive(this Type type)
        {
              return type.GetTypeInfo().IsPrimitive;
        }

         public static bool IsArray(this Type type)
        {
            return type.GetTypeInfo().IsArray;
        }

        public static bool IsGenericType(this Type type)
        {
            return type.GetTypeInfo().IsGenericType;
        }

        public static bool IsEnum(this Type type)
        {
            return type.GetTypeInfo().IsEnum;
        }

        public static bool IsAssignableFrom(this Type type, Type fromType)
        {
            return type.GetTypeInfo().IsAssignableFrom(fromType.GetTypeInfo());
        }

        public static bool IsSubclassOf(this Type type, Type ofType)
        {
            return type.GetTypeInfo().IsSubclassOf(ofType);
        }

        public static bool IsDefined(this Type type, Type attributeType, bool inherit)
        {
            return type.GetTypeInfo().IsDefined(attributeType, inherit);
        }

        public static object[] GetCustomAttributes(this Type type, Type attributeType, bool inherit)
        {
            return type.GetTypeInfo().GetCustomAttributes(attributeType, inherit).ToArray();
        }

         public static bool IsClass(this Type type)
        {
            return type.GetTypeInfo().IsClass;
        }
#else
        public static Type BaseType(this Type type)
        {
            return type.BaseType;      
        }

        public static bool IsValueType(this Type type)
        {
            return type.IsValueType;
        }

        public static bool IsPrimitive(this Type type)
        {
            return type.IsPrimitive;
        }

        public static bool IsArray(this Type type)
        {
            return type.IsArray;
        }

        public static bool IsGenericType(this Type type)
        {
            return type.IsGenericType;
        }

        public static bool IsEnum(this Type type)
        {
            return type.IsEnum;
        }

        public static bool IsClass(this Type type)
        {
            return type.IsClass;
        }

#endif
    }

}
