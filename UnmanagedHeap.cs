using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.CLR
{
	public delegate IntPtr MyUnmanagedDelegate(IntPtr obj);
	
	internal class Stub
	{
		public static void Construct(ConsoleTest.MainClass.Customer obj)
		{
			//return null;
		}
		
		public static void Construct2(ConsoleTest.MainClass.Customer obj)
		{
			Console.WriteLine("Construct2");
			//return null;
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
		private readonly MyUnmanagedDelegate _ctor;
		private readonly Stub _stub = new Stub();
		
		public unsafe UnmanagedHeap(int capacity)
		{                                
		    _freeObjects = new Queue<WeakReference>(capacity);
			_allObjects = new List<WeakReference>(capacity);
			
			var objectSize = 12; //GCEx.SizeOf<T>();
			_totalSize = objectSize * capacity;
			
			var startingPointer = Marshal.AllocHGlobal(_totalSize).ToInt32();
			var mTable = (MethodTableInfo *)typeof(T).TypeHandle.Value.ToInt32();
			
			var ptr = new EntityPtr();			
			//_ctor = CCtorToDelegate();
			
			var pFake = typeof(Stub).GetMethod("Construct", BindingFlags.Static|BindingFlags.Public);
			var pCtor = typeof(Stub).GetMethod("Construct2", BindingFlags.Static|BindingFlags.Public);//typeof(T).GetConstructor(Type.EmptyTypes);
			
			RuntimeHelpers.PrepareMethod(pFake.MethodHandle);
			RuntimeHelpers.PrepareMethod(pCtor.MethodHandle);
			
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
		
		/*
		private unsafe MyUnmanagedDelegate CCtorToDelegate()
		{
			var ctor = new IntPtr(*(int *)GetMethodAddress(typeof(T).GetConstructor(Type.EmptyTypes)).ToPointer());
		    return (MyUnmanagedDelegate) Marshal.GetDelegateForFunctionPointer(ctor, typeof(MyUnmanagedDelegate));
		}
		*/
		
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
			Stub.Construct((ConsoleTest.MainClass.Customer)obj);
			return (T)obj;
		}
		
		public void Free(T obj)
		{
			_freeObjects.Enqueue(new WeakReference(obj));			
		}	
	}
}
