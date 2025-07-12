using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

public abstract class MemberRef
{
    protected object target;
    public readonly bool CanGet;
    public readonly bool CanSet;

    public MemberRef(object obj, bool canGet, bool canSet)
    {
        target = obj;
        CanGet = canGet;
        CanSet = canSet;
    }
    public abstract object IndirectValue { get; set; }
    public abstract Type MemberType { get; }

    static Type fieldBase = typeof(FieldRef<>);
    public static FieldRef MakeRef(object target, FieldInfo field)
        => (FieldRef)fieldBase.MakeGenericType(field.FieldType).GetConstructor(new[] { typeof(object), typeof(FieldInfo) }).Invoke(new[] { target, field });
    static Type propertyBase = typeof(PropertyRef<>);
    public static PropertyRef MakeRef(object target, PropertyInfo property)
        => (PropertyRef)propertyBase.MakeGenericType(property.PropertyType).GetConstructor(new[] { typeof(object), typeof(PropertyInfo) }).Invoke(new[] { target, property });
    public static MemberRef MakeRef(object target, MemberInfo member)
    {
        if (member == null)
            throw new ArgumentNullException(nameof(member));
        if (member is FieldInfo f)
            return MakeRef(target, f);
        if (member is PropertyInfo p)
            return MakeRef(target, p);
        throw new NotSupportedException("Cannot create MemberRef for a " + member.MemberType);
    }
    public static MemberRef MakeRef(object target, string member)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));
        var members = target.GetType().GetMember(member, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
        if (members.Length == 0)
            throw new ArgumentException("Member not found in type " + target.GetType().FullName, nameof(member));
        return MakeRef(target, members[0]);
    }
    public static MemberRef MakeRef(Type type, string member)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        var members = type.GetMember(member, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
        if (members.Length == 0)
            throw new ArgumentException("Member not found in type " + type.FullName, nameof(member));
        return MakeRef(null, members[0]);
    }
    public static FieldRef<T> MakeRef<T>(object target, FieldInfo field)
        => new FieldRef<T>(target, field);
    public static PropertyRef<T> MakeRef<T>(object target, PropertyInfo property)
        => new PropertyRef<T>(target, property);
    public static MemberRef<T> MakeRef<T>(object target, MemberInfo member)
    {
        if (member == null)
            throw new ArgumentNullException(nameof(member));
        if (member is FieldInfo f)
            return MakeRef<T>(target, f);
        if (member is PropertyInfo p)
            return MakeRef<T>(target, p);
        throw new NotSupportedException("Cannot create MemberRef for a " + member.MemberType);
    }
    public static MemberRef<T> MakeRef<T>(object target, string member) => MakeRef<T>(target, target.GetType().GetMember(member, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)[0]);
    public static MemberRef<T> MakeRef<T>(Type type, string member) => MakeRef<T>(null, type.GetMember(member, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)[0]);
}
public interface MemberRef<T>
{
    public abstract T Value { get; set; }
}

public abstract class FieldRef : MemberRef
{
    public IntPtr offset;

    public unsafe FieldRef(object obj) : base(obj, true, true) { }
    public unsafe FieldRef(object obj, FieldInfo field) : base(field.IsStatic ? obj = null : obj, true, true)
    {
        if (field.GetCustomAttribute(typeof(ThreadStaticAttribute)) != null)
            throw new ArgumentException("Field target cannot be thread static", nameof(field));
        offset = GetFieldOffset(obj,field);
    }
    public static unsafe IntPtr GetFieldOffset(object obj, FieldInfo field)
    {
        var m = new DynamicMethod("<" + field.FieldHandle.Value.ToString("X") + ">GetField", typeof(IntPtr), field.IsStatic ? new Type[0] : new[] { typeof(object) }, field.Module, true);
        var il = m.GetILGenerator();
        if (field.IsStatic)
            il.Emit(OpCodes.Ldsflda, field);
        else
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, field);
        }
        il.Emit(OpCodes.Newobj, typeof(IntPtr).GetConstructor(new[] { typeof(void*) }));
        il.Emit(OpCodes.Ret);
        if (field.IsStatic)
            return (IntPtr)((Func<IntPtr>)m.CreateDelegate(typeof(Func<IntPtr>)))();
        else
            return new IntPtr(((Func<object,IntPtr>)m.CreateDelegate(typeof(Func<object,IntPtr>)))(obj).ToInt64() - (*(IntPtr*)&obj).ToInt64());
    }
}

