using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBlockingObject
{
    /// <summary>
    /// If this object is valid. Marked as virual as the most common use case will be if the
    ///   object is null or not. Since the BlockingVariable class does a null check first we
    ///   only need to return true here. However, you can override this variable if you'll
    ///   want more control (i.e. if you've connected to the server).
    /// </summary>
    public virtual bool IsValid => true;
}
