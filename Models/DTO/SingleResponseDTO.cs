using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FixWebApi.Models.DTO
{
    public class SingleResponseDTO
    {
        public SignUpModel userDetail { get; set; }
        public List<EventModel> EventList { get; set; }
        public List<UserSettingModel> UserSettings { get; set; }
    }
}