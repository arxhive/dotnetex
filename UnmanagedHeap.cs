using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.CLR
{
	public delegate void CtorDelegate(IntPtr obj);
	
	internal static class Stub
	{
		public static void Construct(IntPtr obj, int value)
		{
			Console.WriteLine("Construct1");
		}	
	}
	
	
	/// <summary>
	/// Description of UnmanagedHeap.
	/// </summary>
	public unsafe class UnmanagedHeap<T> where T : new()
	{
		private readonly Queue<WeakReference> _freeObjects;
		private readonly List<WeakReference> _allObjects;
		private readonly int _totalSize;
		private readonly CtorDelegate _ctor;
		//private readonly Stub _stub = new Stub();
		
		public unsafe UnmanagedHeap(int capacity)
		{                                
		    _freeObjects = new Queue<WeakReference>(capacity);
			_allObjects = new List<WeakReference>(capacity);
			
			var objectSize = 20; //GCEx.SizeOf<T>();
			_totalSize = objectSize * capacity;
			
			var startingPointer = Marshal.AllocHGlobal(_totalSize).ToInt32();
			var mTable = (MethodTableInfo *)typeof(T).TypeHandle.Value.ToInt32();
			
			var ptr = new EntityPtr();
			var pFake = typeof(Stub).GetMethod("Construct", BindingFlags.Static|BindingFlags.Public);
			var pCtor = typeof(T).GetConstructor(new Type[]{typeof(int)});
		
			MethodUtil.ReplaceMethod(pCtor, pFake);
			
			for(int i = 0; i < capacity; i++)
			{
				ptr.Handler =  startingPointer + (objectSize * i);
				ptr.Object.SetMethodTable(mTable);
				var reference = new WeakReference(ptr.Object);
				_freeObjects.Enqueue(reference);
				_allObjects.Add(reference);
			}
		}
		
		private unsafe CtorDelegate CtorToDelegate()
		{
			var handle= typeof(T).GetConstructor(Type.EmptyTypes).MethodHandle;
			RuntimeHelpers.PrepareMethod(handle);
			var ctor = new IntPtr(*(int *)MethodUtil.GetMethodAddress(typeof(T).GetConstructor(Type.EmptyTypes)).ToPointer());
			return (CtorDelegate)Marshal.GetDelegateForFunctionPointer(ctor, typeof(CtorDelegate));
		}
		
		public int TotalSize
		{
			get 
            {
				return _totalSize;
			}
		}
		
		public T Allocate()
		{			
			var obj = _freeObjects.Dequeue().Target;			
			var ptr = new IntPtr(EntityPtr.ToHandler(obj) + 4);
			Stub.Construct(ptr, 123);
			
			return (T)obj;
		}
		
		public void Free(T obj)
		{
			_freeObjects.Enqueue(new WeakReference(obj));			
		}	
	}
}
