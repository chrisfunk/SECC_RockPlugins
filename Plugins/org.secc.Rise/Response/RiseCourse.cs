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
using System.Runtime.Remoting.Activation;
using Newtonsoft.Json;
using Rock.Data;
using org.secc.Rise.Model;

namespace org.secc.Rise.Response
{
    [Url( "courses" )]

    public class RiseCourse : RiseBase
    {
        [JsonProperty( "title" )]
        public string Title { get; set; }

        [JsonProperty( "url" )]
        public string Url { get; set; }

        public Course GetRockCourse()
        {
            return GetRockCourse( new CourseService( new RockContext() ) );
        }

        public Course GetRockCourse( CourseService courseService )
        {
            var course = courseService.GetByCourseId( Id );
            if ( course == null )
            {
                RiseClient riseClient = new RiseClient();
                course = riseClient.SyncCourse( this, courseService );
            }
            return course;
        }
    }
}
