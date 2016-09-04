﻿//
// TaskListsModel.cs
//
// Author:
// 	Jim Borden  <jim.borden@couchbase.com>
//
// Copyright (c) 2016 Couchbase, Inc All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Couchbase.Lite;
using Couchbase.Lite.Views;

namespace Training.Core
{
    /// <summary>
    /// The model for the list of task lists page
    /// </summary>
    public sealed class TaskListsModel : BaseModel, IDisposable
    {
        private const string TaskListType = "task-list";

        private Database _db;
        private LiveQuery _byNameQuery;
        private LiveQuery _incompleteQuery;

        /// <summary>
        /// Gets the list of task lists currently saved
        /// </summary>
        public ObservableCollection<TaskListCellModel> TasksList { get; } = new ObservableCollection<TaskListCellModel>();

        /// <summary>
        /// Gets the username of the user using the app
        /// </summary>
        public string Username
        {
            get {
                return _db?.Name;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="username">The username of the user using the app.</param>
        public TaskListsModel(string username)
        {
            _db = CoreApp.AppWideManager.GetDatabase(username);
            SetupViewAndQuery();
        }

        /// <summary>
        /// Creates a new task list
        /// </summary>
        /// <param name="name">The name of the task list.</param>
        public void CreateTaskList(string name)
        {
            var properties = new Dictionary<string, object> {
                ["type"] = TaskListType,
                ["name"] = name,
                ["owner"] = Username
            };

            var docId = $"{Username}.{Guid.NewGuid()}";
            var doc = default(Document);
            try {
                doc = _db.GetDocument(docId);
                doc.PutProperties(properties);
            } catch(Exception e) {
                var newException = new ApplicationException("Couldn't save task list", e);
                throw newException;
            }
        }

        private void SetupViewAndQuery()
        {
            var view = _db.GetView("list/listsByName");
            view.SetMap((doc, emit) =>
            {
                if(!doc.ContainsKey("type") || doc["type"] as string != "task-list" || !doc.ContainsKey("name")) {
                    return;
                }

                emit(doc["name"], null);
            }, "1.0");

            _byNameQuery = view.CreateQuery().ToLiveQuery();
            _byNameQuery.Changed += (sender, args) =>
            {
                int currentIndex = 0;
                foreach(var row in args.Rows) {
                    var title = row.Key as string;
                    if(currentIndex >= TasksList.Count) {
                        TasksList.Add(new TaskListCellModel(title, row.DocumentId));
                    }
                    if(row.Key as string != TasksList[currentIndex].Name) {
                        TasksList.Insert(currentIndex, new TaskListCellModel(title, row.DocumentId));
                    }

                    currentIndex++;
                }

                while(currentIndex < TasksList.Count) {
                    TasksList.RemoveAt(currentIndex);
                }
            };
            _byNameQuery.Start();

            var incompleteTasksView = _db.GetView("list/incompleteTasksCount");
            incompleteTasksView.SetMapReduce((doc, emit) =>
            {
                if(!doc.ContainsKey("type") || doc["type"] as string != "task") {
                    return;
                }

                if(!doc.ContainsKey("taskList")) {
                    return;
                }

                var list = JsonUtility.ConvertToNetObject<IDictionary<string, object>>(doc["taskList"]);
                if(!list.ContainsKey("id") || !list.ContainsKey("complete") || (bool)list["complete"]) {
                    return;
                }

                emit(list["id"], null);

             }, BuiltinReduceFunctions.Count, "1.0");

            _incompleteQuery = incompleteTasksView.CreateQuery().ToLiveQuery();
            _incompleteQuery.GroupLevel = 1;
            _incompleteQuery.Changed += (sender, e) => 
            {
                foreach(var row in e.Rows) {
                    var existingTask = TasksList.FirstOrDefault(x => x.Name == row.Key as string);
                    if(existingTask != null) {
                        existingTask.IncompleteCount = (int)row.Value;
                    }
                }
            };
            _incompleteQuery.Start();
        }

        public void Dispose()
        {
            _byNameQuery.Stop();
            _incompleteQuery.Stop();
            _db.Close();
        }
    }
}
