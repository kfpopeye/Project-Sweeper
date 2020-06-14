using System.Collections.Generic;

namespace PKHL.ProjectSweeper
{
    public abstract class ViewOwnerDefinition : BaseStyleDefinition
    {
        public Dictionary<int, string> OwnerViews { get; set; }
        public Dictionary<int, string> OwnerSchedules { get; set; }

        public ViewOwnerDefinition()
        {
            OwnerSchedules = null;
            OwnerViews = null;
        }
    }
}
