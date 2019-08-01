using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json.Linq;

namespace DGTools.Database
{
    /// <summary>
    /// A request is an object that will help you to interract with the database
    /// How to use?
    /// <code>
    ///     TRequest request =  new TRequest({Trequest params});
    ///     request.Execute();
    /// </code>
    /// </summary>
    public abstract class Request
    {
        #region Properties
        /// <summary>
        /// True if the request has been executed
        /// </summary>
        public bool isDone { get; set; } = false;
        #endregion

        #region Abstract Methods
        /// <summary>
        /// Override this and put the <see cref="Request"/> behaviour in it
        /// </summary>
        protected abstract void OnExecute();
        #endregion

        #region Public Methods
        /// <summary>
        /// Executes the <see cref="Request"/>
        /// </summary>
        public virtual void Execute()
        {
            OnExecute();
            isDone = true;
        }
        #endregion
    }

    public abstract class Request<Tdata> : Request where Tdata : IDatabasable, new()
    {
        #region Properties
        /// <summary>
        /// Returns the table for <typeparamref name="Tdata"/> item
        /// </summary>
        public Table<Tdata> table => Database.active.GetTable<Tdata>();
        #endregion
    }

    public abstract class GetRequest<Tdata> : Request<Tdata> where Tdata : IDatabasable, new()
    {
        #region Properties
        /// <summary>
        /// The result of the request
        /// </summary>
        public virtual Tdata result { get; protected set; }

        /// <summary>
        /// The filter function for the request
        /// ex : <code>
        ///     new GetRequest<MyItem>(datas => (string)datas["myCustomName"] == "Shuckle");
        /// </code>
        /// </summary>
        public Func<JToken, bool> filter { get; protected set; }
        #endregion

        #region Constructors
        public GetRequest(Func<JToken, bool> filter)
        {
            this.filter = filter;
        }
        #endregion
    }

    public abstract class PostRequest<Tdata> : Request<Tdata> where Tdata : IDatabasable, new()
    {
        #region Properties
        /// <summary>
        /// The item to post
        /// </summary>
        public Tdata item { get; protected set; }

        /// <summary>
        /// Should the table be saved after request?
        /// </summary>
        public bool saveTable { get; protected set; }
        #endregion

        #region Constructors
        public PostRequest(Tdata item, bool saveTable = true)
        {
            this.item = item;
            this.saveTable = saveTable;
        }
        #endregion

        #region Public Methods
        public override void Execute()
        {
            base.Execute();
            if (saveTable)
                table.SaveTable();
        }
        #endregion
    }

    public class GetOneRequest<Tdata> : GetRequest<Tdata> where Tdata : IDatabasable, new()
    {
        #region Constructors
        public GetOneRequest(Func<JToken, bool> filter) : base(filter) { }
        #endregion

        #region Private Methods
        protected override void OnExecute()
        {
            result = table.GetOne(filter);
        }
        #endregion
    }

    public class GetManyRequest<Tdata> : GetRequest<Tdata>, IRequestAsyncable where Tdata : IDatabasable, new()
    {
        #region Properties
        /// <summary>
        /// The list of results of the request
        /// </summary>
        public List<Tdata> results { get; protected set; }
        #endregion

        #region Constructors
        public GetManyRequest(Func<JToken, bool> filter) : base(filter) { }
        #endregion

        #region Private Methods
        protected override void OnExecute()
        {
            results = table.GetMany(filter);
        }
        #endregion

        #region Coroutines
        [Obsolete]
        public IEnumerator OnExecuteAsync()
        {
            float startTime = Time.realtimeSinceStartup;
            IEnumerator<List<Tdata>> execution = table.GetManyAsync(filter);

            while (execution.MoveNext())
            {
                results = execution.Current;
                if (Time.realtimeSinceStartup - startTime > Database.Settings.coroutinesMaxExecutionTime)
                {
                    yield return new WaitForEndOfFrame();
                    startTime = Time.realtimeSinceStartup;
                }
            }
        }
        #endregion
    }
}
