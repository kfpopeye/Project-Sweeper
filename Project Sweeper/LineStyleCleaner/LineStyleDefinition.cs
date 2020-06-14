using System;
using System.ComponentModel;
using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace PKHL.ProjectSweeper.LineStyleCleaner
{
    public class LineStyleDefinition : ViewOwnerDefinition
    {
        public string StyleWeight { get; set; }
        /// <summary>
        /// A negative categoryId indicated system category, not deleteable
        /// </summary>
		public int CategoryId { get; set; }
        public int ModelLinesUsingStyle { get; set; }
        public int DetailLinesUsingStyle { get; set; }
        public LinePattern thePattern { get; set; }
        public string StylePattern { get; set; }

        public override bool IsDeleteable
        {
            get
            {
                if (CategoryId < 0)
                    return false;
                else
                    return is_deleteable;
            }
            set
            {
                is_deleteable = value;
                if (!is_deleteable)
                    its_deleted_status = false;
            }
        }

        private bool its_deleted_status = false;
        public override bool StyleToBeDeleted
        {
            get
            {
                if (is_deleteable)
                    return its_deleted_status;
                else
                    return false;
            }
            set
            {
                its_deleted_status = value;
                OnPropertyChanged("StyleToBeDeleted");
                if (is_deleteable)
                    System.Diagnostics.Debug.WriteLine(StyleName + " StyleToBeDeleted set to -> " + value.ToString());
                else
                    System.Diagnostics.Debug.WriteLine(string.Format("{0} StyleToBeDeleted set to -> {1} however is_deleted is set to -> {2}",
                        StyleName, value, is_deleteable));
            }
        }

        public override int NumberOfUses
        {
            get
            {
                return (ModelLinesUsingStyle + DetailLinesUsingStyle);
            }
        }

        public LineStyleDefinition()
            : base()
        {
            System.Diagnostics.Debug.WriteLine("LSD ctor : default");
        }

        public LineStyleDefinition(GraphicsStyle _gs)
        {
            this.StyleName = _gs.Name;
            System.Diagnostics.Debug.WriteLine("LSD ctor : " + StyleName);
            this._itsColour = _gs.GraphicsStyleCategory.LineColor;
            int? w = _gs.GraphicsStyleCategory.GetLineWeight(_gs.GraphicsStyleType);
            if (w != null)
                this.StyleWeight = w.ToString();
            else
                this.StyleWeight = string.Empty;
            this.CategoryId = _gs.GraphicsStyleCategory.Id.IntegerValue;
            if (CategoryId < 0)
                is_deleteable = false;
            this.ItsId = _gs.Id.IntegerValue;
            this.ModelLinesUsingStyle = 0;
            this.DetailLinesUsingStyle = 0;

            ElementId eid = _gs.GraphicsStyleCategory.GetLinePatternId(_gs.GraphicsStyleType);
            if (eid != LinePatternElement.GetSolidPatternId() && eid != ElementId.InvalidElementId)
            {
                LinePatternElement lpe = _gs.Document.GetElement(eid) as LinePatternElement;
                this.thePattern = lpe.GetLinePattern();
                this.StylePattern = lpe.Name;
            }
            else
            {
                this.thePattern = null;
                this.StylePattern = "Solid";
            }
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            LineStyleDefinition lsd = obj as LineStyleDefinition;
            if (this.StyleName != lsd.StyleName ||
                this.StyleColour != lsd.StyleColour ||
                this.StylePattern != lsd.StylePattern ||
                this.StyleWeight != lsd.StyleWeight)
                return false;

            return true;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return ItsId;
        }
    }
}
