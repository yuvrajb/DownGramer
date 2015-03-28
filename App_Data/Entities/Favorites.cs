using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InstaAPI.Entities;

namespace DownGramer.App_Data.Entities
{
    [Serializable]
    internal class Favorites
    {
        public List<User> FavoriteList { get; set; }
    }
}
