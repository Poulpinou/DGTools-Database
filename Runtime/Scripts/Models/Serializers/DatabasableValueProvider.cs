using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using System;
using System.Linq;

namespace DGTools.Database
{
	public class DatabasableValueProvider : IValueProvider
    {
        MemberInfo infos;

        public DatabasableValueProvider(MemberInfo memberInfo) {
            infos = memberInfo;
        }

        public object GetValue(object target)
        {
            IDatabasable item = (IDatabasable)infos.GetValue(target);

            if (item != null) {
                if (item.ID <= 0) {
                    GenericsUtilities.CallMethod(typeof(IDatabasableExtensions), "Create", item.GetType(), null, item, true);
                }
                return item.ID;
            }

            return default;
        }

        public void SetValue(object target, object value)
        {
            infos.SetValue(target, Database.active.GetTable(infos.GetFieldType()).GetOneObjectByID((int)value));
        }
    }
}
