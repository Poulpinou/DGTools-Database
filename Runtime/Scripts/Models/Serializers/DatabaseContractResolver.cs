using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Reflection;
using System.Linq;

namespace DGTools.Database
{
	public class DatabaseContractResolver : DefaultContractResolver
    {
        const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);

            properties = properties.Where(
                p => p.AttributeProvider.GetAttributes(true).Any(a => a is DatabaseFieldAttribute) || p.PropertyName == "ID"
            ).ToList();

            IList<JsonProperty> linkedProperties = properties.Where(p => typeof(IDatabasable).IsAssignableFrom(p.PropertyType)).ToList();

            if (linkedProperties.Count > 0) {
                MemberInfo[] members = type.GetFields(bindingFlags)
                                        .Cast<MemberInfo>()
                                        .Concat(type.GetProperties(bindingFlags))
                                        .ToArray();

                foreach (JsonProperty property in linkedProperties)
                {
                    property.ValueProvider = new DatabasableValueProvider(members.Where(m => m.Name == property.PropertyName).First());
                    property.PropertyType = typeof(int);
                    property.PropertyName += "_id";
                }
            }

            

            return properties;
        }
    }
}
