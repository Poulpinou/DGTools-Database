using System;

namespace DGTools.Database {
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
    public class DatabaseFieldAttribute : Attribute
    {

    }
}

