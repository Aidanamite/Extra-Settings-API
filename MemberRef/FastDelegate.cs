using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

public unsafe class FastFunc<R> : FastDelegateBase
{
    delegate*<R> _method;
    public FastFunc(delegate*<R> target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));
        _method = target;
    }
    public FastFunc(Type type, string name, bool ignoreCase = false)
    {
        _method = (delegate*<R>)GetPointer(CheckGet(type,name,ignoreCase,typeof(R)));
    }
    public FastFunc(MethodInfo method)
    {
        Check(method, typeof(R));
        _method = (delegate*<R>)GetPointer(method);
    }
    public R Invoke() => _method();
}
public unsafe class FastFunc<A, R> : FastDelegateBase
{
    delegate*<A, R> _method;
    public FastFunc(delegate*<A, R> target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));
        _method = target;
    }
    public FastFunc(Type type, string name, bool ignoreCase = false)
    {
        _method = (delegate*<A, R>)GetPointer(CheckGet(type, name, ignoreCase, typeof(R), typeof(A)));
    }
    public FastFunc(MethodInfo method)
    {
        Check(method, typeof(R), typeof(A));
        _method = (delegate*<A, R>)GetPointer(method);
    }
    public R Invoke(A arg0) => _method(arg0);
}
public unsafe class FastFunc<A, B, R> : FastDelegateBase
{
    delegate*<A, B, R> _method;
    public FastFunc(delegate*<A, B, R> target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));
        _method = target;
    }
    public FastFunc(Type type, string name, bool ignoreCase = false)
    {
        _method = (delegate*<A, B, R>)GetPointer(CheckGet(type, name, ignoreCase, typeof(R), typeof(A), typeof(B)));
    }
    public FastFunc(MethodInfo method)
    {
        Check(method, typeof(R), typeof(A), typeof(B));
        _method = (delegate*<A, B, R>)GetPointer(method);
    }
    public R Invoke(A arg0, B arg1) => _method(arg0, arg1);
}
public unsafe class FastFunc<A, B, C, R> : FastDelegateBase
{
    delegate*<A, B, C, R> _method;
    public FastFunc(delegate*<A, B, C, R> target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));
        _method = target;
    }
    public FastFunc(Type type, string name, bool ignoreCase = false)
    {
        _method = (delegate*<A, B, C, R>)GetPointer(CheckGet(type, name, ignoreCase, typeof(R), typeof(A), typeof(B), typeof(C)));
    }
    public FastFunc(MethodInfo method)
    {
        Check(method, typeof(R), typeof(A), typeof(B), typeof(C));
        _method = (delegate*<A, B, C, R>)GetPointer(method);
    }
    public R Invoke(A arg0, B arg1, C arg2) => _method(arg0, arg1, arg2);
}
public unsafe class FastFunc<A, B, C, D, R> : FastDelegateBase
{
    delegate*<A, B, C, D, R> _method;
    public FastFunc(delegate*<A, B, C, D, R> target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));
        _method = target;
    }
    public FastFunc(Type type, string name, bool ignoreCase = false)
    {
        _method = (delegate*<A, B, C, D, R>)GetPointer(CheckGet(type, name, ignoreCase, typeof(R), typeof(A), typeof(B), typeof(C), typeof(D)));
    }
    public FastFunc(MethodInfo method)
    {
        Check(method, typeof(R), typeof(A), typeof(B), typeof(C), typeof(D));
        _method = (delegate*<A, B, C, D, R>)GetPointer(method);
    }
    public R Invoke(A arg0, B arg1, C arg2, D arg3) => _method(arg0, arg1, arg2, arg3);
}
public unsafe class FastFunc<A, B, C, D, E, R> : FastDelegateBase
{
    delegate*<A, B, C, D, E, R> _method;
    public FastFunc(delegate*<A, B, C, D, E, R> target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));
        _method = target;
    }
    public FastFunc(Type type, string name, bool ignoreCase = false)
    {
        _method = (delegate*<A, B, C, D, E, R>)GetPointer(CheckGet(type, name, ignoreCase, typeof(R), typeof(A), typeof(B), typeof(C), typeof(D), typeof(E)));
    }
    public FastFunc(MethodInfo method)
    {
        Check(method, typeof(R), typeof(A), typeof(B), typeof(C), typeof(D), typeof(E));
        _method = (delegate*<A, B, C, D, E, R>)GetPointer(method);
    }
    public R Invoke(A arg0, B arg1, C arg2, D arg3, E arg4) => _method(arg0, arg1, arg2, arg3, arg4);
}
public unsafe class FastAction : FastDelegateBase
{
    delegate*<void> _method;
    public FastAction(delegate*<void> target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));
        _method = target;
    }
    public FastAction(Type type, string name, bool ignoreCase = false)
    {
        _method = (delegate*<void>)GetPointer(CheckGet(type, name, ignoreCase, typeof(void)));
    }
    public FastAction(MethodInfo method)
    {
        Check(method, typeof(void));
        _method = (delegate*<void>)GetPointer(method);
    }
    public void Invoke() => _method();
}
public unsafe class FastAction<A> : FastDelegateBase
{
    delegate*<A, void> _method;
    public FastAction(delegate*<A, void> target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));
        _method = target;
    }
    public FastAction(Type type, string name, bool ignoreCase = false)
    {
        _method = (delegate*<A, void>)GetPointer(CheckGet(type, name, ignoreCase, typeof(void), typeof(A)));
    }
    public FastAction(MethodInfo method)
    {
        Check(method, typeof(void), typeof(A));
        _method = (delegate*<A, void>)GetPointer(method);
    }
    public void Invoke(A arg0) => _method(arg0);
}
public unsafe class FastAction<A, B> : FastDelegateBase
{
    delegate*<A, B, void> _method;
    public FastAction(delegate*<A, B, void> target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));
        _method = target;
    }
    public FastAction(Type type, string name, bool ignoreCase = false)
    {
        _method = (delegate*<A, B, void>)GetPointer(CheckGet(type, name, ignoreCase, typeof(void), typeof(A), typeof(B)));
    }
    public FastAction(MethodInfo method)
    {
        Check(method, typeof(void), typeof(A), typeof(B));
        _method = (delegate*<A, B, void>)GetPointer(method);
    }
    public void Invoke(A arg0, B arg1) => _method(arg0, arg1);
}
public unsafe class FastAction<A, B, C> : FastDelegateBase
{
    delegate*<A, B, C, void> _method;
    public FastAction(delegate*<A, B, C, void> target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));
        _method = target;
    }
    public FastAction(Type type, string name, bool ignoreCase = false)
    {
        _method = (delegate*<A, B, C, void>)GetPointer(CheckGet(type, name, ignoreCase, typeof(void), typeof(A), typeof(B), typeof(C)));
    }
    public FastAction(MethodInfo method)
    {
        Check(method, typeof(void), typeof(A), typeof(B), typeof(C));
        _method = (delegate*<A, B, C, void>)GetPointer(method);
    }
    public void Invoke(A arg0, B arg1, C arg2) => _method(arg0, arg1, arg2);
}
public unsafe class FastAction<A, B, C, D> : FastDelegateBase
{
    delegate*<A, B, C, D, void> _method;
    public FastAction(delegate*<A, B, C, D, void> target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));
        _method = target;
    }
    public FastAction(Type type, string name, bool ignoreCase = false)
    {
        _method = (delegate*<A, B, C, D, void>)GetPointer(CheckGet(type, name, ignoreCase, typeof(void), typeof(A), typeof(B), typeof(C), typeof(D)));
    }
    public FastAction(MethodInfo method)
    {
        Check(method, typeof(void), typeof(A), typeof(B), typeof(C), typeof(D));
        _method = (delegate*<A, B, C, D, void>)GetPointer(method);
    }
    public void Invoke(A arg0, B arg1, C arg2, D arg3) => _method(arg0, arg1, arg2, arg3);
}
public unsafe class FastAction<A, B, C, D, E> : FastDelegateBase
{
    delegate*<A, B, C, D, E, void> _method;
    public FastAction(delegate*<A, B, C, D, E, void> target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));
        _method = target;
    }
    public FastAction(Type type, string name, bool ignoreCase = false)
    {
        _method = (delegate*<A, B, C, D, E, void>)GetPointer(CheckGet(type, name, ignoreCase, typeof(void), typeof(A), typeof(B), typeof(C), typeof(D), typeof(E)));
    }
    public FastAction(MethodInfo method)
    {
        Check(method, typeof(void), typeof(A), typeof(B), typeof(C), typeof(D), typeof(E));
        _method = (delegate*<A, B, C, D, E, void>)GetPointer(method);
    }
    public void Invoke(A arg0, B arg1, C arg2, D arg3, E arg4) => _method(arg0, arg1, arg2, arg3, arg4);
}


