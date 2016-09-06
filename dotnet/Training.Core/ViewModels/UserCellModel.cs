﻿//
// UserCellModel.cs
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
using System.Windows.Input;

using MvvmCross.Core.ViewModels;

namespace Training.Core
{
    public class UserCellModel : BaseViewModel<UserModel>
    {
        public ICommand DeleteCommand
        {
            get {
                return new MvxCommand(Model.Delete);
            }
        }

        public string Name 
        {
            get {
                return Model.Name;
            }
        }

        public UserCellModel(string databaseName, string documentID) : base(new UserModel(databaseName, documentID))
        {
        }
    }
}

