using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json.Linq;

namespace DGTools.Database {
    /// <summary>
    /// Fills a <see cref="IFillable{Titem}"/> container with database results. Can be executed asynchronously
    /// </summary>
    public class FillRequest<Titem> : GetRequest<Titem>, IRequestAsyncable where Titem : IDatabasable, new()
    {
        public IFillable<Titem> container { get; protected set; }

        public FillRequest(Func<JToken, bool> filter, IFillable<Titem> container) : base(filter)
        {
            this.container = container;
        }

        public IEnumerator OnExecuteAsync()
        {
            float startTime = Time.realtimeSinceStartup;
            int lastResultIndex = 0;
            IEnumerator<List<Titem>> execution = table.GetManyAsync(filter);

            while (execution.MoveNext())
            {
                int diff = execution.Current.Count - lastResultIndex;
                if (diff > 0)
                {
                    for (int i = lastResultIndex; i < lastResultIndex + diff; i++)
                    {
                        container.AddItem(execution.Current[i]);
                    }
                    lastResultIndex += diff - 1;
                }

                if (Time.realtimeSinceStartup - startTime > Database.Settings.coroutinesMaxExecutionTime)
                {
                    yield return new WaitForEndOfFrame();
                    startTime = Time.realtimeSinceStartup;
                }
            }
        }

        protected override void OnExecute()
        {
            container.AddItems(table.GetMany(filter).ToArray());
        }
    }
}
