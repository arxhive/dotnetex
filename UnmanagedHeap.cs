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
		public static void Construct(int obj, int value)
		{
			Console.WriteLine("Construct1");
		}	
	}
	
	
	/// <summary>
	/// Description of UnmanagedHeap.
	/// </summary>
	public unsafe class UnmanagedHeap<T> where T : new()
	{
		private readonly Queue<T> _freeObjects;
		private readonly List<T> _allObjects;
		private readonly int _totalSize;
		private readonly ConstructorInfo _ctor;
		//private readonly Stub _stub = new Stub();
		
		public unsafe UnmanagedHeap(int capacity)
		{                                
		    _freeObjects = new Queue<T>(capacity);
			_allObjects = new List<T>(capacity);
			
			var objectSize = 20; //GCEx.SizeOf<T>();
			_totalSize = objectSize * capacity;
			
			var startingPointer = Marshal.AllocHGlobal(_totalSize).ToInt32();
			var mTable = (MethodTableInfo *)typeof(T).TypeHandle.Value.ToInt32();
			
			var ptr = new EntityPtr();
			var pFake = typeof(Stub).GetMethod("Construct", BindingFlags.Static|BindingFlags.Public);
			var pCtor = _ctor = typeof(T).GetConstructor(new Type[]{typeof(int)});
		
			MethodUtil.ReplaceMethod(pCtor, pFake);
			
			for(int i = 0; i < capacity; i++)
			{
				ptr.Handler =  startingPointer + (objectSize * i);
				ptr.Object.SetMethodTable(mTable);
				var reference = (T)ptr.Object;
				_freeObjects.Enqueue(reference);
				_allObjects.Add(reference);
			}
			
			obj = _freeObjects.Dequeue();			
			ppp = EntityPtr.ToHandler(obj) + 4;
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
			var obj = _freeObjects.Dequeue();			
			_ctor.Invoke(obj, new Object[]{123});		
			return (T)obj;
		}
		
		
		int ppp;
	T obj;
		public T AllocatePure()
		{
			/*
			obj = _freeObjects.Dequeue();			
			ppp = EntityPtr.ToHandler(obj) + 4;
			*/
			Stub.Construct(ppp, 123);			
			return (T)obj;
		}
		
		public void Free(T obj)
		{
			_freeObjects.Enqueue(obj);			
		}	
	}
}
