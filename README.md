dotnetex
========

Gets size of .Net Framework objects, can change type of object to incompatible and can alloc .Net objects at unmanaged memory area

`
   var size = GCex.SizeOf<Object>();    // prints 12 on 32-bit .Net Framework;
   var size = GCEx.SizeOf(someObject);  // prints size for already existing object;
`
> SizeOf allows to compute size of any .Net type, including reference types, strings, arrays and so on.

`
  var from = new object();
  
  callSomeMethodOrJustCodeBlock();
  
  GCEx.EnumerateSOH(from).Select(obj => GCEx.SizeOf(obj)).Sum()
`
> will compute sum of objects, which are allocated by callSomeMethodOrJustCodeBlock();

`
  var heap = new UnmanagedHeap<Foo>(100);
  var obj = heap.Allocate();
  
  obj.CallMethod();
  
  heap.Free(obj);
`
> Will create objects pool in unmanaged memory. Object will have type 'Foo' and pool's size will be 100.
