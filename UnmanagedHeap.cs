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
	
	public class UnmanagedObject : IDisposable
	{
		internal int index;
		internal int ptr;
		
		
		#region IDisposable implementation
		void IDisposable.Dispose()
		{
			// throw new NotImplementedException();
		}
		#endregion
	}
	
	
	/// <summary>
	/// Description of UnmanagedHeap.
	/// </summary>
	public unsafe class UnmanagedHeap<TPoolItem> where TPoolItem : UnmanagedObject
	{
		private readonly TPoolItem[] _freeObjects;
		private readonly TPoolItem[] _allObjects;
		private readonly int _totalSize;
		private int _freeSize;
		private readonly ConstructorInfo _ctor;
		
		public unsafe UnmanagedHeap(int capacity)
		{                                
			_allObjects = new TPoolItem[capacity];
			_freeSize = capacity;
			
			var objectSize = 20; //GCEx.SizeOf<T>();
			_totalSize = objectSize * capacity;
			
			var startingPointer = Marshal.AllocHGlobal(_totalSize).ToInt32();
			var mTable = (MethodTableInfo *)typeof(TPoolItem).TypeHandle.Value.ToInt32();
			
			var ptr = new EntityPtr();
			var pFake = typeof(Stub).GetMethod("Construct", BindingFlags.Static|BindingFlags.Public);
			var pCtor = _ctor = typeof(TPoolItem).GetConstructor(new Type[]{typeof(int)});
		
			MethodUtil.ReplaceMethod(pCtor, pFake);
			
			for(int i = 0; i < capacity; i++)
			{
				ptr.Handler =  startingPointer + (objectSize * i);
				ptr.Object.SetMethodTable(mTable);
				
				var reference = (TPoolItem)ptr.Object;
				reference.index = i;
				reference.ptr = EntityPtr.ToHandler(ptr.Object) + 4;
				
				_allObjects[i] = reference;
			}			
			
			_freeObjects = (TPoolItem[])_allObjects.Clone();
			
			// compile methods
			this.Free(this.Allocate());
			this.Free(this.AllocatePure());
		}
		
		public int TotalSize
		{
			get {
				return _totalSize;
			}
		}
		
		public TPoolItem Allocate()
		{
			_freeSize--;
			var obj = _freeObjects[_freeSize];
			Stub.Construct(_freeObjects[_freeSize].ptr, 123);			
			return (TPoolItem)obj;
		}
		
		public TPoolItem AllocatePure()
		{
			_freeSize--;
			var obj = _freeObjects[_freeSize]; 
			_ctor.Invoke(obj, new object[]{123});			
			return (TPoolItem)obj;
		}
		
		public void Free(TPoolItem obj)
		{
			_freeObjects[_freeSize] = obj;
			_freeSize++;
		}	
		
		public void Reset()
		{
			_allObjects.CopyTo(_freeObjects, 0);
			_freeSize = _freeObjects.Length;
		}
	}
}
