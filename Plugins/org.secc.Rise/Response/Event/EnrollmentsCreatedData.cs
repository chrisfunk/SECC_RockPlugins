﻿// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System.Collections.Generic;
using Newtonsoft.Json;

namespace org.secc.Rise.Response.Event
{
    public class EnrollmentsCreatedData
    {
        [JsonProperty( "course" )]
        public RiseCourse Course { get; set; }

        [JsonProperty( "enrolledBy" )]
        public RiseUser EnrolledBy { get; set; }

        [JsonProperty( "groups" )]
        public List<RiseGroup> Groups { get; set; }

        [JsonProperty( "users" )]
        public List<RiseUser> Users { get; set; }
    }
}
