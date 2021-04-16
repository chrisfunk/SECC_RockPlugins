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
using System.Runtime.Serialization;
using org.secc.Rise.Model;
using org.secc.xAPI.Model;

namespace org.secc.Rise.Helpers
{
    [DataContract]
    public class CourseResult : DotLiquid.Drop
    {
        [DataMember]
        public Course Course { get; set; }

        [DataMember]
        public List<int> CategoryIds { get; set; }

        [DataMember]
        public List<Experience> Experiences { get; set; }
    }
}