public class FastDelegateBase
{
    protected static void Check(MethodInfo method, Type returnType, params Type[] parameters)
    {
        if (method.CheckCallSig(returnType, parameters) == -1)
            throw new ArgumentException($"Method does not match the call signiture {returnType.ToDisplayName()}({parameters.ToParamsStr()})", nameof(method));
    }
    protected static MethodInfo CheckGet(Type type, string name, bool ignoreCase, Type returnType, params Type[] parameters)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));
        if (type.ContainsGenericParameters)
            throw new ArgumentException("Type cannot contain unassigned generic parameters", nameof(type));
        var m = type.FindMethod(name, ignoreCase, returnType, parameters);
        if (m == null)
            throw new MissingMethodException($"No suitable method found for {type.ToDisplayName()}:{name}({parameters.ToParamsStr()}) {returnType.ToDisplayName()}");
        return m;
    }
    protected static IntPtr GetPointer(MethodInfo method)
    {
        var handle = method.MethodHandle;
        handle.GetFunctionPointer();
        RuntimeHelpers.PrepareMethod(handle);
        return handle.GetFunctionPointer();
    }
}
public static class FastDelegateExtentions
{
    public static int CheckCallSig(this MethodInfo method, Type returnType, params Type[] parameters)
    {
        if (method == null)
            throw new ArgumentNullException(nameof(method));
        if (method.ContainsGenericParameters || method.DeclaringType.ContainsGenericParameters)
            throw new ArgumentException("Method cannot contain unassigned generic parameters", nameof(method));
        if (returnType != null)
        {

            if (returnType == typeof(void))
            {
                if (method.ReturnType != typeof(void))
                    return -1;
            }
            else
            if (!returnType.IsAssignableFrom(method.ReturnType))
                return -1;
        }
        var result = 0;
        var mParameters = method.GetParameters();
        var startInd = method.IsStatic ? 0 : 1;
        if (mParameters.Length + startInd != parameters.Length)
            return -1;
        if (!method.IsStatic && !method.DeclaringType.IsAssignableFrom(parameters[0]))
            return -1;
        for (int i = 0; i < mParameters.Length; i++)
            if (!mParameters[i].ParameterType.IsAssignableFrom(parameters[i + startInd]))
                return -1;
            else if (mParameters[i].ParameterType != parameters[i + startInd])
                result++;
        return result;
    }
    public static MethodInfo FindMethod(this Type type, string name, bool ignoreCase, Type returnType, params Type[] parameters)
    {
        if (type.ContainsGenericParameters)
            return null;
        int bestMatch = int.MaxValue;
        MethodInfo best = null;
        foreach (var m in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            if (!m.ContainsGenericParameters && string.Equals(m.Name, name, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                var match = m.CheckCallSig(returnType, parameters);
                if (match == 0)
                    return m;
                if (match == -1)
                    continue;
                if (bestMatch > match)
                {
                    bestMatch = match;
                    best = m;
                }
            }
        return best;
    }
    public static StringBuilder AppendDisplayName(this StringBuilder builder, Type type)
    {
        if (builder == null)
            builder = new StringBuilder();
        if (type == null || type == typeof(void))
            builder.Append("void");
        else if (type == typeof(object))
            builder.Append("object");
        else if (type == typeof(string))
            builder.Append("string");
        else if (type == typeof(byte))
            builder.Append("byte");
        else if (type == typeof(sbyte))
            builder.Append("sbyte");
        else if (type == typeof(ushort))
            builder.Append("ushort");
        else if (type == typeof(short))
            builder.Append("short");
        else if (type == typeof(uint))
            builder.Append("uint");
        else if (type == typeof(int))
            builder.Append("int");
        else if (type == typeof(ulong))
            builder.Append("ulong");
        else if (type == typeof(long))
            builder.Append("long");
        else if (type == typeof(float))
            builder.Append("float");
        else if (type == typeof(double))
            builder.Append("double");
        else if (type == typeof(char))
            builder.Append("char");
        else if (type.IsPointer)
            builder.Append("*").AppendDisplayName(type.GetElementType());
        else if (type.IsByRef)
            builder.Append("&").AppendDisplayName(type.GetElementType());
        else if (type.IsArray)
            builder.AppendDisplayName(type.GetElementType()).Append("[").Append(',', type.GetArrayRank()).Append("]");
        else
        {
            if (!string.IsNullOrEmpty(type.Namespace))
                builder.Append(type.Namespace).Append(".");
            builder.Append(type.Name);
            if (type.IsConstructedGenericType)
                builder.Append("<").AppendParams(type.GenericTypeArguments).Append(">");
        }
        return builder;
    }
    public static StringBuilder AppendParams(this StringBuilder builder, Type[] types)
    {
        for (int i = 0; i < types.Length; i++)
        {
            if (i != 0)
                builder.Append(", ");
            builder.AppendDisplayName(types[i]);
        }
        return builder;
    }
    public static string ToParamsStr(this Type[] types) => new StringBuilder().AppendParams(types).ToString();
    public static string ToDisplayName(this Type type) => new StringBuilder().AppendDisplayName(type).ToString();
}