public class FieldRef<T> : FieldRef, MemberRef<T>
{
    public override Type MemberType => typeof(T);
    public FieldRef(object obj, FieldInfo field) : base(obj, field)
    {
        if (typeof(T) != field.FieldType)
            throw new ArgumentException("Field type does not match type argument", nameof(field));
    }
    public unsafe FieldRef(object obj, ref T field) : base(obj)
    {
        target = obj;
        long _offset;
        fixed (T* v = &field)
            _offset = (long)((byte*)v - (*(IntPtr*)&obj).ToInt64());
        if (_offset < 0)
            throw new ArgumentException("Value given is not a field of the target object", nameof(field));
        if (obj != null)
        {
            var size = obj.GetType().SizeOf();
            if (_offset >= (size == 0 ? 0xFFFFFF : size))
                throw new ArgumentException("Value given is not a field of the target object", nameof(field));
        }
        offset = (IntPtr)_offset;
    }
    public override object IndirectValue { get => Value; set => Value = (T)value; }
    public unsafe T Value
    {
        get
        {
            fixed (object* t = &target)
                return *(T*)(*(byte**)t + (long)offset);
        }
        set
        {
            fixed (object* t = &target)
                *(T*)(*(byte**)t + (long)offset) = value;
        }
    }
}

public abstract class PropertyRef : MemberRef
{
    public unsafe PropertyRef(object obj, bool canGet, bool canSet) : base(obj, canGet, canSet) { }
}

public unsafe class PropertyRef<T> : PropertyRef, MemberRef<T>
{
    public override Type MemberType => typeof(T);
    protected delegate*<object, T> getter;
    protected delegate*<object, T, void> setter;
    protected delegate*<T> staticgetter;
    protected delegate*<T, void> staticsetter;
    public unsafe PropertyRef(object obj, PropertyInfo property) : base(property.GetMethod?.IsStatic ?? property.SetMethod.IsStatic ? null : obj, property.GetMethod != null, property.SetMethod != null)
    {
        if (typeof(T) != property.PropertyType)
            throw new ArgumentException("Property type does not match type argument", nameof(property));
        if (property.GetIndexParameters().Length != 0)
            throw new ArgumentException("Property cannot be indexed", nameof(property));
        if (CanGet)
        {
            var handle = property.GetMethod.MethodHandle;
            handle.GetFunctionPointer();
            RuntimeHelpers.PrepareMethod(handle);
            if (property.GetMethod.IsStatic)
                staticgetter = (delegate*<T>)handle.GetFunctionPointer();
            else
                getter = (delegate*<object, T>)handle.GetFunctionPointer();
        }
        if (CanSet)
        {
            var handle = property.SetMethod.MethodHandle;
            handle.GetFunctionPointer();
            RuntimeHelpers.PrepareMethod(handle);
            if (property.SetMethod.IsStatic)
                staticsetter = (delegate*<T, void>)handle.GetFunctionPointer();
            else
                setter = (delegate*<object, T, void>)handle.GetFunctionPointer();
        }
    }
    public override object IndirectValue { get => Value; set => Value = (T)value; }
    public unsafe T Value
    {
        get => getter != null ? getter(target) : staticgetter != null ? staticgetter() : default;
        set
        {
            if (setter != null)
                setter(target, value);
            else if (staticsetter != null)
                staticsetter(value);
        }
    }
}

public static class MemberRefExtensions
{
    static bool firstRun = true;
    static object nObj;
    public static long SizeOf(this Type type)
    {
        var size = GC.GetTotalMemory(true);
        nObj = FormatterServices.GetUninitializedObject(type);
        if (firstRun)
        {
            firstRun = false;
            size = GC.GetTotalMemory(true);
            nObj = FormatterServices.GetUninitializedObject(type);
        }
        size = GC.GetTotalMemory(true) - size;
        nObj = null;
        return size;
    }
}