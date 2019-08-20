using System;

namespace DGTools.Database
{
    public class TableField
    {
        #region Public Variables
        public Type fieldType;
        public string fieldName;
        public bool isProperty;
        #endregion

        #region Constructors
        public TableField(Type fieldType, string fieldName, bool isProperty)
        {
            this.fieldType = fieldType;
            this.fieldName = fieldName;
            this.isProperty = isProperty;
        }
        #endregion
    }
}
