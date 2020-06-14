using System;
using Autodesk.Revit.DB;

namespace PKHL.ProjectSweeper
{

    public class AssetDefinition
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public int RvtId { get; set; }

        //generic
        public AssetDefinition(string type, string name)
        {
            Type = type;
            Name = name;
            RvtId = -1;
            Debug();
        }

        //used by line pattern cleaner
        public AssetDefinition(GraphicsStyle gs)
        {
            if(gs.GraphicsStyleCategory.Parent != null)
                Type = gs.GraphicsStyleCategory.Parent.Name;
            else
                Type = gs.GraphicsStyleCategory.Name;
            Name = gs.Name + " : " + LocalizationProvider.GetLocalizedValue<string>(gs.GraphicsStyleType.ToString()); // cut or projection
            RvtId = gs.Id.IntegerValue;
            Debug();
        }

        //used by fill pattern cleaner
        public AssetDefinition(Material m)
        {
            Type = "Material";
            Name = m.Name;
            RvtId = m.Id.IntegerValue;
            Debug();
        }

        //used by fill pattern cleaner
        public AssetDefinition(Element el)
        {
            Type = "Component";
            Name = el.Category.Name + " : " + el.Name;
            RvtId = el.Id.IntegerValue;
            Debug();
        }

        //used by fill pattern cleaner
        public AssetDefinition(FilledRegionType frt)
        {
            Type = "Region";
            Name = frt.Name;
            RvtId = frt.Id.IntegerValue;
            Debug();
        }

        //used by fill pattern cleaner
        public AssetDefinition(Family fs)
        {
            Type = "Family";
            Name = fs.FamilyCategory.Name + " : " + fs.Name;
            RvtId = fs.Id.IntegerValue;
            Debug();
        }

        private void Debug()
        {
            System.Diagnostics.Debug.WriteLine("\tCreate AssetDefinition for {0} {1}", Type, Name);
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            AssetDefinition rhs = obj as AssetDefinition;
            if (this.Type != rhs.Type ||
                this.Name != rhs.Name ||
                this.RvtId != rhs.RvtId)
                return false;

            return true;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return this.RvtId;
        }
    }

    //public class AssetDefinition
    //{
    //    public string Type { get; set; }
    //    public string Name { get; set; }

    //    public AssetDefinition(string t, string n)
    //    {
    //        Type = t;
    //        Name = n;
    //    }
    //}
}
