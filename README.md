# MatrixFFN
 a feed forward network with sigmoid activation function - allows 'n' hidden layers



## <u>1.About this namespace</u>

Purpose of this class library is **'FFN'** a feed forward network implementation. 

The class makes **n** hidden layers possible - it is your choice. For convenience networks can be loaded and saved. This feature is quite practical as there is the **'FFN_Window'** giving you an **UI** to define and train networks to your convenience. Trained networks can then be used in every program using the **'FFN'** internally - this gives everything into your hands to your success.

You can use Excel files for your datasets. With **NPOI** and my <u>helper wrapper</u> for it your datasets can be buffered in quite easily. The **'FFN_Window'** is only using that option for data loading. But you still can use raw data directly for the **'FFN'**-class.

The network is optimized as good as i could by using global fields as much as possible to avoid the garbage collector. The GC is one of the best inventions for C# but it will use memory and after a while CPU-resources in the background thus slowing calculations down.

Input and output layers use normal **double[]** giving only a low threshold for the common programmer to move data in all convenience. On that base a dataset is a field of arrays - a ragged array ( **double\[]\[]**  ). 

I would prefer to use the Excel sources and that's why the **'FFN_Window'** is using it. You can see a build in example with a parabel and for the loading the same dataset as Excel-version. The columns are marked as input/output telling the class their meaning. You can give this information manually for datasets having no headers. Creating datasets in Excel-version can be done with the <u>helper wrapper</u> - look at his homesite on GitHub "https://github.com/MarcTolkmitt/NPOIwrap".

## <u>2.the twin of **'FFN'** is **'FFN_ILGPU'**</u>

Having found **ILGPU** for C# led to the twin of **'FFN'**. **ILGPU** is giving you the ability to use the **GPU** for your own needs and me the possibility to offer the **'FFN'** as **'FFN_ILGPU'**.

Using **'Matrix'** in the math's of the network made the transpilation elegant  to **'Matrix_ILGPU'** on the other hand. I only had to rewrite my C# work into 3 new parts:

1. define an **'Action'**: the parameters for the kernel and then the working kernel's name
2. instantiate the **'Action'** in the constructor
3. put the original codework into a kernel having a position in the problemfield ( like with **ParallelFor** ).
4. calling this kernel via the **'Action'** in the original function.

Quite complex at the beginning to add 3 new parts to the original code - but having its own beauty. Watching the kernels in their reduction as they have a position in the problem field is awesome.

Example: the original function 

```c#
    public Matrix DeriveSigmoid()
    {
        Matrix temp = new Matrix(sizeX, sizeY, 0);
        for (int posX = 0; posX < sizeX; posX++)
            for (int posY = 0; posY < sizeY; posY++)
                temp.data[posX, posY] =
                    data[posX, posY] *
                    (1 - data[posX, posY]);

        return (temp);

    }   // end: DeriveSigmoid
```

will lead to an 'Action' being evoked using the kernel

```c#
    public MatrixILGPU DeriveSigmoid( )
    {
        MatrixILGPU target = new MatrixILGPU(sizeX, sizeY, 0);

        actionDeriveSigmoid_any(
            dataIl.Extent.ToIntIndex(),
            dataIl,
            target.dataIl );
        accelerator.Synchronize();

        target.data = target.dataIl.GetAsArray2D();

        return ( target );

    }   // end: DeriveSigmoid

    public static void DeriveSigmoid_any_Kernel(
            Index2D index,
            ArrayView2D<double, Stride2D.DenseX> source,
            ArrayView2D<double, Stride2D.DenseX> target
        )
    {
        target[ index.X, index.Y ] =
                    source[ index.X, index.Y ] *
                    ( 1 - source[ index.X, index.Y ] );

    }   // end: DeriveSigmoid_instance_Kernel
```

Here you can see the code's beauty. Originally there is a loop used for a position that is not there for the kernel as it is in position as part of the threadgroup running the kernel on the GPU. These positional reductions are the power for the kernels fulfilling matrix calculations for the **'Matrix_ILGPU'**-class.

### <u>3.Donations</u>

You can if you want donate to me. I always can use it, thank you.

https://www.paypal.com/ncp/payment/F4QDBSHGTXN2S
